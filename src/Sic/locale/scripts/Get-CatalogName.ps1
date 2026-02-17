# Helper function to get the catalog name from the project
# This is shared by all localization scripts

function Get-CatalogName {
    param(
        [string]$ProjectPath = (Join-Path (Split-Path $PSScriptRoot -Parent) "..")
    )
    
    # Try to find a .csproj file
    $csprojFiles = Get-ChildItem -Path $ProjectPath -Filter "*.csproj" | Select-Object -First 1
    
    if ($csprojFiles) {
        # Read the .csproj file and look for AssemblyName
        [xml]$proj = Get-Content $csprojFiles.FullName
        
        # Try to find AssemblyName in PropertyGroup
        $assemblyName = $proj.Project.PropertyGroup.AssemblyName | Where-Object { $_ } | Select-Object -First 1
        
        if ($assemblyName) {
            Write-Host "Found AssemblyName in project file: $assemblyName" -ForegroundColor Gray
            return $assemblyName
        }
        
        # If no AssemblyName, use the project file name (without extension)
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($csprojFiles.Name)
        Write-Host "No AssemblyName found, using project name: $projectName" -ForegroundColor Gray
        return $projectName
    }
    
    # If we can't determine it automatically, throw an error
    throw "Unable to determine catalog name. Please ensure you're running from a project directory with a .csproj file."
}
