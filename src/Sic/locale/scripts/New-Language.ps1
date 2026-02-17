# Create a new language translation from the POT template
# This script creates a new locale directory and PO file for translation

param(
    [Parameter(Mandatory=$true)]
    [string]$Language,
    [string]$PotFile = "../messages.pot"
)

# Import the Get-CatalogName function
. (Join-Path $PSScriptRoot "Get-CatalogName.ps1")

Write-Host "Creating new language: $Language" -ForegroundColor Green

# Get the catalog name automatically
$catalogName = Get-CatalogName

# Adjust paths to work from scripts subfolder
$PotPath = Join-Path $PSScriptRoot $PotFile
$LocaleRoot = Split-Path $PSScriptRoot -Parent

if (!(Test-Path $PotPath)) {
    Write-Error "❌ POT file not found: $PotPath. Run Extract-Strings.ps1 first."
    exit 1
}

$LocaleDir = Join-Path $LocaleRoot $Language
$PoFile = Join-Path $LocaleDir "$catalogName.po"

try {
    # Create locale directory
    New-Item -ItemType Directory -Path $LocaleDir -Force | Out-Null
    Write-Host "📁 Created directory: $LocaleDir" -ForegroundColor Yellow

    # Copy POT to PO file
    Copy-Item $PotPath $PoFile
    Write-Host "📄 Created PO file: $PoFile" -ForegroundColor Yellow

    # Update PO file header with language information
    $content = Get-Content $PoFile -Raw
    $content = $content -replace 'POT-Creation-Date:', "POT-Creation-Date:"
    $content = $content -replace '"PO-Revision-Date: .*\\n"', "`"PO-Revision-Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:sszzz')\n`""
    $content = $content -replace '"Language-Team: .*\\n"', "`"Language-Team: $Language\n`""
    $content = $content -replace '"Language: .*\\n"', "`"Language: $Language\n`""
    
    # Add language header if not present
    if ($content -notmatch '"Language:') {
        $content = $content -replace '("Language-Team: .*\\n")', "`$1`"Language: $Language\n`""
    }

    Set-Content $PoFile $content -NoNewline
    
    Write-Host "✅ Language $Language created successfully!" -ForegroundColor Green
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Edit $PoFile to add translations" -ForegroundColor Gray
    Write-Host "2. Run Compile-Translations.ps1 -Language $Language" -ForegroundColor Gray
} catch {
    Write-Error "❌ Failed to create language: $_"
    exit 1
}