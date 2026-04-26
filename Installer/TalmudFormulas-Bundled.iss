; ============================================================================
;  TalmudFormulas-Bundled.iss
;  גרסת installer הכוללת את talmud.db (~1GB) בתוך קובץ ההתקנה.
;  גודל installer סופי: ~600MB אחרי דחיסת LZMA2.
;
;  בנייה:
;    iscc Installer\TalmudFormulas-Bundled.iss
;
;  פלט:
;    Release\TalmudFormulas-Setup-Bundled-1.0.0.exe
; ============================================================================

#include "Common.iss"

#define InstallerVariant "Bundled"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}/releases
DefaultDirName={autopf}\{#AppNameEn}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
LicenseFile={#ProjectRoot}LICENSE.txt
OutputDir={#ProjectRoot}Release
OutputBaseFilename=TalmudFormulas-Setup-Bundled-{#AppVersion}
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
WizardStyle=modern
WizardSizePercent=110
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible arm64
ArchitecturesAllowed=x64compatible arm64 x86
MinVersion=10.0.17763
ShowLanguageDialog=no
DiskSpanning=no
UsePreviousAppDir=yes
UsePreviousLanguage=no
CloseApplications=force
RestartApplications=no
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} - Setup
VersionInfoProductName={#AppName}

[Languages]
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenu"; Description: "צור קיצור בתפריט התחל"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; ── הקבצים העיקריים — מפלט dotnet publish לפי ארכיטקטורה ──
; כברירת מחדל מבצעים התקנה x64. גרסאות x86/arm64 דורשות בנייה נפרדת.

; x64 (ברירת מחדל)
Source: "{#PublishX64Dir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb,*.xml,*.iobj,*.ipdb,Assets\talmud.db"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Check: Is64BitInstallMode and not IsArm64

; arm64
Source: "{#PublishArm64Dir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb,*.xml,*.iobj,*.ipdb,Assets\talmud.db"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Check: IsArm64

; x86 (32-bit)
Source: "{#PublishX86Dir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb,*.xml,*.iobj,*.ipdb,Assets\talmud.db"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Check: not Is64BitInstallMode

; ── מסד הנתונים — נכלל בתוך הinstaller ──
Source: "{#AssetsDir}\talmud.db"; DestDir: "{app}\Assets"; \
    Flags: ignoreversion; \
    Check: DbFileExists

; ── אייקונים ──
Source: "{#AssetsDir}\AppIcon.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "{#AssetsDir}\AppIcon.png"; DestDir: "{app}\Assets"; Flags: ignoreversion

; ── רישיון וREADME ──
Source: "{#ProjectRoot}LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectRoot}README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; \
    IconFilename: "{app}\Assets\AppIcon.ico"; Tasks: startmenu
Name: "{group}\הסר את {#AppName}"; Filename: "{uninstallexe}"; Tasks: startmenu
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; \
    IconFilename: "{app}\Assets\AppIcon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "הפעל את {#AppName}"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\TalmudFormulas"

[Code]
function DbFileExists: Boolean;
begin
  Result := FileExists(ExpandConstant('{#AssetsDir}\talmud.db'));
end;

function InitializeSetup(): Boolean;
begin
  if not DbFileExists then
  begin
    if MsgBox('קובץ מסד הנתונים talmud.db לא נמצא בתיקיית Assets.' + #13#10 + #13#10 +
             'התקנה תתקדם אך התוכנה לא תוכל להציג עדי נוסח עד שיועתק קובץ DB ידנית.' + #13#10 + #13#10 +
             'האם להמשיך בכל זאת?',
             mbConfirmation, MB_YESNO) <> IDYES then
    begin
      Result := False;
      Exit;
    end;
  end;
  Result := True;
end;
