#
# SIC! Installer Build Script
# Copyright © 2026 Oire Software SARL.
#
# This script builds SIC! in Release x64 configuration and creates an installer
#

param(
    [string]$InnoSetupPath = "",
    [switch]$SkipBuild = $false,
    [switch]$OpenOutput = $false
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
    }
    catch {
        Write-Error "Build failed: $_"
        exit 1
    }
} else {
    Write-Host "Skipping build (using existing binaries)..." -ForegroundColor Yellow
}

# Verify required files exist in build output
$BuildOutputPath = Join-Path $RepoRoot "src\Sic\bin\x64\Release\win-x64\publish"
$RequiredFiles = @(
    "sic.exe"
)

Write-Host "Verifying build output..." -ForegroundColor Yellow
foreach ($File in $RequiredFiles) {
    $FilePath = Join-Path $BuildOutputPath $File
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

    # Find the created installer file
    $InstallerFiles = Get-ChildItem -Path $OutputDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending
    if ($InstallerFiles.Count -gt 0) {
        $InstallerPath = $InstallerFiles[0].FullName
        $FileSize = [math]::Round((Get-Item $InstallerPath).Length / 1MB, 2)

        Write-Host ""
        Write-Host "Installer Details:" -ForegroundColor Green
        Write-Host "  File: $($InstallerFiles[0].Name)" -ForegroundColor White
        Write-Host "  Size: $FileSize MB" -ForegroundColor White
        Write-Host "  Path: $InstallerPath" -ForegroundColor White

        if ($OpenOutput) {
            Write-Host "Opening output directory..." -ForegroundColor Yellow
            Start-Process -FilePath "explorer.exe" -ArgumentList "/select,`"$InstallerPath`""
        }
    }
}
catch {
    Write-Error "Installer compilation failed: $_"
    exit 1
}

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
