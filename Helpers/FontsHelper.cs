using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TalmudFormulas.Services;

namespace TalmudFormulas.Helpers;

/// <summary>
/// כלי לאיתור גופנים עברים מותקנים. מקביל ל-get_hebrew_fonts() ב-settings_dialog.py.
/// </summary>
public static class FontsHelper
{
    private static List<string>? _cache;

    /// <summary>
    /// מחזיר רשימת גופנים שתומכים בעברית. מטמון לאחר טעינה ראשונה.
    /// </summary>
    public static List<string> GetHebrewFonts()
    {
        if (_cache is not null) return _cache;

        var fonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // נשתמש ב-GDI EnumFontFamilies דרך PInvoke
            EnumerateInstalledFonts(fonts);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("EnumerateInstalledFonts failed", ex);
        }

        // אם לא הצלחנו — fallback לרשימה ידועה
        if (fonts.Count == 0)
        {
            foreach (var f in DefaultHebrewFonts)
            {
                fonts.Add(f);
            }
        }
        else
        {
            // נוסיף גם את אלה שתמיד אמורים להיות
            foreach (var f in DefaultHebrewFonts)
            {
                fonts.Add(f);
            }
        }

        _cache = fonts.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        return _cache;
    }

    private static readonly string[] DefaultHebrewFonts = new[]
    {
        "David", "Frank Ruehl CLM", "Frank Ruehl", "FrankRuehl", "Miriam",
        "Narkisim", "Rod", "Levenim MT", "Times New Roman",
        "Arial", "Tahoma", "Segoe UI", "Calibri",
    };

    // ── PInvoke ל-EnumFontFamiliesEx ──────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct LOGFONT
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName;
    }

    private const byte HEBREW_CHARSET = 177;
    private const byte DEFAULT_CHARSET = 1;

    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    private static extern int EnumFontFamiliesEx(IntPtr hdc, ref LOGFONT lpLogfont,
        EnumFontExDelegate lpEnumFontFamExProc, IntPtr lParam, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private delegate int EnumFontExDelegate(ref LOGFONT lpelfe, IntPtr lpntme,
        uint FontType, IntPtr lParam);

    private static void EnumerateInstalledFonts(HashSet<string> fonts)
    {
        var hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero) return;

        try
        {
            var lf = new LOGFONT
            {
                lfCharSet = HEBREW_CHARSET, // רק גופנים שתומכים בעברית
                lfFaceName = "",
            };

            int Callback(ref LOGFONT lpelfe, IntPtr _, uint __, IntPtr ___)
            {
                var name = lpelfe.lfFaceName;
                if (!string.IsNullOrEmpty(name) && !name.StartsWith("@"))
                {
                    fonts.Add(name);
                }
                return 1;
            }

            EnumFontFamiliesEx(hdc, ref lf, Callback, IntPtr.Zero, 0);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }
    }
}
