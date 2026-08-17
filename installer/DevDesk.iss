#define MyAppName "DevDesk"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Mohammad Hadi Siavashi"
#define MyAppExeName "DevDesk.exe"
#ifndef PublishDir
  #define PublishDir AddBackslash(SourcePath) + "..\publish\win-x64"
#endif

[Setup]
AppId={{8F3A2C91-6D47-4B1E-9E5A-2C8F1D4A7B90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (C) {#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#SourcePath}..\publish\installer
OutputBaseFilename=DevDesk-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoCompany={#MyAppPublisher}
CloseApplications=yes
RestartApplications=yes
SetupLogging=yes
AllowNoIcons=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
procedure RemovePreviousPerUserInstall;
var
  PerUserDir: String;
begin
  PerUserDir := ExpandConstant('{localappdata}\Programs\DevDesk');
  if DirExists(PerUserDir) then
    DelTree(PerUserDir, True, True, True);

  DeleteFile(ExpandConstant('{userappdata}\Microsoft\Windows\Start Menu\Programs\DevDesk.lnk'));
  DeleteFile(ExpandConstant('{userdesktop}\DevDesk.lnk'));
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\DevDesk');
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not FileExists(ExpandConstant('{pf}\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe'))
     and not FileExists(ExpandConstant('{pf}\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe'))
     and not FileExists(ExpandConstant('{pf}\Microsoft SQL Server\140\Tools\Binn\SqlLocalDB.exe')) then
  begin
    MsgBox('DevDesk needs SQL Server Express LocalDB to run.'#13#10#13#10
      + 'You can still install the app, but start LocalDB before launching DevDesk.',
      mbInformation, MB_OK);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    RemovePreviousPerUserInstall;
end;
