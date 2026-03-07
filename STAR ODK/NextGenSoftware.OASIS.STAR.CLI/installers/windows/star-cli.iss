; Inno Setup script for OASIS STAR CLI (Windows)
; Build: Run publish-crossplatform.ps1 first, then compile this script with Inno Setup.
; Requires: Inno Setup 6 (https://jrsoftware.org/isinfo.php)

#define MyAppName "OASIS STAR CLI"
#define MyAppId "OASIS.STAR.CLI"
#define MyAppVersion "3.4.0"
#define MyAppPublisher "NextGen Software Ltd"
#define MyAppURL "https://github.com/NextGenSoftware/OASIS"
#define MyAppExe "star.exe"
; Publish output relative to this script (installers\windows): project dir is ..\..\
#define PublishDir "..\..\publish\win-x64"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\OASIS STAR CLI
DefaultGroupName=OASIS STAR CLI
DisableProgramGroupPage=yes
; Add install dir to system PATH so "star" works from any terminal
ChangesEnvironment=yes
OutputDir=..\..\publish\installers
OutputBaseFilename=star-cli-{#MyAppVersion}-win-x64
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64 arm64
ArchitecturesInstallIn64BitMode=x64 arm64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "addpath"; Description: "Add STAR CLI to system PATH"; GroupDescription: "Options:"

[Files]
Source: "{#PublishDir}\{#MyAppExe}"; DestDir: "{app}"; Flags: ignoreversion
; DNA folder next to star.exe (only JSON files; populated at publish by CLI csproj)
Source: "{#PublishDir}\DNA\*"; DestDir: "{app}\DNA"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Registry]
; Add install dir to PATH (only if task selected). Uses {olddata} to append.
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; Tasks: addpath
; Note: Uninstall does not remove from PATH (safe; user can edit PATH if needed).

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Comment: "OASIS STAR CLI"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
