#
# SIC! Installer Build Script
# Copyright © 2026 Oire Software SARL.
#
# Builds SIC! in Release x64 configuration and creates an installer.
# Optionally generates a signed appcast.xml for NetSparkle auto-updates.
#

[CmdletBinding(PositionalBinding=$false)]
param(
    [switch]$SkipBuild,
    [switch]$Appcast,
    [switch]$OpenOutput,
    [string]$InnoSetupPath = "",
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

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Compile installer
Write-Host "Compiling installer..." -ForegroundColor Yellow
Write-Host "ISS File: $IssPath" -ForegroundColor Gray

try {
    & "$InnoSetupPath" "$IssPath"

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
        "--product-name", "SIC!",
        "--reparse-existing"
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

if ($Appcast) {
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  Upload $($Installer.Name), appcast.xml, and appcast.xml.signature to: $BaseUrl/" -ForegroundColor White
}

if ($OpenOutput) {
    $SelectFile = if ($Appcast) { Join-Path $OutputDir "appcast.xml" } else { $Installer.FullName }
    Start-Process -FilePath "explorer.exe" -ArgumentList "/select,`"$SelectFile`""
}
