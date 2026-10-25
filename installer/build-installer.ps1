#
# SIC! Installer Build Script
# Copyright © 2026 Oire Software.
#
# Builds SIC! in Release x64 configuration and creates an installer.
# Signs sic.exe, the installer and its uninstaller with Azure Artifact Signing whenever
# installer\signing.json exists (-NoSign to skip; -Appcast and -Deploy refuse to go out unsigned
# without it). Signing needs the Artifact Signing client tools and an Azure CLI session
# (az login --use-device-code).
# Also creates a portable ZIP archive (use -NoPortable to skip).
# Optionally generates a signed appcast.xml for NetSparkle auto-updates.
# Optionally deploys release files to the hosting server via SCP (-Deploy).
#

[CmdletBinding(PositionalBinding=$false)]
param(
    [switch]$SkipBuild,
    [switch]$Appcast,
    [switch]$NoPortable,
    [switch]$Deploy,
    [switch]$OpenOutput,
    [switch]$NoSign,
    [string]$InnoSetupPath = "",
    [string]$SignToolPath = "",
    [string]$DlibPath = "",
    [string]$BaseUrl = "https://sic.oire.dev",
    [string]$KeyPath = "",
    [string]$ChangeLog = ""
)

# Script directory and paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$SolutionPath = Join-Path $RepoRoot "sic.sln"
$IssPath = Join-Path $ScriptDir "sic.iss"
$OutputDir = Join-Path $ScriptDir "Output"

Write-Host "SIC! Installer Build Script" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# -Deploy implies -Appcast (deploying without an appcast would be pointless)
if ($Deploy) {
    $Appcast = $true
}

# Clean build output (only when not skipping build)
$BuildOutputPath = Join-Path $RepoRoot "src\Sic\bin\x64\Release"
if (!$SkipBuild -and (Test-Path $BuildOutputPath)) {
    Write-Host "Cleaning release build directory..." -ForegroundColor Yellow
    Remove-Item -Path $BuildOutputPath -Recurse -Force
}

# Check if solution file exists
if (!(Test-Path $SolutionPath)) {
    Write-Error "Solution file not found at: $SolutionPath"
    exit 1
}

# Find InnoSetup compiler
if ([string]::IsNullOrEmpty($InnoSetupPath)) {
    $PossiblePaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
    )

    foreach ($Path in $PossiblePaths) {
        if (Test-Path $Path) {
            $InnoSetupPath = $Path
            break
        }
    }
}

if ([string]::IsNullOrEmpty($InnoSetupPath) -or !(Test-Path $InnoSetupPath)) {
    Write-Error "Inno Setup compiler not found. Please install Inno Setup or specify the path using -InnoSetupPath parameter."
    Write-Host "Download from: https://jrsoftware.org/isdl.php"
    exit 1
}

Write-Host "Using Inno Setup: $InnoSetupPath" -ForegroundColor Yellow

# Code signing setup
$SigningConfigPath = Join-Path $ScriptDir "signing.json"
$TimestampUrl = "http://timestamp.acs.microsoft.com"
$Sign = !$NoSign -and (Test-Path $SigningConfigPath)

if (!$NoSign -and !$Sign) {
    if ($Appcast) {
        Write-Error "Signing configuration not found at: $SigningConfigPath"
        Write-Host "Copy signing.example.json to signing.json and fill in the Artifact Signing details, or pass -NoSign to release unsigned." -ForegroundColor Yellow
        exit 1
    }

    Write-Warning "signing.json not found: building unsigned. Copy signing.example.json to signing.json to sign."
}

if ($Sign) {
    # Newest signtool from the Windows SDK
    if ([string]::IsNullOrEmpty($SignToolPath)) {
        $SignToolPath = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
            Where-Object { $null -ne ($_.Directory.Parent.Name -as [version]) } |
            Sort-Object { $_.Directory.Parent.Name -as [version] } -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }

    if ([string]::IsNullOrEmpty($SignToolPath) -or !(Test-Path $SignToolPath)) {
        Write-Error "signtool.exe not found. Install the Windows SDK signing tools or specify the path using -SignToolPath parameter."
        exit 1
    }

    # The signtool plugin from the Artifact Signing client tools (winget: Microsoft.Azure.ArtifactSigningClientTools)
    if ([string]::IsNullOrEmpty($DlibPath)) {
        $PossiblePaths = @(
            "${env:LOCALAPPDATA}\Microsoft\MicrosoftArtifactSigningClientTools\Azure.CodeSigning.Dlib.dll",
            "${env:LOCALAPPDATA}\Microsoft\MicrosoftTrustedSigningClientTools\Azure.CodeSigning.Dlib.dll"
        )

        foreach ($Path in $PossiblePaths) {
            if (Test-Path $Path) {
                $DlibPath = $Path
                break
            }
        }
    }

    if ([string]::IsNullOrEmpty($DlibPath) -or !(Test-Path $DlibPath)) {
        Write-Error "Azure.CodeSigning.Dlib.dll not found. Install it with:"
        Write-Host "  winget install --id Microsoft.Azure.ArtifactSigningClientTools --exact"
        exit 1
    }

    # The plugin authenticates through the Azure CLI session unless service principal
    # credentials are set in the environment (AZURE_CLIENT_ID and friends)
    if ([string]::IsNullOrEmpty($env:AZURE_CLIENT_ID)) {
        if ($null -eq (Get-Command az -ErrorAction SilentlyContinue)) {
            Write-Error "Azure CLI not found. Install it with: winget install --id Microsoft.AzureCLI --exact"
            exit 1
        }

        $AzAccount = & az account show --query user.name --output tsv 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Not signed in to Azure. Run: az login --use-device-code"
            exit 1
        }

        Write-Host "Signing as: $AzAccount" -ForegroundColor Yellow
    }

    Write-Host "Using signtool: $SignToolPath" -ForegroundColor Yellow
}

# Signs a file with Artifact Signing and checks the result
function Invoke-CodeSigning([string]$FilePath) {
    Write-Host "Signing $(Split-Path -Leaf $FilePath)..." -ForegroundColor Yellow

    & "$SignToolPath" sign /fd SHA256 /tr $TimestampUrl /td SHA256 /dlib "$DlibPath" /dmdf "$SigningConfigPath" "$FilePath"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Signing failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Assert-Signed $FilePath
}

function Assert-Signed([string]$FilePath) {
    $Signature = Get-AuthenticodeSignature -FilePath $FilePath

    if ($Signature.Status -ne "Valid") {
        Write-Error "$(Split-Path -Leaf $FilePath) has no valid signature: $($Signature.StatusMessage)"
        exit 1
    }

    Write-Host "  $(Split-Path -Leaf $FilePath) signed by: $($Signature.SignerCertificate.Subject)" -ForegroundColor Gray
}

# Build the application
if (!$SkipBuild) {
    Write-Host "Building SIC! (Release x64)..." -ForegroundColor Yellow

    try {
        $BuildArgs = @(
            $SolutionPath,
            "/verbosity:minimal"
        )

        & dotnet publish @BuildArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }

        Write-Host "Build completed successfully!" -ForegroundColor Green
    } catch {
        Write-Error "Build failed: $_"
        exit 1
    }
} else {
    Write-Host "Skipping build (using existing binaries)..." -ForegroundColor Yellow
}

# Verify required files exist in build output
$PublishOutputPath = Join-Path $BuildOutputPath "win-x64\publish"
$RequiredFiles = @(
    "sic.exe"
)

Write-Host "Verifying build output..." -ForegroundColor Yellow
foreach ($File in $RequiredFiles) {
    $FilePath = Join-Path $PublishOutputPath $File
    if (!(Test-Path $FilePath)) {
        Write-Error "Required file not found: $FilePath"
        exit 1
    }
}

# Sign the exe before Inno Setup and the portable ZIP pick it up. With -SkipBuild it may
# already carry a signature from an earlier run.
if ($Sign) {
    $ExePath = Join-Path $PublishOutputPath "sic.exe"

    if ((Get-AuthenticodeSignature -FilePath $ExePath).Status -eq "Valid") {
        Write-Host "sic.exe is already signed, skipping" -ForegroundColor Gray
    } else {
        Invoke-CodeSigning $ExePath
    }
}

# Clean and (re)create output directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Path "$OutputDir\*" -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Compile installer
Write-Host "Compiling installer..." -ForegroundColor Yellow
Write-Host "ISS File: $IssPath" -ForegroundColor Gray

$IsccArgs = @()

if ($Sign) {
    # Inno Setup signs the installer and its uninstaller itself through the "azure" sign tool
    # that sic.iss enables under /DSign. In the command, $q is a quote and $f the quoted file name.
    $SignCommand = '$q' + $SignToolPath + '$q sign /fd SHA256 /tr ' + $TimestampUrl + ' /td SHA256 /dlib $q' + $DlibPath + '$q /dmdf $q' + $SigningConfigPath + '$q $f'
    $IsccArgs += @("/DSign", "/Sazure=$SignCommand")
}

$IsccArgs += $IssPath

try {
    & "$InnoSetupPath" @IsccArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Installer compilation failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host "Installer created successfully!" -ForegroundColor Green
} catch {
    Write-Error "Installer compilation failed: $_"
    exit 1
}

# Find the created installer file and extract version
$InstallerFiles = Get-ChildItem -Path $OutputDir -Filter "sic-v*-setup.exe" |
    Sort-Object LastWriteTime -Descending

if ($InstallerFiles.Count -eq 0) {
    Write-Error "No installer file found in $OutputDir after compilation."
    exit 1
}

$Installer = $InstallerFiles[0]
$FileSize = [math]::Round($Installer.Length / 1MB, 2)

# The appcast below signs the installer's bytes, so it must already carry its Authenticode signature
if ($Sign) {
    Assert-Signed $Installer.FullName
}

if ($Installer.Name -match 'v([\d.]+)-setup') {
    $Version = $Matches[1]
} else {
    Write-Error "Could not extract version from filename: $($Installer.Name)"
    exit 1
}

Write-Host ""
Write-Host "Installer Details:" -ForegroundColor Green
Write-Host "  File: $($Installer.Name)" -ForegroundColor White
Write-Host "  Size: $FileSize MB" -ForegroundColor White
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Path: $($Installer.FullName)" -ForegroundColor White

# Create portable ZIP archive
if (!$NoPortable) {
    Write-Host ""
    Write-Host "Creating portable ZIP archive..." -ForegroundColor Yellow

    # Find 7-Zip
    $SevenZipPath = ""
    $SevenZipCmd = Get-Command 7z -ErrorAction SilentlyContinue

    if ($null -ne $SevenZipCmd) {
        $SevenZipPath = $SevenZipCmd.Source
    } else {
        $PossiblePaths = @(
            "${env:ProgramFiles}\7-Zip\7z.exe",
            "${env:ProgramFiles(x86)}\7-Zip\7z.exe",
            "${env:LOCALAPPDATA}\7-Zip\7z.exe"
        )

        foreach ($Path in $PossiblePaths) {
            if (Test-Path $Path) {
                $SevenZipPath = $Path
                break
            }
        }
    }

    $Use7Zip = ![string]::IsNullOrEmpty($SevenZipPath)
    if ($Use7Zip) {
        Write-Host "Using 7-Zip: $SevenZipPath" -ForegroundColor Yellow
    } else {
        Write-Warning "7-Zip not found. Using built-in Compress-Archive (larger file size). Install 7-Zip for better compression."
    }

    $PortableName = "sic-v$Version-portable"
    $PortableZip = Join-Path $OutputDir "$PortableName.zip"
    $PortableTempDir = Join-Path $env:TEMP $PortableName

    # Clean up any previous temp directory
    if (Test-Path $PortableTempDir) {
        Remove-Item -Path $PortableTempDir -Recurse -Force
    }

    # Copy publish output to temp directory
    Copy-Item -Path $PublishOutputPath -Destination $PortableTempDir -Recurse

    # Remove debug symbols
    $PdbFiles = Get-ChildItem -Path $PortableTempDir -Filter "*.pdb" -Recurse
    foreach ($Pdb in $PdbFiles) {
        Remove-Item -Path $Pdb.FullName -Force
        Write-Host "  Removed: $($Pdb.Name)" -ForegroundColor Gray
    }

    # Create empty userdata folder (marks as portable mode)
    New-Item -ItemType Directory -Path (Join-Path $PortableTempDir "userdata") -Force | Out-Null
    Write-Host "  Created: userdata/" -ForegroundColor Gray

    # Remove existing ZIP if present
    if (Test-Path $PortableZip) {
        Remove-Item -Path $PortableZip -Force
    }

    # Create ZIP archive
    if ($Use7Zip) {
        & "$SevenZipPath" a -tzip -mx=9 -mfb=258 -mpass=15 "$PortableZip" "$PortableTempDir\*"

        if ($LASTEXITCODE -ne 0) {
            Write-Error "7-Zip compression failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    } else {
        Compress-Archive -Path "$PortableTempDir\*" -DestinationPath $PortableZip -CompressionLevel Optimal
    }

    # Clean up temp directory
    Remove-Item -Path $PortableTempDir -Recurse -Force

    $PortableSize = [math]::Round((Get-Item $PortableZip).Length / 1MB, 2)
    Write-Host ""
    Write-Host "Portable ZIP Details:" -ForegroundColor Green
    Write-Host "  File: $PortableName.zip" -ForegroundColor White
    Write-Host "  Size: $PortableSize MB" -ForegroundColor White
    Write-Host "  Path: $PortableZip" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "Skipping portable ZIP (-NoPortable specified)..." -ForegroundColor Yellow
}

# Generate appcast if requested
if ($Appcast) {
    Write-Host ""
    Write-Host "Generating appcast..." -ForegroundColor Green
    Write-Host "=================================" -ForegroundColor Green
    Write-Host ""

    # Find key path
    if ([string]::IsNullOrEmpty($KeyPath)) {
        $KeyPath = Join-Path $RepoRoot "keys"
    }

    $PrivKeyFile = Join-Path $KeyPath "NetSparkle_Ed25519.priv"
    $PubKeyFile = Join-Path $KeyPath "NetSparkle_Ed25519.pub"

    if (!(Test-Path $PrivKeyFile)) {
        Write-Error "Private key not found at: $PrivKeyFile"
        Write-Host "Generate keys with: netsparkle-generate-appcast --generate-keys --key-path `"$KeyPath`""
        exit 1
    }

    if (!(Test-Path $PubKeyFile)) {
        Write-Error "Public key not found at: $PubKeyFile"
        exit 1
    }

    Write-Host "Using keys from: $KeyPath" -ForegroundColor Yellow

    # Ensure netsparkle-generate-appcast is available
    $AppcastTool = Get-Command netsparkle-generate-appcast -ErrorAction SilentlyContinue
    if ($null -eq $AppcastTool) {
        Write-Error "netsparkle-generate-appcast not found. Install it with:"
        Write-Host "  dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator"
        exit 1
    }

    if ([string]::IsNullOrEmpty($ChangeLog)) {
        $ChangeLog = Join-Path $RepoRoot "changelogs"
    }

    Write-Host "Download base URL: $BaseUrl" -ForegroundColor Yellow

    # Generate appcast
    Write-Host "Generating signed appcast..." -ForegroundColor Yellow

    $AppcastArgs = @(
        "--single-file", $Installer.FullName,
        "--key-path", $KeyPath,
        "--appcast-output-directory", $OutputDir,
        "--os", "windows",
        "--base-url", $BaseUrl,
        "--file-version", $Version,
        "--product-name", "SIC!"
    )

    if (Test-Path $ChangeLog) {
        $AppcastArgs += @("--change-log-path", $ChangeLog)
        Write-Host "Using changelogs from: $ChangeLog" -ForegroundColor Yellow
    } else {
        Write-Host "No changelogs directory found at $ChangeLog, skipping release notes" -ForegroundColor Gray
        Write-Host "  Create per-version files like: changelogs/1.0.0.md" -ForegroundColor Gray
    }

    & netsparkle-generate-appcast @AppcastArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Appcast generation failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    # Verify output
    $AppcastFile = Join-Path $OutputDir "appcast.xml"
    if (!(Test-Path $AppcastFile)) {
        Write-Error "appcast.xml was not created"
        exit 1
    }

    Write-Host ""
    Write-Host "Appcast generated successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Output files:" -ForegroundColor Green

Get-ChildItem -Path $OutputDir | ForEach-Object {
    Write-Host "  $($_.Name)" -ForegroundColor White
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green

# Deploy via SCP if requested
if ($Deploy) {
    Write-Host ""
    Write-Host "Deploying release files..." -ForegroundColor Green
    Write-Host "=================================" -ForegroundColor Green
    Write-Host ""

    # Load deploy configuration
    $DeployConfigPath = Join-Path $ScriptDir "deploy.json"
    $DeployExamplePath = Join-Path $ScriptDir "deploy.example.json"

    if (!(Test-Path $DeployConfigPath)) {
        Write-Error "Deploy configuration not found at: $DeployConfigPath"
        if (Test-Path $DeployExamplePath) {
            Write-Host "Copy $DeployExamplePath to $DeployConfigPath and fill in your SSH details." -ForegroundColor Yellow
        }
        exit 1
    }

    $DeployConfig = Get-Content $DeployConfigPath -Raw | ConvertFrom-Json

    if ([string]::IsNullOrEmpty($DeployConfig.SshHost) -or [string]::IsNullOrEmpty($DeployConfig.RemotePath)) {
        Write-Error "deploy.json must contain 'SshHost' and 'RemotePath' fields."
        exit 1
    }

    $SshHost = $DeployConfig.SshHost
    $RemotePath = $DeployConfig.RemotePath

    # Collect files to upload
    $FilesToUpload = @($Installer.FullName)

    if (!$NoPortable -and (Test-Path $PortableZip)) {
        $FilesToUpload += $PortableZip
    }

    $AppcastFile = Join-Path $OutputDir "appcast.xml"
    $AppcastSig = Join-Path $OutputDir "appcast.xml.signature"

    if (Test-Path $AppcastFile) {
        $FilesToUpload += $AppcastFile
    }

    if (Test-Path $AppcastSig) {
        $FilesToUpload += $AppcastSig
    }

    Write-Host "Uploading to ${SshHost}:${RemotePath}" -ForegroundColor Yellow
    foreach ($File in $FilesToUpload) {
        Write-Host "  $(Split-Path -Leaf $File)" -ForegroundColor White
    }

    Write-Host ""

    & scp -o BatchMode=yes @FilesToUpload "${SshHost}:${RemotePath}"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "SCP upload failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host ""
    Write-Host "Deploy completed successfully!" -ForegroundColor Green
} elseif ($Appcast) {
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    $UploadFiles = "$($Installer.Name), appcast.xml, and appcast.xml.signature"
    if (!$NoPortable) {
        $UploadFiles = "$($Installer.Name), $PortableName.zip, appcast.xml, and appcast.xml.signature"
    }
    Write-Host "  Upload $UploadFiles to: $BaseUrl/" -ForegroundColor White
    Write-Host "  Or re-run with -Deploy to upload automatically." -ForegroundColor White
}

if ($OpenOutput) {
    $SelectFile = if ($Appcast) { Join-Path $OutputDir "appcast.xml" } else { $Installer.FullName }
    Start-Process -FilePath "explorer.exe" -ArgumentList "/select,`"$SelectFile`""
}
