<div align="right" dir="rtl">

# 🚀 התחלה מהירה — TalmudFormulas

מדריך זה יסביר איך להעביר את הפרויקט מ-zip ל-installer מוכן.

## שלב 1: דרישות

ודא שמותקנים על המחשב שלך:
- ✅ **Visual Studio 2022** (17.10 ומעלה) עם Workload "Windows App SDK"
- ✅ **.NET 8 SDK** — [הורד מ-microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ **Inno Setup 6.2+** — [הורד מ-jrsoftware.org](https://jrsoftware.org/isdl.php) (רק אם רוצים installers)
- ✅ **PowerShell 5.1+** (מובנה ב-Windows 10/11)

## שלב 2: השג את talmud.db

קובץ ה-DB גדול (~1GB) ולא נכלל בארכיון. אפשרויות:

### אפשרות א': בנה אותו מהפרויקט המקורי
1. הורד את [ACHI-GARCINAN/ACHI-GARCINAN](https://github.com/ACHI-GARCINAN/ACHI-GARCINAN) (Python)
2. הורץ את `migrate_db.py` כדי לייצר את `talmud.db`
3. העתק את הקובץ ל-`Assets\talmud.db`

### אפשרות ב': תן לinstaller להוריד אותו (גרסת Online)
תוכל לדלג על שלב זה ופשוט לבנות את ה-installer ב-Online (ראה שלב 4),
שיוריד את ה-DB אוטומטית בעת ההתקנה.

## שלב 3: בדוק שהכל עובד

```powershell
# פתח את הפרויקט ב-Visual Studio
start TalmudFormulas.sln

# או — קומפיל מ-CLI ובדוק
dotnet build
```

אם הבנייה עוברת בלי שגיאות — אתה מוכן ליצור installer.

## שלב 4: בנה installers

```powershell
# בנייה רגילה (x64) + שני installers
.\build.ps1

# האפשרויות שזמינות:
.\build.ps1 -Architecture x64        # רק x64 (ברירת מחדל)
.\build.ps1 -Architecture x86        # רק x86
.\build.ps1 -Architecture arm64      # רק ARM64
.\build.ps1 -All                     # כל הארכיטקטורות

.\build.ps1 -Variant Bundled         # רק installer עם DB מובנה
.\build.ps1 -Variant Online          # רק installer מורידים

.\build.ps1 -SkipInstaller           # קומפיל בלבד, ללא installer
.\build.ps1 -Clean                   # נקה לפני הבנייה
```

הפלט יישמר ב-`Release\`:
- `TalmudFormulas-Setup-Bundled-1.0.0.exe` (~600MB עם DB)
- `TalmudFormulas-Setup-Online-1.0.0.exe` (~50MB ללא DB)

## שלב 5: הגדרת כתובת ההורדה (לגרסת Online בלבד)

לפני שמפיצים את ה-installer ה-Online, יש לעדכן את כתובת ההורדה ב-`Installer\Common.iss`:

```pascal
#define DbDownloadUrl "https://github.com/<YOUR_USERNAME>/TalmudFormulas/releases/download/v1.0.0/talmud.db"
```

מומלץ להעלות את `talmud.db` כ-Release Asset ב-GitHub (או לאחסן ב-CDN/S3 אחר).

## שלב 6: הפצה

ה-installers הם קבצים עצמאיים. ניתן להפיץ אותם דרך:
- GitHub Releases
- WinGet (winget-pkgs)
- אתר אינטרנט פרטי
- USB / שיתוף קבצים

## פתרון בעיות

### "Bootstrap.Initialize failed"
ה-Windows App Runtime 1.7 חסר. הורד אותו מ:
https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-x64.exe

### "talmud.db not found"
- ודא שהקובץ נמצא ב-`Assets\talmud.db` (לבנייה Bundled)
- או שנתת installer Online שיוריד אותו

### Logs
ה-app כותב לוג שגיאות ב-`%APPDATA%\TalmudFormulas\errors.log`.

</div>
