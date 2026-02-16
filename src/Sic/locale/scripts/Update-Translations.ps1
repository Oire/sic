# Update existing PO files with new strings from POT template
# This script merges new strings from messages.pot into existing language files

param(
    [string]$Language = "",
    [string]$PotFile = "../messages.pot"
)

# Import the Get-CatalogName function
. (Join-Path $PSScriptRoot "Get-CatalogName.ps1")

# Get the catalog name automatically
$catalogName = Get-CatalogName

# Adjust paths to work from scripts subfolder
$PotPath = Join-Path $PSScriptRoot $PotFile
$LocaleRoot = Split-Path $PSScriptRoot -Parent

if (!(Test-Path $PotPath)) {
    Write-Error "❌ POT file not found: $PotPath. Run Extract-Strings.ps1 first."
    exit 1
}

# Find msgmerge tool
$msgmerge = $null
$possiblePaths = @(
    "C:\Program Files (x86)\GnuWin32\bin\msgmerge.exe",
    "C:\Program Files\GnuWin32\bin\msgmerge.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $msgmerge = $path
        break
    }
}

if (!$msgmerge) {
    Write-Error "❌ msgmerge not found. Install gettext tools: winget install GnuWin32.GetText"
    exit 1
}

# Get languages to update
$languages = @()
if ($Language) {
    $languages = @($Language)
} else {
    $languages = Get-ChildItem -Directory -Path $LocaleRoot | Where-Object { $_.Name -match '^[a-z]{2}(-[A-Z]{2})?$' } | ForEach-Object { $_.Name }
}

if ($languages.Count -eq 0) {
    Write-Warning "⚠️ No language directories found. Use New-Language.ps1 to create a language first."
    exit 0
}

Write-Host "Updating translations for languages: $($languages -join ', ')" -ForegroundColor Green

foreach ($lang in $languages) {
    $langDir = Join-Path $LocaleRoot $lang
    $poFile = Join-Path $langDir "$catalogName.po"

    if (!(Test-Path $poFile)) {
        Write-Warning "⚠️ PO file not found: $poFile. Skipping $lang."
        continue
    }

    $backupFile = Join-Path $langDir "$catalogName.po.bak"

    try {
        Write-Host "Updating $lang..." -ForegroundColor Yellow

        # Create backup
        Copy-Item $poFile $backupFile

        # Merge new strings
        & "$msgmerge" --update --backup=off "$poFile" "$PotPath"

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Updated $lang successfully" -ForegroundColor Green
            # Remove backup on success
            Remove-Item $backupFile -ErrorAction SilentlyContinue
        } else {
            Write-Warning "⚠️ msgmerge failed for $lang. Restoring backup."
            Move-Item $backupFile $poFile -Force
        }
    } catch {
        Write-Error "❌ Failed to update ${lang}: $_"
        if (Test-Path $backupFile) {
            Move-Item $backupFile $poFile -Force
        }
    }
}

Write-Host "Update completed!" -ForegroundColor Cyan
Write-Host "Next step: Run Compile-Translations.ps1 to compile updated translations" -ForegroundColor Gray
