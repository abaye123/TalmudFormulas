# build.ps1 — סקריפט בנייה אוטומטי
#
# שימוש:
#   .\build.ps1                                  # בונה x64 + שני installers
#   .\build.ps1 -Architecture x86                # בונה רק x86
#   .\build.ps1 -Variant Bundled                 # בונה רק את הגרסה עם DB מובנה
#   .\build.ps1 -Variant Online                  # בונה רק את הגרסה ההמורידה
#   .\build.ps1 -SkipInstaller                   # בונה רק את הקבצים, ללא installer
#   .\build.ps1 -Clean                           # מנקה לפני הבנייה
#   .\build.ps1 -All                             # בונה את כל הארכיטקטורות
#
# דרישות:
#   - .NET 8 SDK
#   - Inno Setup 6.2+ (https://jrsoftware.org/isdl.php)
#
# פלט:
#   - bin\Release\net8.0-windows10.0.19041.0\<rid>\publish\
#   - Release\TalmudFormulas-Setup-Bundled-1.0.0.exe   (~600MB עם DB)
#   - Release\TalmudFormulas-Setup-Online-1.0.0.exe    (~50MB ללא DB)

param(
    [ValidateSet("x86", "x64", "arm64")]
    [string]$Architecture = "x64",

    [ValidateSet("Both", "Bundled", "Online")]
    [string]$Variant = "Both",

    [switch]$SkipInstaller,
    [switch]$Clean,
    [switch]$All
)

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
Set-Location $ProjectDir

function Write-Header($text) {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
}

function Write-Step($text) {
    Write-Host ""
    Write-Host "→ $text" -ForegroundColor Yellow
}

function Write-OK($text) {
    Write-Host "✓ $text" -ForegroundColor Green
}

function Write-Warn($text) {
    Write-Host "⚠ $text" -ForegroundColor Yellow
}

Write-Header "TalmudFormulas Build Script"
Write-Host "  Architecture: $Architecture"
Write-Host "  Variant: $Variant"

# ── בדיקת תלויות ──────────────────────────────
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Error ".NET SDK לא נמצא. נא להתקין .NET 8 SDK מ-https://dotnet.microsoft.com/download"
    exit 1
}
Write-OK ".NET SDK: $dotnetVersion"

$dbPath = Join-Path $ProjectDir "Assets\talmud.db"
$hasDb = Test-Path $dbPath
if (-not $hasDb) {
    Write-Warn "קובץ talmud.db לא נמצא ב-Assets\."
    Write-Warn "ה-Bundled installer ישאל את המשתמש אם להמשיך בכל זאת."
    if ($Variant -eq "Bundled") {
        Write-Host "  אם רוצים installer Bundled מלא — יש להעתיק את talmud.db ל-Assets\ ולהריץ שוב." -ForegroundColor Gray
    }
} else {
    $dbSize = (Get-Item $dbPath).Length / 1MB
    Write-OK "talmud.db: $($dbSize.ToString('N1')) MB"
}

# ── ניקוי ──────────────────────────────────
if ($Clean) {
    Write-Step "ניקוי תיקיות bin / obj / Release"
    Remove-Item -Recurse -Force "bin", "obj", "Release" -ErrorAction SilentlyContinue
    Write-OK "ניקוי הושלם"
}

# ── רשימת ארכיטקטורות לבנייה ──
$archs = if ($All) { @("x64", "x86", "arm64") } else { @($Architecture) }

# ── Restore + Publish ──
foreach ($arch in $archs) {
    $rid = "win-$arch"

    Write-Step "Restoring packages עבור $rid..."
    dotnet restore TalmudFormulas.csproj /p:RuntimeIdentifier=$rid
    if ($LASTEXITCODE -ne 0) { Write-Error "Restore failed עבור $rid"; exit 1 }

    Write-Step "Publishing עבור $rid..."
    dotnet publish TalmudFormulas.csproj `
        -c Release `
        -r $rid `
        --self-contained false `
        /p:Platform=$arch `
        /p:PublishReadyToRun=false `
        --nologo `
        --verbosity minimal

    if ($LASTEXITCODE -ne 0) { Write-Error "Publish failed עבור $rid"; exit 1 }

    $publishDir = "bin\Release\net8.0-windows10.0.19041.0\$rid\publish"
    if (-not (Test-Path "$publishDir\TalmudFormulas.exe")) {
        Write-Error "תיקיית publish לא נוצרה כראוי: $publishDir"
        exit 1
    }
    $exeSize = (Get-Item "$publishDir\TalmudFormulas.exe").Length / 1MB
    Write-OK "Publish ($rid): $publishDir (EXE: $($exeSize.ToString('N1')) MB)"
}

# ── Installers ──
if ($SkipInstaller) {
    Write-Step "דילוג על יצירת installers (-SkipInstaller)"
    exit 0
}

# חיפוש Inno Setup
$isccPaths = @(
    "iscc.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)
$iscc = $null
foreach ($p in $isccPaths) {
    if ($p -eq "iscc.exe") {
        if (Get-Command iscc.exe -ErrorAction SilentlyContinue) {
            $iscc = "iscc.exe"
            break
        }
    } elseif (Test-Path $p) {
        $iscc = $p
        break
    }
}

if (-not $iscc) {
    Write-Warn "Inno Setup 6 לא נמצא במסלולים המוכרים."
    Write-Host "  ניתן להוריד מ-https://jrsoftware.org/isdl.php" -ForegroundColor Gray
    Write-Host "  הקבצים שנבנו זמינים ב-bin\Release\..." -ForegroundColor Gray
    exit 0
}
Write-OK "Inno Setup: $iscc"

# וודא שתיקיית הפלט קיימת
New-Item -ItemType Directory -Force -Path "Release" | Out-Null

$variants = @()
if ($Variant -eq "Both" -or $Variant -eq "Bundled") {
    $variants += "Bundled"
}
if ($Variant -eq "Both" -or $Variant -eq "Online") {
    $variants += "Online"
}

foreach ($v in $variants) {
    $issPath = "Installer\TalmudFormulas-$v.iss"
    if (-not (Test-Path $issPath)) {
        Write-Warn "סקריפט installer חסר: $issPath"
        continue
    }

    Write-Step "Creating $v installer..."
    & $iscc "/Q" $issPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "$v installer creation failed"
        continue
    }

    $output = Get-ChildItem "Release\TalmudFormulas-Setup-$v-*.exe" `
        -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($output) {
        $size = $output.Length / 1MB
        Write-OK "$v installer: $($output.Name) — $($size.ToString('N1')) MB"
    }
}

Write-Header "✓ הבנייה הושלמה!"
Write-Host "  קבצי installer זמינים ב: Release\" -ForegroundColor Green

if (Test-Path "Release") {
    Get-ChildItem "Release\*.exe" | ForEach-Object {
        $size = $_.Length / 1MB
        Write-Host "    $($_.Name) ($($size.ToString('N1')) MB)" -ForegroundColor Green
    }
}
