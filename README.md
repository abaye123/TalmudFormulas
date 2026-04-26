<div align="right" dir="rtl">

# סינופסיס תלמוד בבלי — TalmudFormulas

> השוואת עדי נוסח של התלמוד הבבלי בממשק Windows מודרני, מבוסס WinUI 3 (.NET 8).

יציאה מקורית של הפרויקט [ACHI-GARCINAN/ACHI-GARCINAN](https://github.com/ACHI-GARCINAN/ACHI-GARCINAN) (Python/PyQt6) ל-Windows native, בסטייל הארכיטקטוני של Windows 11.

## ✨ פיצ'רים

- **תצוגת קטעים** — דף מלא של גמרא לפי דפוס וילנא, עם לחיצה על קטע להצגת עדי הנוסח השונים בצד.
- **תצוגת מילים** — מצב פורמטי שבו ניתן ללחוץ על מילה בודדת ולראות את ההקשר שלה ב-12 מילים לפני ואחרי בכל עד נוסח.
- **הדגשת שינויים** — סימון אוטומטי של מילים שונות בין דפוס וילנא לעד הנוסח, עם אופציה להסתיר "שינויים קלים" (גרשיים, חסר י' וכו').
- **חיפוש בדף** — חיפוש טקסטואלי עם דילוג בין תוצאות באמצעות Enter.
- **ניווט מהיר** — חיפוש "שבת ב" מנווט אוטומטית למסכת ולדף.
- **שתי ערכות נושא** — קלאסי (אפור-כחול בהיר) וצבעוני (חום-זהב כהה).
- **גופנים עברים** — תמיכה בכל גופן עברי מותקן במערכת, עם בחירת גודל 8-36.
- **תצוגה רציפה** — מצב המסיר מסגרות בין קטעים לתצוגה זורמת יותר.
- **תמיכה במקלדת** — חצים בתצוגת מילים, Enter בחיפושים.
- **ניווט במגע** — תמיכה בגלילה מסך-מגע.

### דרישות מערכת
- Windows 10 גרסה 1809 (build 17763) ומעלה / Windows 11
- ארכיטקטורה: x86, x64, או ARM64
- [Windows App Runtime 1.7](https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-x64.exe) (יותקן אוטומטית אם חסר)
- שטח דיסק פנוי: ~1.2GB

## 🛠 בנייה מהמקור

### בנייה אוטומטית (PowerShell)

הדרך הקלה ביותר היא להריץ את ה-build script:

```powershell
# בנייה x64 + שני installers (ברירת מחדל)
.\build.ps1

# בניית כל הארכיטקטורות (x86 + x64 + arm64)
.\build.ps1 -All

# רק גרסת Bundled
.\build.ps1 -Variant Bundled

# רק גרסת Online
.\build.ps1 -Variant Online

# בלי installer (רק קומפילציה)
.\build.ps1 -SkipInstaller

# ניקוי לפני הבנייה
.\build.ps1 -Clean
```

### בנייה ידנית

```bash
git clone https://github.com/abaye123/TalmudFormulas.git
cd TalmudFormulas

# הורדת חבילות
dotnet restore

# בנייה לדיבאג
dotnet build -c Debug

# פרסום ל-Release (x64)
dotnet publish -c Release -r win-x64 --self-contained false

# יצירת installer (דורש Inno Setup 6.2+)
iscc Installer\TalmudFormulas.iss
```

### דרישות פיתוח
- Visual Studio 2022 17.10+ עם workload "Windows App SDK"
- .NET 8 SDK
- Windows App SDK 1.7
- [Inno Setup 6.2+](https://jrsoftware.org/isdl.php) (לבניית installer)

## 📁 מבנה הפרויקט

```
TalmudFormulas/
├── Assets/                 — אייקונים ומסד נתונים
│   ├── AppIcon.ico
│   ├── AppIcon.png
│   └── talmud.db          — מסד הנתונים (לא נכלל ב-Git, ~1GB)
├── Helpers/                — כלי עזר
│   ├── DiffHelper.cs      — לוגיקת השוואת עדי נוסח
│   ├── ThemeHelper.cs     — ערכות נושא וצבעים
│   ├── FontsHelper.cs     — איתור גופנים עברים
│   └── WindowHelper.cs    — עבודה עם חלונות WinUI
├── Installer/              — Inno Setup
│   └── TalmudFormulas.iss
├── Models/                 — מודלים
│   └── Models.cs
├── Services/               — שירותים
│   ├── DatabaseService.cs — גישה ל-SQLite
│   ├── SettingsManager.cs — שמירת הגדרות
│   └── ErrorLogger.cs
├── Views/                  — חלונות ו-UserControls
│   ├── MainWindow.xaml
│   ├── SettingsDialog.xaml
│   └── Controls/
│       ├── SectionBlock.xaml
│       ├── WitnessCard.xaml
│       ├── WitnessPanel.xaml
│       ├── WordsView.xaml
│       ├── ClickableWord.cs
│       └── RtlFlowPanel.cs
├── App.xaml
├── App.xaml.cs
├── Program.cs              — נקודת כניסה עם Bootstrap
├── app.manifest
├── TalmudFormulas.csproj
└── TalmudFormulas.sln
```

## 🗄 מסד הנתונים

הקובץ `talmud.db` הוא מסד SQLite גדול (~1GB) שנבנה על ידי הסקריפט `migrate_db.py` של הפרויקט המקורי. הוא **לא** נכלל במאגר הקוד ויש להעתיק אותו לתיקיית `Assets/` לפני בנייה.

הפרויקט תומך גם בפורמט הישן (טבלת `sections_words_texts` עם עמודת `content`) וגם בפורמט החדש (טבלת `words` נפרדת).

## ⚖️ זכויות יוצרים

> כל החומר באפליקציה זו לוקט מתוכן השייך משפטית לאתר **"פרידברג – הכי גרסינן"**. השימוש בו מותר אך ורק לצורך שימוש פרטי ולא לצורך מסחרי.

תגובות והערות: talmud1239@gmail.com

## 📝 רישיון

MIT — ראה `LICENSE.txt`.

</div>
