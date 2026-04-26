using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;
using TalmudFormulas.Services;

namespace TalmudFormulas.Helpers;

/// <summary>
/// מנהל ערכות הנושא של האפליקציה.
/// תומך ב-3 ערכות: system (Fluent מערכת), classic (אור-Fluent בהיר), colorful (ספר חם).
/// </summary>
public static class ThemeHelper
{
    public const string ThemeSystem = "system";
    public const string ThemeClassic = "classic";
    public const string ThemeColorful = "colorful";

    private static string _currentTheme = ThemeSystem;
    private static UISettings? _uiSettings;

    public static string CurrentTheme => _currentTheme;

    public static event EventHandler<string>? ThemeChanged;

    /// <summary>
    /// מחזיר true אם המערכת ב-Dark mode (לערכת system).
    /// </summary>
    public static bool IsSystemDark()
    {
        try
        {
            _uiSettings ??= new UISettings();
            var bg = _uiSettings.GetColorValue(UIColorType.Background);
            // אם הרקע כהה => Dark mode
            return bg.R + bg.G + bg.B < 384;
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyTheme(string theme)
    {
        _currentTheme = theme;
        ThemeChanged?.Invoke(null, theme);

        // עדכן את ApplicationTheme הגלובלי כך ש-{ThemeResource} יחזיר את הצבעים הנכונים
        try
        {
            if (Application.Current is App app)
            {
                // לא ניתן להחליף ApplicationTheme בזמן ריצה ב-WinUI 3.
                // לכן נשתמש ב-RequestedTheme של ה-Window root במקום זאת
                // (זה נעשה ב-MainWindow.SetTheme).
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("ApplyTheme failed", ex);
        }
    }

    /// <summary>
    /// מחזיר את ה-ElementTheme המתאים לערכת הנושא הנוכחית.
    /// classic / system => מערכת, colorful => Light (כדי ששמירת הספר תיראה נכון).
    /// </summary>
    public static ElementTheme GetElementTheme(string theme)
    {
        return theme switch
        {
            ThemeColorful => ElementTheme.Light,
            ThemeClassic => ElementTheme.Light,
            _ => ElementTheme.Default,
        };
    }

    public static ThemeConfig GetConfig(string theme)
    {
        bool isDark = theme == ThemeSystem && IsSystemDark();
        return theme switch
        {
            ThemeColorful => CreateColorfulConfig(),
            ThemeClassic => CreateClassicConfig(isDark: false),
            _ => CreateClassicConfig(isDark: isDark),
        };
    }

    /// <summary>
    /// פלטה ניטרלית Fluent-style. עובדת מעל Mica - רקעי-קלף שקופים-חצי.
    /// </summary>
    private static ThemeConfig CreateClassicConfig(bool isDark) => isDark
        ? new ThemeConfig
        {
            // chrome - רקעים שקופים כדי ש-Mica יזרח
            MainBg = ColorFromHex("#00000000"),
            HeaderBg = ColorFromHex("#00000000"),
            HeaderText = ColorFromHex("#FFFFFF"),
            HeaderSubText = ColorFromHex("#B0B7BF"),
            AccentColor = ColorFromHex("#60CDFF"),
            AccentBorder = ColorFromHex("#60CDFF"),
            HoverBg = ColorFromHex("#22FFFFFF"),
            HoverText = ColorFromHex("#FFFFFF"),

            // search inputs
            SearchBg = ColorFromHex("#1AFFFFFF"),
            SearchText = ColorFromHex("#FFFFFF"),
            SearchBorder = ColorFromHex("#33FFFFFF"),
            SearchPlaceholder = ColorFromHex("#80FFFFFF"),

            // section card colors
            SectionTagBg = ColorFromHex("#26FFFFFF"),
            SectionTagText = ColorFromHex("#E0E4E9"),
            SectionText = ColorFromHex("#F2F4F7"),
            SectionNormalBg = ColorFromHex("#1FFFFFFF"),
            SectionNormalBorder = ColorFromHex("#26FFFFFF"),
            SectionHoverBg = ColorFromHex("#33FFFFFF"),
            SectionHoverBorder = ColorFromHex("#4DFFFFFF"),
            SectionHoverRight = ColorFromHex("#60CDFF"),
            SectionSelectedBg = ColorFromHex("#3360CDFF"),
            SectionSelectedBorder = ColorFromHex("#60CDFF"),
            SectionSelectedRight = ColorFromHex("#60CDFF"),
            SectionDiffBg = ColorFromHex("#33CC4444"),
            SectionDiffBorder = ColorFromHex("#FF8080"),
            SectionDiffRight = ColorFromHex("#FF6060"),

            // word cell
            WordNormalText = ColorFromHex("#F0F2F5"),
            WordHoverBg = ColorFromHex("#33FFFFFF"),
            WordHoverText = ColorFromHex("#FFFFFF"),
            WordSelectedBg = ColorFromHex("#60CDFF"),
            WordSelectedText = ColorFromHex("#000000"),
            WordMissingText = ColorFromHex("#80FFFFFF"),

            // panels
            PanelHeaderBg = ColorFromHex("#00000000"),
            PanelHeaderText = ColorFromHex("#F2F4F7"),
            PanelHeaderBorder = ColorFromHex("#26FFFFFF"),
            PanelHintText = ColorFromHex("#A0A8B0"),
            MasechetListBg = ColorFromHex("#00000000"),
            PageListBg = ColorFromHex("#00000000"),
            ListItemSelectedBg = ColorFromHex("#3360CDFF"),
            ListItemHoverBg = ColorFromHex("#1FFFFFFF"),

            DiffYellow = ColorFromHex("#FFD93D"),
            DiffRed = ColorFromHex("#FF6B6B"),
            WitnessAccent = ColorFromHex("#60CDFF"),
            WitnessBg = ColorFromHex("#1FFFFFFF"),
        }
        : new ThemeConfig
        {
            // chrome - רקעים שקופים-חצי כדי ש-Mica יזרח
            MainBg = ColorFromHex("#00000000"),
            HeaderBg = ColorFromHex("#00000000"),
            HeaderText = ColorFromHex("#1F1F1F"),
            HeaderSubText = ColorFromHex("#5C636B"),
            AccentColor = ColorFromHex("#005FB8"),
            AccentBorder = ColorFromHex("#005FB8"),
            HoverBg = ColorFromHex("#11000000"),
            HoverText = ColorFromHex("#1F1F1F"),

            // search inputs
            SearchBg = ColorFromHex("#80FFFFFF"),
            SearchText = ColorFromHex("#1F1F1F"),
            SearchBorder = ColorFromHex("#33000000"),
            SearchPlaceholder = ColorFromHex("#80000000"),

            // section card colors
            SectionTagBg = ColorFromHex("#1A005FB8"),
            SectionTagText = ColorFromHex("#005FB8"),
            SectionText = ColorFromHex("#1F1F1F"),
            SectionNormalBg = ColorFromHex("#CCFFFFFF"),
            SectionNormalBorder = ColorFromHex("#15000000"),
            SectionHoverBg = ColorFromHex("#FFFFFFFF"),
            SectionHoverBorder = ColorFromHex("#33005FB8"),
            SectionHoverRight = ColorFromHex("#005FB8"),
            SectionSelectedBg = ColorFromHex("#FFFFFFFF"),
            SectionSelectedBorder = ColorFromHex("#005FB8"),
            SectionSelectedRight = ColorFromHex("#005FB8"),
            SectionDiffBg = ColorFromHex("#FFFAF5"),
            SectionDiffBorder = ColorFromHex("#80E04040"),
            SectionDiffRight = ColorFromHex("#C92A2A"),

            // word cell
            WordNormalText = ColorFromHex("#1F1F1F"),
            WordHoverBg = ColorFromHex("#1A000000"),
            WordHoverText = ColorFromHex("#000000"),
            WordSelectedBg = ColorFromHex("#005FB8"),
            WordSelectedText = ColorFromHex("#FFFFFF"),
            WordMissingText = ColorFromHex("#80000000"),

            // panels
            PanelHeaderBg = ColorFromHex("#00000000"),
            PanelHeaderText = ColorFromHex("#1F1F1F"),
            PanelHeaderBorder = ColorFromHex("#15000000"),
            PanelHintText = ColorFromHex("#5C636B"),
            MasechetListBg = ColorFromHex("#00000000"),
            PageListBg = ColorFromHex("#00000000"),
            ListItemSelectedBg = ColorFromHex("#1A005FB8"),
            ListItemHoverBg = ColorFromHex("#11000000"),

            DiffYellow = ColorFromHex("#FFE066"),
            DiffRed = ColorFromHex("#C92A2A"),
            WitnessAccent = ColorFromHex("#005FB8"),
            WitnessBg = ColorFromHex("#F4F8FC"),
        };

    /// <summary>
    /// פלטה חמה לתחושת ספר ישן. הרקעים אטומים (לא שקופים) - מחליפים את Mica.
    /// </summary>
    private static ThemeConfig CreateColorfulConfig() => new()
    {
        MainBg = ColorFromHex("#FBF5E9"),
        HeaderBg = ColorFromHex("#F2E8D0"),
        HeaderText = ColorFromHex("#2A1A0A"),
        HeaderSubText = ColorFromHex("#7A5A30"),
        AccentColor = ColorFromHex("#8B4513"),
        AccentBorder = ColorFromHex("#A06A40"),
        HoverBg = ColorFromHex("#22A06A40"),
        HoverText = ColorFromHex("#2A1A0A"),

        SearchBg = ColorFromHex("#FFFDF8"),
        SearchText = ColorFromHex("#2A1A0A"),
        SearchBorder = ColorFromHex("#33A06A40"),
        SearchPlaceholder = ColorFromHex("#80805030"),

        SectionTagBg = ColorFromHex("#FFEFD5"),
        SectionTagText = ColorFromHex("#8B4513"),
        SectionText = ColorFromHex("#1A0800"),
        SectionNormalBg = ColorFromHex("#FFFDF8"),
        SectionNormalBorder = ColorFromHex("#22000000"),
        SectionHoverBg = ColorFromHex("#FFF7E8"),
        SectionHoverBorder = ColorFromHex("#33A06A40"),
        SectionHoverRight = ColorFromHex("#8B4513"),
        SectionSelectedBg = ColorFromHex("#FFFDF8"),
        SectionSelectedBorder = ColorFromHex("#8B4513"),
        SectionSelectedRight = ColorFromHex("#8B4513"),
        SectionDiffBg = ColorFromHex("#FFF8F0"),
        SectionDiffBorder = ColorFromHex("#80FF6B35"),
        SectionDiffRight = ColorFromHex("#CC3300"),

        WordNormalText = ColorFromHex("#1A0800"),
        WordHoverBg = ColorFromHex("#FFEFC8"),
        WordHoverText = ColorFromHex("#5A1A00"),
        WordSelectedBg = ColorFromHex("#8B4513"),
        WordSelectedText = ColorFromHex("#FFF5E0"),
        WordMissingText = ColorFromHex("#A08060"),

        PanelHeaderBg = ColorFromHex("#F2E8D0"),
        PanelHeaderText = ColorFromHex("#2A1A0A"),
        PanelHeaderBorder = ColorFromHex("#22000000"),
        PanelHintText = ColorFromHex("#7A5A30"),
        MasechetListBg = ColorFromHex("#F2E8D0"),
        PageListBg = ColorFromHex("#EFE0BF"),
        ListItemSelectedBg = ColorFromHex("#338B4513"),
        ListItemHoverBg = ColorFromHex("#22A06A40"),

        DiffYellow = ColorFromHex("#FFD700"),
        DiffRed = ColorFromHex("#C92A2A"),
        WitnessAccent = ColorFromHex("#5B3A8A"),
        WitnessBg = ColorFromHex("#F0ECF8"),
    };

    /// <summary>
    /// תומך גם בהקסה 6-תווים (RGB) וגם 8-תווים (ARGB).
    /// </summary>
    public static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 8)
        {
            return Color.FromArgb(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16),
                Convert.ToByte(hex.Substring(6, 2), 16));
        }
        if (hex.Length == 6)
        {
            return Color.FromArgb(255,
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
        return Color.FromArgb(255, 0, 0, 0);
    }

    public static SolidColorBrush BrushFromHex(string hex) => new(ColorFromHex(hex));

    /// <summary>
    /// פלטת מבטא לעדי נוסח - 8 צבעים מודרניים שעובדים עם Light או Dark.
    /// </summary>
    public static (Color Accent, Color Background) GetWitnessColor(string theme, int index)
    {
        if (theme == ThemeColorful)
        {
            var palette = new[]
            {
                ("#5B3A8A", "#F0ECF8"),
                ("#1A5E8A", "#EAF3FA"),
                ("#2E7A4A", "#E8F5EE"),
                ("#8A4A1A", "#FBF0E8"),
                ("#7A1A3A", "#F8EAF0"),
                ("#2A6A6A", "#E8F5F5"),
                ("#5A6A1A", "#F2F5E8"),
                ("#6A2A6A", "#F5E8F5"),
            };
            var p = palette[Math.Abs(index) % palette.Length];
            return (ColorFromHex(p.Item1), ColorFromHex(p.Item2));
        }

        bool dark = theme == ThemeSystem && IsSystemDark();
        if (dark)
        {
            var palette = new[]
            {
                ("#A6B5FF", "#1A2240"),
                ("#85D0FF", "#0F2638"),
                ("#A0E5C0", "#0F2A1F"),
                ("#FFC58A", "#3A2410"),
                ("#FF9FB3", "#3A1622"),
                ("#7FE6E6", "#0F2A2A"),
                ("#D4E48A", "#262E10"),
                ("#E0A8E0", "#2E1430"),
            };
            var p = palette[Math.Abs(index) % palette.Length];
            return (ColorFromHex(p.Item1), ColorFromHex(p.Item2));
        }
        else
        {
            var palette = new[]
            {
                ("#3D5AFE", "#EEF1FF"),
                ("#0288D1", "#E5F3FB"),
                ("#2E7D32", "#E8F5E9"),
                ("#E65100", "#FFF3E0"),
                ("#C2185B", "#FCE4EC"),
                ("#00838F", "#E0F2F1"),
                ("#827717", "#F4F8E8"),
                ("#6A1B9A", "#F3E5F5"),
            };
            var p = palette[Math.Abs(index) % palette.Length];
            return (ColorFromHex(p.Item1), ColorFromHex(p.Item2));
        }
    }

    /// <summary>
    /// מנסה להחיל Mica backdrop על חלון WinUI 3.
    /// אם לא נתמך - מחזיר false והקוד הקורא ייחזור לרקע רגיל.
    /// </summary>
    public static bool TrySetMica(Window window)
    {
        try
        {
            window.SystemBackdrop = new MicaBackdrop
            {
                Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base,
            };
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("TrySetMica failed", ex);
            return false;
        }
    }
}

/// <summary>
/// קונפיגורציה מלאה של ערכת נושא.
/// </summary>
public class ThemeConfig
{
    public Color MainBg { get; set; }
    public Color HeaderBg { get; set; }
    public Color HeaderText { get; set; }
    public Color HeaderSubText { get; set; }
    public Color AccentColor { get; set; }
    public Color AccentBorder { get; set; }
    public Color HoverBg { get; set; }
    public Color HoverText { get; set; }
    public Color SearchBg { get; set; }
    public Color SearchText { get; set; }
    public Color SearchBorder { get; set; }
    public Color SearchPlaceholder { get; set; }
    public Color SectionTagBg { get; set; }
    public Color SectionTagText { get; set; }
    public Color SectionText { get; set; }
    public Color SectionNormalBg { get; set; }
    public Color SectionNormalBorder { get; set; }
    public Color SectionHoverBg { get; set; }
    public Color SectionHoverBorder { get; set; }
    public Color SectionHoverRight { get; set; }
    public Color SectionSelectedBg { get; set; }
    public Color SectionSelectedBorder { get; set; }
    public Color SectionSelectedRight { get; set; }
    public Color SectionDiffBg { get; set; }
    public Color SectionDiffBorder { get; set; }
    public Color SectionDiffRight { get; set; }
    public Color WordNormalText { get; set; }
    public Color WordHoverBg { get; set; }
    public Color WordHoverText { get; set; }
    public Color WordSelectedBg { get; set; }
    public Color WordSelectedText { get; set; }
    public Color WordMissingText { get; set; }
    public Color PanelHeaderBg { get; set; }
    public Color PanelHeaderText { get; set; }
    public Color PanelHeaderBorder { get; set; }
    public Color PanelHintText { get; set; }
    public Color MasechetListBg { get; set; }
    public Color PageListBg { get; set; }
    public Color ListItemSelectedBg { get; set; }
    public Color ListItemHoverBg { get; set; }
    public Color DiffYellow { get; set; }
    public Color DiffRed { get; set; }
    public Color WitnessAccent { get; set; }
    public Color WitnessBg { get; set; }

    public SolidColorBrush MainBgBrush => new(MainBg);
    public SolidColorBrush HeaderBgBrush => new(HeaderBg);
    public SolidColorBrush HeaderTextBrush => new(HeaderText);
    public SolidColorBrush HeaderSubTextBrush => new(HeaderSubText);
    public SolidColorBrush AccentBrush => new(AccentColor);
    public SolidColorBrush AccentBorderBrush => new(AccentBorder);
    public SolidColorBrush HoverBgBrush => new(HoverBg);
    public SolidColorBrush HoverTextBrush => new(HoverText);
    public SolidColorBrush SearchBgBrush => new(SearchBg);
    public SolidColorBrush SearchTextBrush => new(SearchText);
    public SolidColorBrush SearchBorderBrush => new(SearchBorder);
    public SolidColorBrush SearchPlaceholderBrush => new(SearchPlaceholder);
    public SolidColorBrush SectionTagBgBrush => new(SectionTagBg);
    public SolidColorBrush SectionTagTextBrush => new(SectionTagText);
    public SolidColorBrush SectionTextBrush => new(SectionText);
    public SolidColorBrush SectionNormalBgBrush => new(SectionNormalBg);
    public SolidColorBrush SectionNormalBorderBrush => new(SectionNormalBorder);
    public SolidColorBrush SectionHoverBgBrush => new(SectionHoverBg);
    public SolidColorBrush SectionHoverBorderBrush => new(SectionHoverBorder);
    public SolidColorBrush SectionHoverRightBrush => new(SectionHoverRight);
    public SolidColorBrush SectionSelectedBgBrush => new(SectionSelectedBg);
    public SolidColorBrush SectionSelectedBorderBrush => new(SectionSelectedBorder);
    public SolidColorBrush SectionSelectedRightBrush => new(SectionSelectedRight);
    public SolidColorBrush SectionDiffBgBrush => new(SectionDiffBg);
    public SolidColorBrush SectionDiffBorderBrush => new(SectionDiffBorder);
    public SolidColorBrush SectionDiffRightBrush => new(SectionDiffRight);
    public SolidColorBrush WordNormalTextBrush => new(WordNormalText);
    public SolidColorBrush WordHoverBgBrush => new(WordHoverBg);
    public SolidColorBrush WordHoverTextBrush => new(WordHoverText);
    public SolidColorBrush WordSelectedBgBrush => new(WordSelectedBg);
    public SolidColorBrush WordSelectedTextBrush => new(WordSelectedText);
    public SolidColorBrush WordMissingTextBrush => new(WordMissingText);
    public SolidColorBrush PanelHeaderBgBrush => new(PanelHeaderBg);
    public SolidColorBrush PanelHeaderTextBrush => new(PanelHeaderText);
    public SolidColorBrush PanelHeaderBorderBrush => new(PanelHeaderBorder);
    public SolidColorBrush PanelHintTextBrush => new(PanelHintText);
    public SolidColorBrush MasechetListBgBrush => new(MasechetListBg);
    public SolidColorBrush PageListBgBrush => new(PageListBg);
    public SolidColorBrush ListItemSelectedBgBrush => new(ListItemSelectedBg);
    public SolidColorBrush ListItemHoverBgBrush => new(ListItemHoverBg);
    public SolidColorBrush DiffYellowBrush => new(DiffYellow);
    public SolidColorBrush DiffRedBrush => new(DiffRed);
}
