# Helper function to find a gettext tool such as msgfmt or msgmerge
# This is shared by the localization scripts that run gettext

function Find-GettextTool {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    # On the PATH first, then where mlocati's gettext build installs it, then GnuWin32
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $possiblePaths = @(
        (Join-Path $env:LOCALAPPDATA "Programs\gettext-iconv\bin\$Name.exe"),
        "C:\Program Files (x86)\GnuWin32\bin\$Name.exe",
        "C:\Program Files\GnuWin32\bin\$Name.exe"
    )

    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    Write-Error "❌ $Name not found. Install gettext tools: winget install mlocati.GetText"
    exit 1
}
