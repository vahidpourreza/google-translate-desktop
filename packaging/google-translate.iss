#ifndef AppVersion
  #error AppVersion is required; use scripts/release.ps1.
#endif
#ifndef PublishDir
  #error PublishDir is required.
#endif
#ifndef ReleaseDir
  #error ReleaseDir is required.
#endif
#ifndef SourceRoot
  #error SourceRoot is required.
#endif
#ifndef OutputName
  #define OutputName "GoogleTranslate-Setup-" + AppVersion + "-win-x64"
#endif

[Setup]
#ifdef ValidationBuild
AppId={{3945D878-36B4-45D8-A221-404EA525189E}
AppName=Google Translate Installer Validation
CreateUninstallRegKey=no
AppMutex=Local\GoogleTranslate-installer-validation-only
#else
AppId={{1DC717AE-65CE-45E3-AF2E-FC2DA15D15B6}
AppName=Google Translate Desktop
AppMutex=Local\GoogleTranslate.Singleton.v3
#endif
AppVersion={#AppVersion}
AppPublisher=vahidpourreza
AppPublisherURL=https://github.com/vahidpourreza/google-translate-desktop
AppSupportURL=https://github.com/vahidpourreza/google-translate-desktop/issues
AppUpdatesURL=https://github.com/vahidpourreza/google-translate-desktop/releases
VersionInfoVersion={#AppVersion}
VersionInfoCompany=vahidpourreza
VersionInfoDescription=Google Translate desktop companion setup
VersionInfoProductName=Google Translate Desktop Companion
DefaultDirName={localappdata}\Programs\GoogleTranslate
DefaultGroupName=Google Translate
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#ReleaseDir}
OutputBaseFilename={#OutputName}
SetupIconFile={#SourceRoot}\assets\translate.ico
UninstallDisplayIcon={app}\GoogleTranslate.exe
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes
#ifdef ValidationBuild
CloseApplications=no
#else
CloseApplications=yes
CloseApplicationsFilter=GoogleTranslate.exe
#endif
RestartApplications=no
AllowNoIcons=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
Uninstallable=yes
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"
Name: "autostart"; Description: "Start quietly with Windows so global shortcuts are ready"; GroupDescription: "Background companion:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

#ifndef ValidationBuild
[Icons]
Name: "{autoprograms}\Google Translate"; Filename: "{app}\GoogleTranslate.exe"; WorkingDir: "{app}"; AppUserModelID: "Personal.GoogleTranslate.Desktop.v1"
Name: "{autodesktop}\Google Translate"; Filename: "{app}\GoogleTranslate.exe"; WorkingDir: "{app}"; AppUserModelID: "Personal.GoogleTranslate.Desktop.v1"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "GoogleTranslate"; ValueData: """{app}\GoogleTranslate.exe"" --background"; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\GoogleTranslate.exe"; Description: "Launch Google Translate"; Flags: nowait postinstall skipifsilent unchecked
#endif

#ifndef ValidationBuild
[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'GoogleTranslate');
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'TranslateDesktop');
  end;
end;
#endif
