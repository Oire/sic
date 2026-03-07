#
# SIC! Appcast Generation Script
# Copyright © 2026 Oire Software SARL.
#
# Generates a signed appcast.xml for NetSparkle auto-updates.
# Run this after build-installer.ps1 has produced the installer .exe.
#

param(
    [string]$BaseUrl = "https://oire.org/software/sic/releases",
    [string]$KeyPath = "",
    [string]$ChangeLog = "",
    [switch]$OpenOutput = $false
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$OutputDir = Join-Path $ScriptDir "Output"
$AppcastDir = Join-Path $OutputDir "appcast"

if ([string]::IsNullOrEmpty($ChangeLog)) {
    $ChangeLog = Join-Path $RepoRoot "changelogs"
}

Write-Host "SIC! Appcast Generation Script" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host ""

# Find the installer exe in Output/
$InstallerFiles = Get-ChildItem -Path $OutputDir -Filter "sic-v*-setup.exe" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending

if ($InstallerFiles.Count -eq 0) {
    Write-Error "No installer found in $OutputDir. Run build-installer.ps1 first."
    exit 1
}

$Installer = $InstallerFiles[0]
Write-Host "Found installer: $($Installer.Name)" -ForegroundColor Yellow
$FileSize = [math]::Round($Installer.Length / 1MB, 2)
Write-Host "  Size: $FileSize MB" -ForegroundColor Gray

# Extract version from filename: sic-v1.0.0.24-setup.exe -> 1.0.0.24
if ($Installer.Name -match 'v([\d.]+)-setup') {
    $Version = $Matches[1]
    Write-Host "  Version: $Version" -ForegroundColor Gray
} else {
    Write-Error "Could not extract version from filename: $($Installer.Name)"
    exit 1
}

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

# Create appcast output directory
if (!(Test-Path $AppcastDir)) {
    New-Item -ItemType Directory -Path $AppcastDir -Force | Out-Null
}

# Generate appcast
Write-Host "Generating signed appcast..." -ForegroundColor Yellow

$AppcastArgs = @(
    "--single-file", $Installer.FullName,
    "--key-path", $KeyPath,
    "--appcast-output-directory", $AppcastDir,
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
$AppcastFile = Join-Path $AppcastDir "appcast.xml"
if (!(Test-Path $AppcastFile)) {
    Write-Error "appcast.xml was not created"
    exit 1
}

Write-Host ""
Write-Host "Appcast generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output files:" -ForegroundColor Green

Get-ChildItem -Path $AppcastDir | ForEach-Object {
    Write-Host "  $($_.Name)" -ForegroundColor White
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Upload the installer to: $BaseUrl/" -ForegroundColor White
Write-Host "  2. Upload appcast.xml and appcast.xml.signature to: https://oire.org/software/sic/" -ForegroundColor White

if ($OpenOutput) {
    Start-Process -FilePath "explorer.exe" -ArgumentList "/select,`"$AppcastFile`""
}
