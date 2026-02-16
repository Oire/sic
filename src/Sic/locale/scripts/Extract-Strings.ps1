# Extract translatable strings from source code to POT file
# This script uses GetText.NET.Extractor to extract all _() function calls

param(
    [string]$SourcePath = "../..",
    [string]$OutputFile = "../messages.pot"
)

Write-Host "Extracting translatable strings..." -ForegroundColor Green
Write-Host "Source: $SourcePath" -ForegroundColor Gray
Write-Host "Output: $OutputFile" -ForegroundColor Gray

try {
    # Run GetText.NET.Extractor with custom aliases
    GetText.Extractor --source "$SourcePath" --target "$OutputFile" --verbose --order `
        --aliasgetstring "_" `
        --aliasgetparticular "_p" `
        --aliasgetplural "_n" `
        --aliasgetparticularplural "_pn"
    
    Write-Host "✅ String extraction completed successfully!" -ForegroundColor Green
    Write-Host "Template file created: $OutputFile" -ForegroundColor Yellow
} catch {
    Write-Error "❌ String extraction failed: $_"
    exit 1
}