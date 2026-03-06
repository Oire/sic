; SIC! InnoSetup Installer Script
; Copyright © 2026 Oire Software SARL.

#define MyAppName "SIC!"
#define MyAppPublisher "Oire Software SARL"
#define MyAppCompany "Oire"
#define MyAppFolderName "Sic"
#define MyAppURL "https://oire.org/"
#define MyAppExeName "sic.exe"
#define MyAppDescription "Simple Image Converter"

; Source paths (relative to installer script location)
#define SourcePath "..\src\Sic\bin\x64\Release\win-x64\publish"

; Get version from the compiled executable
#define MyAppVersion GetVersionNumbersString(SourcePath + "\" + MyAppExeName)

[Setup]
; Application information
AppId={{B2702091-8CAD-441A-9D7C-5E8EFFDAB4A5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright © 2026 {#MyAppPublisher}.

; Installation directory
DefaultDirName={autopf}\{#MyAppCompany}\{#MyAppName}
DefaultGroupName={#MyAppCompany}\{#MyAppName}
AllowNoIcons=yes

; Output configuration
OutputDir=Output
OutputBaseFilename={#MyAppName}-V{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Uninstall configuration
UninstallDisplayName={#MyAppName} {#MyAppVersion}

; System requirements
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; License file
LicenseFile=..\LICENSE

; Icons
SetupIconFile=..\src\Sic\sic.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Privileges
PrivilegesRequired=admin
DisableProgramGroupPage=yes

; Language options
ShowLanguageDialog=yes

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl,Languages\Custom.en.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl,Languages\Custom.fr.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl,Languages\Custom.de.isl"  
Name: "uk"; MessagesFile: "compiler:Languages\Ukrainian.isl,Languages\Custom.uk.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl,Languages\Custom.ru.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourcePath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\Magick.Native-Q16-x64.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourcePath}\help\*"; DestDir: "{app}\help"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourcePath}\locale\*"; DestDir: "{app}\locale"; Flags: ignoreversion recursesubdirs createallsubdirs

; Icons
Source: "..\src\Sic\sic.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\sic.ico"; Comment: "{cm:AppDescription}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\sic.ico"; Comment: "{cm:AppDescription}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

#include "CodeDependencies.iss"

[Code]
// Function to get file version
function GetFileVersion(const FileName: string): string;
var
  MS, LS: Cardinal;
begin
  Result := '';
  if GetVersionNumbers(FileName, MS, LS) then
    Result := IntToStr(MS shr 16) + '.' + IntToStr(MS and $FFFF) + '.' + 
              IntToStr(LS shr 16) + '.' + IntToStr(LS and $FFFF);
  if Result = '' then
    Result := '0.0.0.0';
end;

// Function to compare version strings
function CompareVersions(const V1, V2: string): Integer;
var
  P1, P2: Integer;
  N1, N2: Integer;
  S1, S2: string;
begin
  Result := 0;
  S1 := V1;
  S2 := V2;
  
  while (S1 <> '') or (S2 <> '') do
  begin
    // Extract next version component
    P1 := Pos('.', S1);
    if P1 = 0 then P1 := Length(S1) + 1;
    P2 := Pos('.', S2);
    if P2 = 0 then P2 := Length(S2) + 1;
    
    // Convert to numbers
    N1 := StrToIntDef(Copy(S1, 1, P1 - 1), 0);
    N2 := StrToIntDef(Copy(S2, 1, P2 - 1), 0);
    
    // Compare
    if N1 > N2 then
    begin
      Result := 1;
      Exit;
    end
    else if N1 < N2 then
    begin
      Result := -1;
      Exit;
    end;
    
    // Remove processed component
    Delete(S1, 1, P1);
    Delete(S2, 1, P2);
  end;
end;

function InitializeSetup: Boolean;
begin
  // Force x64 dependencies since our app is 64-bit only
  Dependency_ForceX86 := False;
  
  // Add .NET 8.0 Desktop Runtime dependency
  Dependency_AddDotNet80Desktop;
  
  Result := True;
end;

// InitializeUninstall is called after the default confirmation dialog
// We can use it for additional checks if needed
function InitializeUninstall(): Boolean;
begin
  Result := True;
end;

// Remove user data on uninstall (optional)
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  UserDataPath: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    UserDataPath := ExpandConstant('{userappdata}\{#MyAppCompany}\{#MyAppFolderName}');
    if DirExists(UserDataPath) then
    begin
      if MsgBox(CustomMessage('RemoveUserData'), 
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(UserDataPath, True, True, True);
        RemoveDir(ExpandConstant('{userappdata}\{#MyAppCompany}'));
      end;
    end;
  end;
end;