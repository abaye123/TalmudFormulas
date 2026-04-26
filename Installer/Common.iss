; ============================================================================
;  Common.iss — הגדרות משותפות לשני סוגי המתקינים (Bundled / Online)
;  קובץ זה לא נבנה ישירות; הוא נכלל ע"י TalmudFormulas-Bundled.iss
;  ו-TalmudFormulas-Online.iss באמצעות #include.
; ============================================================================

#define AppName "סינופסיס תלמוד בבלי"
#define AppNameEn "TalmudFormulas"
#define AppPublisher "abaye"
#define AppVersion "1.0.0"
#define AppExeName "TalmudFormulas.exe"
#define AppId "{{8F3A4E50-9B2C-4C8E-9A1A-2D7C6F3A1B5E}"
#define AppURL "https://github.com/abaye123/TalmudFormulas"

; ----------------------------------------------------------------------------
;  קבועים — נתיבים יחסיים לקובץ ה-.iss (שנמצא בתיקייה Installer\)
; ----------------------------------------------------------------------------
#define ProjectRoot "..\"
#define PublishX64Dir ProjectRoot + "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
#define PublishX86Dir ProjectRoot + "bin\Release\net8.0-windows10.0.19041.0\win-x86\publish"
#define PublishArm64Dir ProjectRoot + "bin\Release\net8.0-windows10.0.19041.0\win-arm64\publish"
#define AssetsDir ProjectRoot + "Assets"
#define IconFile AssetsDir + "\AppIcon.ico"

; ----------------------------------------------------------------------------
;  כתובת ההורדה של talmud.db (לגרסת Online)
;  לעדכן לפני בנייה לכתובת ה-CDN/GitHub Releases הסופית
; ----------------------------------------------------------------------------
#define DbDownloadUrl "https://github.com/ACHI-GARCINAN/TalmudFormulas/releases/download/v1.0.0/talmud.db"
#define DbExpectedSizeMB "964"
