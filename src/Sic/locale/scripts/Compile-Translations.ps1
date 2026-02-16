# Compile PO files to MO format for use by the application
# This script uses msgfmt to compile .po files to .mo files

param(
    [string]$Language = ""
)

# Import the Get-CatalogName function
. (Join-Path $PSScriptRoot "Get-CatalogName.ps1")

# Get the catalog name automatically
$catalogName = Get-CatalogName

# Find msgfmt tool
$msgfmt = $null
$possiblePaths = @(
    "C:\Program Files (x86)\GnuWin32\bin\msgfmt.exe",
    "C:\Program Files\GnuWin32\bin\msgfmt.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $msgfmt = $path
        break
    }
}

if (!$msgfmt) {
    Write-Error "❌ msgfmt not found. Install gettext tools: winget install GnuWin32.GetText"
    exit 1
}

# Adjust paths to work from scripts subfolder
$LocaleRoot = Split-Path $PSScriptRoot -Parent

# Get languages to compile
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

Write-Host "Compiling translations for languages: $($languages -join ', ')" -ForegroundColor Green

$successCount = 0
$totalCount = 0

foreach ($lang in $languages) {
    $langDir = Join-Path $LocaleRoot $lang
    $poFile = Join-Path $langDir "$catalogName.po"
    $moFile = Join-Path $langDir "$catalogName.mo"
    
    if (!(Test-Path $poFile)) {
        Write-Warning "⚠️ PO file not found: $poFile. Skipping $lang."
        continue
    }

    $totalCount++
    
    try {
        Write-Host "Compiling $lang..." -ForegroundColor Yellow
        
        # Compile PO to MO
        & "$msgfmt" -o "$moFile" "$poFile"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Compiled $lang successfully" -ForegroundColor Green
            $successCount++
        } else {
            Write-Warning "⚠️ msgfmt failed for $lang"
        }
    } catch {
        Write-Error "❌ Failed to compile ${lang}: $_"
    }
}

Write-Host "Compilation completed: $successCount/$totalCount languages compiled successfully" -ForegroundColor Cyan

if ($successCount -gt 0) {
    Write-Host "Next step: Build the project to copy MO files to output directory" -ForegroundColor Gray
    Write-Host "Command: dotnet build -c Release" -ForegroundColor Gray
}