#define MyAppName "Photo Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Mooli-web"
#define MyAppExeName "PhotoManager.exe"

[Setup]
AppId={{B95DDA86-A129-4CE2-9E70-645617AFE4BD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Photo Manager
DefaultGroupName=Photo Manager
OutputDir=installer-output
OutputBaseFilename=PhotoManager-Setup-{#MyAppVersion}-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=assets\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "dist\PhotoManager.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Photo Manager"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Photo Manager"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Photo Manager"; Flags: nowait postinstall skipifsilent
