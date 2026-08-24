#define AppName "Photo Manager"
#define AppVersion "2.0.0"
#define Publisher "Mooli-web"
#define SourceDir "artifacts\win-x64"

[Setup]
AppId={{1EA5DE60-9DE9-467C-9860-BDE6B9E641C8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={localappdata}\Programs\Photo Manager
DefaultGroupName=Photo Manager
OutputDir=artifacts\installer
OutputBaseFilename=PhotoManager-Setup-{#AppVersion}-win-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
SetupIconFile=assets\icon.ico
UninstallDisplayIcon={app}\PhotoManager.exe
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\PhotoManager.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Photo Manager"; Filename: "{app}\PhotoManager.exe"
Name: "{autodesktop}\Photo Manager"; Filename: "{app}\PhotoManager.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\PhotoManager.exe"; Description: "Launch Photo Manager"; Flags: nowait postinstall skipifsilent
