; ============================================================================
;  HPX PIRATE - Inno Setup script
;  Produces:  HPX-PIRATE-Setup.exe
;
;  Build steps:
;    1) dotnet publish src/HPXPirate/HPXPirate.csproj -c Release -r win-x64 ^
;         --self-contained true -o publish
;    2) iscc installer\HPX-PIRATE.iss
;
;  Requirements:
;    - Inno Setup 6.x  (https://jrsoftware.org/isdl.php)
;    - The "publish" folder must exist next to the repo root (created in step 1)
;
;  NOTE: The build is SELF-CONTAINED. The .NET 8 runtime is bundled inside the
;  publish folder, so the target machine does NOT need .NET installed. This is
;  what makes the app work on a clean Windows 10 / Windows 11 install.
; ============================================================================

#define AppName        "HPX PIRATE"
#define AppShortName   "HPXPirate"
#define AppPublisher   "HPX Pirate"
#define AppVersion     "1.1.0"
#define AppExeName     "HPXPirate.exe"
#define AppId          "{{8F2A6C1E-9B4D-4E7A-A1C3-9E5D7B2F4C10}"
#define AppRepoURL     "https://github.com/hpxprojectx/HPX-PIRATE_WINDOWS"
; Publish output folder (relative to this .iss file -> ..\publish)
#define PublishDir     "..\publish"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppRepoURL}
AppSupportURL={#AppRepoURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=..\dist
OutputBaseFilename=HPX-PIRATE-Setup
SetupIconFile=..\src\HPXPirate\Assets\hpx.ico
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; The application manifest requests administrator privileges, so the installer
; should request them as well (needed for restore points / system tuning).
PrivilegesRequired=admin
MinVersion=10.0
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "indonesian"; MessagesFile: "compiler:Languages\Indonesian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; --- Main application (SELF-CONTAINED publish output: includes the .NET runtime) ---
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; --- Documentation ---
Source: "..\README.md";           DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "..\BUILD.md";            DestDir: "{app}"; Flags: ignoreversion
Source: "..\INSTALL-WINDOWS.md";  DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE";             DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";            Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";      Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: quicklaunchicon

[Run]
; Launch the app after install
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; User data lives in %APPDATA%\HPX Pirate and is intentionally preserved.
; Only the install directory is removed.

[Code]
function InitializeSetup(): Boolean;
begin
  // Self-contained build: no .NET runtime check needed.
  Result := True;
end;
