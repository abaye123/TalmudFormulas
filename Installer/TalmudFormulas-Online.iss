; ============================================================================
;  TalmudFormulas-Online.iss
;  גרסת installer קלה (~50MB) שמורידה את talmud.db ישירות מהאינטרנט
;  במהלך תהליך ההתקנה.
;
;  בנייה:
;    iscc Installer\TalmudFormulas-Online.iss
;
;  פלט:
;    Release\TalmudFormulas-Setup-Online-1.0.0.exe
;
;  דורש: ISCC עם Inno Download Plugin (idp.iss) — ראה הוראות בתחתית הקובץ.
; ============================================================================

#include "Common.iss"

#define InstallerVariant "Online"

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
OutputBaseFilename=TalmudFormulas-Setup-Online-{#AppVersion}
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=110
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible arm64
ArchitecturesAllowed=x64compatible arm64 x86
MinVersion=10.0.17763
ShowLanguageDialog=no
UsePreviousAppDir=yes
UsePreviousLanguage=no
CloseApplications=force
RestartApplications=no
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} - Online Setup
VersionInfoProductName={#AppName}

[Languages]
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenu"; Description: "צור קיצור בתפריט התחל"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
; ── הקבצים העיקריים — בלי מסד הנתונים, יורד בנפרד ──

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

; x86
Source: "{#PublishX86Dir}\*"; DestDir: "{app}"; \
    Excludes: "*.pdb,*.xml,*.iobj,*.ipdb,Assets\talmud.db"; \
    Flags: ignoreversion recursesubdirs createallsubdirs; \
    Check: not Is64BitInstallMode

; ── אייקונים ──
Source: "{#AssetsDir}\AppIcon.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "{#AssetsDir}\AppIcon.png"; DestDir: "{app}\Assets"; Flags: ignoreversion

; ── רישיון וREADME ──
Source: "{#ProjectRoot}LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ProjectRoot}README.md"; DestDir: "{app}"; Flags: ignoreversion

; ── ה-DB יורד דינמית; נציב אותו במיקום ב-AfterInstall ──
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
Type: files; Name: "{app}\Assets\talmud.db"

; ============================================================================
;  הורדת talmud.db דרך BITS (Background Intelligent Transfer Service)
;  שיטה מובנית ב-Windows — לא דורש plugins חיצוניים, מציג התקדמות,
;  ניתן להמשיך מנקודת השבירה אם התקשרות נופלת.
; ============================================================================

[Code]
const
  DB_DOWNLOAD_URL = '{#DbDownloadUrl}';

var
  DownloadPage: TDownloadWizardPage;

function DbAlreadyExists: Boolean;
begin
  // אם המשתמש שם את ה-DB ידנית בתיקיית Assets לפני ההתקנה - נדלג על הורדה
  Result := FileExists(ExpandConstant('{#AssetsDir}\talmud.db'));
end;

procedure InitializeWizard;
begin
  // יצירת עמוד הורדה (Inno Setup 6.1+ תומך מובנית)
  DownloadPage := CreateDownloadPage(
    'מוריד את מסד הנתונים',
    'אנא המתן בזמן שמסד הנתונים מורד...' + #13#10 +
    '(גודל הקובץ: כ-{#DbExpectedSizeMB} מגה-בייט)',
    nil);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
begin
  if CurPageID = wpReady then
  begin
    if DbAlreadyExists then
    begin
      // הקובץ כבר קיים — נדלג על שלב ההורדה
      Result := True;
      Exit;
    end;

    DownloadPage.Clear;
    DownloadPage.Add(DB_DOWNLOAD_URL, 'talmud.db', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        Result := True;
      except
        if MsgBox(
          'שגיאה בהורדת מסד הנתונים:' + #13#10 +
          GetExceptionMessage + #13#10 + #13#10 +
          'האם ברצונך להמשיך בהתקנה גם בלי מסד הנתונים?' + #13#10 +
          'ניתן יהיה להוריד אותו ידנית מאוחר יותר ולהעתיק לתיקיית האפליקציה.',
          mbCriticalError, MB_YESNO) = IDYES then
        begin
          Result := True;
        end
        else
        begin
          Result := False;
        end;
      end;
    finally
      DownloadPage.Hide;
    end;
  end
  else
    Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  TempDb, TargetDb, AssetsDir: string;
begin
  if CurStep = ssPostInstall then
  begin
    // העתקת ה-DB שירד (אם ירד) לתיקיית Assets של האפליקציה
    TempDb := ExpandConstant('{tmp}\talmud.db');
    AssetsDir := ExpandConstant('{app}\Assets');
    TargetDb := AssetsDir + '\talmud.db';

    if not DirExists(AssetsDir) then
      ForceDirectories(AssetsDir);

    if FileExists(TempDb) then
    begin
      if not FileCopy(TempDb, TargetDb, False) then
      begin
        MsgBox('אזהרה: לא ניתן להעתיק את מסד הנתונים לתיקיית האפליקציה.' + #13#10 +
               'נסה להריץ את ההתקנה כמנהל מערכת.',
               mbError, MB_OK);
      end
      else
      begin
        DeleteFile(TempDb);
      end;
    end
    else if not DbAlreadyExists then
    begin
      // אם לא הורידו ולא הוצב ידנית — הצגת הוראות
      MsgBox(
        'מסד הנתונים לא הותקן.' + #13#10 + #13#10 +
        'כדי שהאפליקציה תעבוד, יש להוריד את הקובץ talmud.db' + #13#10 +
        'ולהציבו בתיקייה:' + #13#10 +
        AssetsDir,
        mbInformation, MB_OK);
    end;
  end;
end;
