# Localization

This directory contains translation files and scripts for managing localization in SIC! (Simple Image Converter).

## Prerequisites

1. Install gettext tools:
   ```powershell
   winget install GnuWin32.GetText
   ```

2. Install GetText.NET.Extractor:
   ```powershell
   dotnet tool install --global GetText.NET.Extractor
   ```

## Scripts

The PowerShell scripts are located in the `scripts/` subdirectory.

### 1. scripts/Extract-Strings.ps1

Extracts all translatable strings from the source code into a POT template file.

```powershell
cd scripts
# Extract strings to messages.pot
.\Extract-Strings.ps1

# Custom source path and output file
.\Extract-Strings.ps1 -SourcePath "../.." -OutputFile "../custom.pot"
```

**When to use**: After adding new UI text or modifying existing strings in the code.

### 2. scripts/New-Language.ps1

Creates a new language translation by copying the POT template to a new locale directory.

```powershell
cd scripts
# Create Ukrainian translation
.\New-Language.ps1 -Language "uk-UA"

# Create German translation
.\New-Language.ps1 -Language "de-DE"
```

**When to use**: When adding support for a new language.

### 3. scripts/Update-Translations.ps1

Updates existing PO files with new strings from the POT template, preserving existing translations.

```powershell
cd scripts
# Update all languages
.\Update-Translations.ps1

# Update specific language
.\Update-Translations.ps1 -Language "uk-UA"
```

**When to use**: After extracting new strings to merge them into existing translations.

### 4. scripts/Compile-Translations.ps1

Compiles PO files to MO format for use by the application.

```powershell
cd scripts
# Compile all languages
.\Compile-Translations.ps1

# Compile specific language
.\Compile-Translations.ps1 -Language "uk-UA"
```

**When to use**: After editing translations to generate the binary MO files.

## Typical Workflow

1. **Add/modify strings in code** (wrap with `_()` function)
2. **Extract strings**: `cd scripts && .\Extract-Strings.ps1`
3. **Update existing translations**: `cd scripts && .\Update-Translations.ps1`
4. **Edit translations** in the `.po` files
5. **Compile to binary**: `cd scripts && .\Compile-Translations.ps1`
6. **Build project**: `dotnet build -c Release`

## Directory Structure

```
locale/
├── scripts/                  # PowerShell helper scripts
│   ├── Extract-Strings.ps1
│   ├── New-Language.ps1
│   ├── Update-Translations.ps1
│   ├── Compile-Translations.ps1
│   └── Get-CatalogName.ps1
├── messages.pot              # Template with all translatable strings
├── uk-UA/                    # Ukrainian translations
│   ├── Sic.po                # Source translation file
│   └── Sic.mo                # Compiled binary file
└── ...                       # Other language directories
```

## Notes

- MO files must be named to match the executable name (e.g., "Sic.mo")
- The catalog name is automatically detected from the project's AssemblyName
- The simplified structure does not require LC_MESSAGES subdirectories
- Only .mo files are copied to the build output (configured in .csproj)
- Language codes follow standard format: `language-COUNTRY` (e.g., `uk-UA`, `en-US`)
- All scripts are portable and use `Get-CatalogName.ps1` to detect the project name automatically
