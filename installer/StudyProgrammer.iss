#define AppName "StudyProgrammer"
#define AppVersion "0.1.0"
#define AppPublisher "StudyProgrammer"
#define AppExe "..\artifacts\StudyProgrammer-final-win-x64\StudyTracker.UI.exe"
#define AppIcon "..\src\StudyTracker.UI\Assets\StudyProgrammer.ico"

[Setup]
AppId={{8E462FC8-7A51-41BA-B460-C7A0B647A29B}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} MVP {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/jarher/programadorDeEstudios
AppSupportURL=https://github.com/jarher/programadorDeEstudios
AppUpdatesURL=https://github.com/jarher/programadorDeEstudios
DefaultDirName={localappdata}\Programs\StudyProgrammer
DefaultGroupName=StudyProgrammer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
UninstallDisplayIcon={app}\StudyTracker.UI.exe
SetupIconFile={#AppIcon}
OutputDir=..\artifacts\installer
OutputBaseFilename=StudyProgrammer-Setup-{#AppVersion}-win-x64
VersionInfoVersion=0.1.0.0
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\StudyProgrammer"; Filename: "{app}\StudyTracker.UI.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\StudyProgrammer"; Filename: "{app}\StudyTracker.UI.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\StudyTracker.UI.exe"; Description: "Iniciar StudyProgrammer"; Flags: nowait postinstall skipifsilent
