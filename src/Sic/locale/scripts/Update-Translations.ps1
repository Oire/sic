# Update existing PO files with new strings from POT template
# This script merges new strings from messages.pot into existing language files

param(
    [string]$Language = "",
    [string]$PotFile = "../messages.pot"
)

# Import the shared helper functions
. (Join-Path $PSScriptRoot "Get-CatalogName.ps1")
. (Join-Path $PSScriptRoot "Find-GettextTool.ps1")

# Get the catalog name automatically
$catalogName = Get-CatalogName

# Adjust paths to work from scripts subfolder
$PotPath = Join-Path $PSScriptRoot $PotFile
$LocaleRoot = Split-Path $PSScriptRoot -Parent

if (!(Test-Path $PotPath)) {
    Write-Error "❌ POT file not found: $PotPath. Run Extract-Strings.ps1 first."
    exit 1
}

$msgmerge = Find-GettextTool msgmerge

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
            # msgmerge writes LF; restore the repository's CRLF so an update isn't a whole-file diff.
            # .NET rather than Get-Content/Set-Content: under Windows PowerShell 5.1 those read UTF-8
            # as ANSI and write a BOM. Lines already ending in CRLF are left alone.
            $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
            $merged = [System.IO.File]::ReadAllText($poFile, $utf8NoBom)
            $crlf = [regex]::Replace($merged, '(?<!\r)\n', "`r`n")
            if ($crlf -cne $merged) {
                [System.IO.File]::WriteAllText($poFile, $crlf, $utf8NoBom)
            }

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
