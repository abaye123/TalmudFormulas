using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;
using TalmudFormulas.Models;

namespace TalmudFormulas.Views.Controls;

/// <summary>
/// הפאנל הצדדי של עדי הנוסח. מקביל ל-witness_panel.py.
/// תומך בשני מצבים:
/// 1. תצוגת קטע — מציג טקסט מלא של כל עד נוסח לקטע נבחר
/// 2. תצוגת מילה — מציג הקשר של מילה אחת בכל עד נוסח
/// </summary>
public sealed partial class WitnessPanel : UserControl
{
    // ── מצב ──────────────────────────────────
    private List<string> _witnesses = new();
    private Section? _currentSection;
    private string _currentPage = "";
    private string _baseText = "";
    private bool _wordMode;
    private List<WordEntry>? _wordsData;
    private int _wordIdx = -1;
    private string _mainWitness = "";

    // ── הגדרות ──────────────────────────────────
    private bool _suppressEvents = true;
    private bool? _pendingHighlight;
    private bool? _pendingHideEmpty;
    private bool? _pendingHideMinor;

    // טוקן לאישור רינדור — אם המשתמש לחץ על קטע אחר תוך כדי, הרינדור הקודם בטל
    private int _renderToken;

    public string Theme { get; set; } = "classic";
    public string FontFamilyName { get; set; } = "David";
    public int FontSizeValue { get; set; } = 16;

    public bool HighlightDiffs
    {
        get => HighlightCheck?.IsChecked == true;
        set
        {
            if (HighlightCheck is null)
            {
                _pendingHighlight = value;
                return;
            }
            _suppressEvents = true;
            HighlightCheck.IsChecked = value;
            HideMinorCheck.IsEnabled = value;
            _suppressEvents = false;
        }
    }

    public bool HideEmptyWitnesses
    {
        get => HideEmptyCheck?.IsChecked == true;
        set
        {
            if (HideEmptyCheck is null) { _pendingHideEmpty = value; return; }
            _suppressEvents = true;
            HideEmptyCheck.IsChecked = value;
            _suppressEvents = false;
        }
    }

    public bool HideMinorDiffs
    {
        get => HideMinorCheck?.IsChecked == true;
        set
        {
            if (HideMinorCheck is null) { _pendingHideMinor = value; return; }
            _suppressEvents = true;
            HideMinorCheck.IsChecked = value;
            _suppressEvents = false;
        }
    }

    public event EventHandler<string>? WitnessClicked;
    public event EventHandler? SettingsChanged;

    private bool _initialized = false;

    public WitnessPanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        // החלת ערכים שנקבעו לפני Loaded
        if (_pendingHighlight.HasValue)
        {
            HighlightCheck.IsChecked = _pendingHighlight.Value;
            _pendingHighlight = null;
        }
        if (_pendingHideEmpty.HasValue)
        {
            HideEmptyCheck.IsChecked = _pendingHideEmpty.Value;
            _pendingHideEmpty = null;
        }
        if (_pendingHideMinor.HasValue)
        {
            HideMinorCheck.IsChecked = _pendingHideMinor.Value;
            _pendingHideMinor = null;
        }

        _suppressEvents = false;
        HideMinorCheck.IsEnabled = HighlightCheck.IsChecked == true;
        HintLabel.Visibility = HighlightCheck.IsChecked == true && !_wordMode
            ? Visibility.Visible : Visibility.Collapsed;

        UpdateUiColors();
        ShowPlaceholder();
    }

    private void UpdateUiColors()
    {
        var c = ThemeHelper.GetConfig(Theme);

        if (Theme == ThemeHelper.ThemeColorful)
        {
            // ערכת ספר חמה - מחליפה את Mica ברקע אטום
            HeaderBorder.Background = c.PanelHeaderBgBrush;
            WitnessScroll.Background = c.MainBgBrush;
            WitnessContainer.Background = c.MainBgBrush;
            RootGrid.Background = c.MainBgBrush;
            HeaderLabel.Foreground = c.PanelHeaderTextBrush;
            HintLabelText.Foreground = c.PanelHintTextBrush;
            HighlightCheck.Foreground = c.PanelHeaderTextBrush;
            HideEmptyCheck.Foreground = c.PanelHeaderTextBrush;
            HideMinorCheck.Foreground = c.PanelHeaderTextBrush;
        }
        else
        {
            // system / classic - שקיפות כך ש-Mica יזרח
            HeaderBorder.Background = null;
            WitnessScroll.Background = null;
            WitnessContainer.Background = null;
            RootGrid.Background = null;
            HeaderLabel.ClearValue(TextBlock.ForegroundProperty);
            HintLabelText.ClearValue(TextBlock.ForegroundProperty);
            HighlightCheck.ClearValue(Control.ForegroundProperty);
            HideEmptyCheck.ClearValue(Control.ForegroundProperty);
            HideMinorCheck.ClearValue(Control.ForegroundProperty);
        }
        HeaderBorder.BorderBrush = c.PanelHeaderBorderBrush;
    }

    public void UpdateWitnesses(List<string> witnesses)
    {
        _witnesses = witnesses;
    }

    public void UpdateFont(string fontFamily, int fontSize, string theme)
    {
        FontFamilyName = fontFamily;
        FontSizeValue = fontSize;
        Theme = theme;
        UpdateUiColors();

        // רענון התצוגה הנוכחית
        if (_wordMode && _currentSection is not null && _wordsData is not null)
        {
            // לא רלוונטי במצב WordEntry — נשמור _wordsData לשימוש מאוחר
        }
        else if (_currentSection is not null)
        {
            ShowSection(_currentSection, _currentPage, _baseText);
        }

        if (_wordMode && _wordsData is not null && _wordIdx >= 0 && _wordIdx < _wordsData.Count)
        {
            ShowWord(_wordsData[_wordIdx], _currentPage, _mainWitness, _wordsData, _wordIdx);
        }
    }

    public void Reset()
    {
        _renderToken++; // לבטל deferred renders ממתינים
        _currentSection = null;
        _wordsData = null;
        _wordIdx = -1;
        ShowPlaceholder();
    }

    // ── אירועי checkboxes ──────────────────────────

    private void OnHighlightChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        HideMinorCheck.IsEnabled = HighlightDiffs;
        HintLabel.Visibility = HighlightDiffs && !_wordMode ? Visibility.Visible : Visibility.Collapsed;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    private void OnHideEmptyChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    private void OnHideMinorChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    private void Refresh()
    {
        if (_wordMode && _wordsData is not null && _wordIdx >= 0 && _wordIdx < _wordsData.Count)
        {
            ShowWord(_wordsData[_wordIdx], _currentPage, _mainWitness, _wordsData, _wordIdx);
        }
        else if (_currentSection is not null)
        {
            ShowSection(_currentSection, _currentPage, _baseText);
        }
    }

    // ── הצגה ──────────────────────────────────

    private void ShowPlaceholder()
        => BuildIllustration(
            glyph: "",
            title: "בחרו קטע לעדי הנוסח",
            subtitle: "לחיצה על קטע בטקסט המרכזי תציג כאן את כל עדי הנוסח שזמינים עבורו",
            muted: true);

    private void ShowNoWitnesses()
        => BuildIllustration(
            glyph: "",
            title: "אין עדי נוסח אחרים",
            subtitle: "לקטע הזה אין עדויות נוספות במאגר.\nניתן לעבור לקטע אחר או לבדוק את האפשרות \"הסתר עדים ריקים\".",
            muted: false);

    private void BuildIllustration(string glyph, string title, string subtitle, bool muted)
    {
        WitnessContainer.Children.Clear();

        var outer = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 18,
            Padding = new Thickness(28, 70, 28, 70),
        };

        // עיגול-רקע עם אייקון במרכז
        var iconCircle = new Border
        {
            Width = 88,
            Height = 88,
            CornerRadius = new CornerRadius(44),
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        if (Application.Current.Resources.TryGetValue("SubtleFillColorSecondaryBrush", out var bgVal)
            && bgVal is Brush bgBrush)
        {
            iconCircle.Background = bgBrush;
        }

        var icon = new FontIcon
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 36,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SolidColorBrush? accent = null;
        var accentKey = muted ? "AccentTextFillColorSecondaryBrush" : "AccentTextFillColorPrimaryBrush";
        if (Application.Current.Resources.TryGetValue(accentKey, out var accVal) && accVal is SolidColorBrush ab)
        {
            accent = ab;
        }
        if (accent is not null) icon.Foreground = accent;
        iconCircle.Child = icon;

        var titleBlock = new TextBlock
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        };

        var subtitleBlock = new TextBlock
        {
            Text = subtitle,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 280,
            Opacity = 0.7,
            LineHeight = 18,
        };

        if (Theme == ThemeHelper.ThemeColorful)
        {
            var c = ThemeHelper.GetConfig(Theme);
            icon.Foreground = c.AccentBrush;
            titleBlock.Foreground = c.PanelHeaderTextBrush;
            subtitleBlock.Foreground = c.PanelHintTextBrush;
            subtitleBlock.Opacity = 1.0;
            // עיגול-רקע חמים יותר
            iconCircle.Background = new SolidColorBrush(
                Windows.UI.Color.FromArgb(40, c.AccentColor.R, c.AccentColor.G, c.AccentColor.B));
        }

        outer.Children.Add(iconCircle);
        outer.Children.Add(titleBlock);
        outer.Children.Add(subtitleBlock);
        WitnessContainer.Children.Add(outer);
    }

    private void ShowLoading()
    {
        WitnessContainer.Children.Clear();

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 18,
            Padding = new Thickness(28, 90, 28, 80),
        };

        var ring = new ProgressRing
        {
            IsActive = true,
            Width = 44,
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var label = new TextBlock
        {
            Text = "טוען עדי נוסח...",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            FontSize = 13,
            Opacity = 0.7,
        };
        if (Theme == ThemeHelper.ThemeColorful)
        {
            var c = ThemeHelper.GetConfig(Theme);
            label.Foreground = c.PanelHintTextBrush;
            label.Opacity = 1.0;
        }

        stack.Children.Add(ring);
        stack.Children.Add(label);
        WitnessContainer.Children.Add(stack);
    }

    /// <summary>
    /// תצוגת קטע — מציג טקסט מלא של כל עד נוסח (פרט לעד הראשי).
    /// </summary>
    public void ShowSection(Section section, string page, string baseText)
    {
        var token = ++_renderToken;
        _currentSection = section;
        _currentPage = page;
        _baseText = baseText;
        _wordMode = false;
        _wordsData = null;
        _wordIdx = -1;

        HeaderLabel.Text = $"דף {page}  ·  {section.SectionLabel}";
        ShowLoading();

        // דחיית הרינדור הכבד לפריים הבא — נותן ל-loading state להופיע מיד
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (token != _renderToken) return; // המשתמש לחץ על משהו אחר בינתיים
            RenderSectionContent(section, page, baseText);
        });
    }

    private void RenderSectionContent(Section section, string page, string baseText)
    {
        WitnessContainer.Children.Clear();

        var hideEmpty = HideEmptyWitnesses;
        var highlight = HighlightDiffs;
        var hideMinor = HideMinorDiffs;
        int added = 0;

        for (int i = 0; i < _witnesses.Count; i++)
        {
            if (i == 0) continue; // וילנא — הטקסט המרכזי, לא מציגים שוב

            var witnessName = _witnesses[i];
            var text = section.Witnesses.GetValueOrDefault(witnessName);
            if (text == "None" || string.IsNullOrEmpty(text))
            {
                text = null;
            }

            if (text is null && hideEmpty) continue;

            var (accent, bg) = ThemeHelper.GetWitnessColor(Theme, i);
            var card = new WitnessCard(
                witnessName, text,
                new SolidColorBrush(accent), new SolidColorBrush(bg),
                baseText, highlight, highlight,
                FontFamilyName, FontSizeValue, Theme, hideMinor);

            if (highlight && !string.IsNullOrEmpty(text))
            {
                card.Clicked += (s, _) => WitnessClicked?.Invoke(this, witnessName);
            }

            WitnessContainer.Children.Add(card);
            added++;
        }

        if (added == 0)
        {
            ShowNoWitnesses();
        }

        WitnessScroll.ChangeView(null, 0, null, true);
    }

    /// <summary>
    /// תצוגת מילה — מציג הקשר של מילה ספציפית בכל עד נוסח.
    /// CONTEXT=12 מילים לפני ואחרי. המילה הנוכחית מודגשת.
    /// </summary>
    public void ShowWord(WordEntry wordEntry, string page, string mainWitness,
        List<WordEntry> wordsData, int wordIdx)
    {
        var token = ++_renderToken;
        _currentSection = null;
        _currentPage = page;
        _baseText = "";
        _wordMode = true;
        _wordsData = wordsData;
        _wordIdx = wordIdx;
        _mainWitness = mainWitness;

        var sectionLabel = wordEntry.SectionLabel;
        var mainText = wordEntry.Witnesses.GetValueOrDefault(mainWitness) ?? "";
        if (mainText == "None") mainText = "";
        HeaderLabel.Text = $"דף {page}  ·  {sectionLabel}  ·  מילה: {(string.IsNullOrEmpty(mainText) ? "—" : mainText)}";

        ShowLoading();

        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (token != _renderToken) return;
            RenderWordContent(wordEntry, page, mainWitness, wordsData, wordIdx);
        });
    }

    private void RenderWordContent(WordEntry wordEntry, string page, string mainWitness,
        List<WordEntry> wordsData, int wordIdx)
    {
        WitnessContainer.Children.Clear();

        const int CONTEXT = 12;

        var mainText = wordEntry.Witnesses.GetValueOrDefault(mainWitness) ?? "";
        if (mainText == "None") mainText = "";

        var vilnaWord = mainText.Trim();
        var hideEmpty = HideEmptyWitnesses;
        var highlight = HighlightDiffs;
        var hideMinor = HideMinorDiffs;
        int added = 0;

        for (int i = 0; i < _witnesses.Count; i++)
        {
            if (i == 0) continue;
            var witnessName = _witnesses[i];

            // ── איסוף ההקשר ──
            var beforeParts = new List<string>();
            for (int j = Math.Max(0, wordIdx - CONTEXT); j < wordIdx; j++)
            {
                var t = wordsData[j].Witnesses.GetValueOrDefault(witnessName) ?? "";
                if (t == "None") t = "";
                beforeParts.Add(string.IsNullOrEmpty(t) ? "—" : t);
            }

            var selText = wordsData[wordIdx].Witnesses.GetValueOrDefault(witnessName) ?? "";
            if (selText == "None") selText = "";
            var selectedDisplay = string.IsNullOrEmpty(selText) ? "—" : selText;

            var afterParts = new List<string>();
            for (int j = wordIdx + 1; j < Math.Min(wordsData.Count, wordIdx + CONTEXT + 1); j++)
            {
                var t = wordsData[j].Witnesses.GetValueOrDefault(witnessName) ?? "";
                if (t == "None") t = "";
                afterParts.Add(string.IsNullOrEmpty(t) ? "—" : t);
            }

            var hasAnyInContext = !string.IsNullOrEmpty(selText);
            if (!hasAnyInContext)
            {
                for (int j = Math.Max(0, wordIdx - CONTEXT);
                     j < Math.Min(wordsData.Count, wordIdx + CONTEXT + 1); j++)
                {
                    var t = (wordsData[j].Witnesses.GetValueOrDefault(witnessName) ?? "").Trim();
                    if (!string.IsNullOrEmpty(t) && t != "None")
                    {
                        hasAnyInContext = true;
                        break;
                    }
                }
            }

            if (!hasAnyInContext && hideEmpty) continue;

            // ── האם המילה הנבחרת שונה מוילנא? ──
            bool wordDiffers = false;
            bool isVilna = (witnessName == mainWitness);
            if (highlight && !isVilna)
            {
                var normSel = DiffHelper.NormalizeWord(selText);
                var normVil = DiffHelper.NormalizeWord(vilnaWord);
                bool missingInWitness = !string.IsNullOrEmpty(vilnaWord) && string.IsNullOrEmpty(selText);
                wordDiffers = missingInWitness ||
                              (!string.IsNullOrEmpty(selText) && (normSel != normVil));
                if (wordDiffers && !missingInWitness && hideMinor)
                {
                    if (DiffHelper.IsMinorDiff(selText, vilnaWord))
                    {
                        wordDiffers = false;
                    }
                }
            }

            // ── בנייה של הכרטיס עם custom renderer לטקסט בעל הקשר ──
            var (accent, bg) = ThemeHelper.GetWitnessColor(Theme, i);
            var beforeStr = string.Join(" ", beforeParts);
            var afterStr = string.Join(" ", afterParts);
            var capturedSelected = selectedDisplay;
            var capturedDiffers = wordDiffers;

            void Renderer(RichTextBlock rtb)
            {
                rtb.Blocks.Clear();
                rtb.TextHighlighters.Clear();
                var p = new Paragraph();
                var grayBrush = new SolidColorBrush(ThemeHelper.ColorFromHex("#888888"));
                var c2 = ThemeHelper.GetConfig(Theme);

                // ── שלוש Runs (לפני / נבחרת / אחרי) + TextHighlighter למסגרת הצהובה ──
                int pos = 0;
                int selectedStart = 0, selectedLen = 0;

                var beforeWithSpace = string.IsNullOrEmpty(beforeStr) ? "" : beforeStr + " ";
                if (beforeWithSpace.Length > 0)
                {
                    p.Inlines.Add(new Run
                    {
                        Text = beforeWithSpace,
                        FontFamily = new FontFamily(FontFamilyName),
                        FontSize = FontSizeValue,
                        Foreground = grayBrush,
                    });
                    pos += beforeWithSpace.Length;
                }

                selectedStart = pos;
                selectedLen = capturedSelected.Length;
                p.Inlines.Add(new Run
                {
                    Text = capturedSelected,
                    FontFamily = new FontFamily(FontFamilyName),
                    FontSize = FontSizeValue,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = c2.SectionTextBrush,
                });
                pos += selectedLen;

                var afterWithSpace = string.IsNullOrEmpty(afterStr) ? "" : " " + afterStr;
                if (afterWithSpace.Length > 0)
                {
                    p.Inlines.Add(new Run
                    {
                        Text = afterWithSpace,
                        FontFamily = new FontFamily(FontFamilyName),
                        FontSize = FontSizeValue,
                        Foreground = grayBrush,
                    });
                }

                rtb.Blocks.Add(p);

                // אם המילה הנבחרת שונה מוילנא — מוסיפים רקע צהוב על הטווח של המילה הנבחרת
                if (capturedDiffers && selectedLen > 0)
                {
                    var hl = new TextHighlighter
                    {
                        Background = new SolidColorBrush(ThemeHelper.ColorFromHex("#FFD700")),
                        Foreground = new SolidColorBrush(ThemeHelper.ColorFromHex("#000000")),
                    };
                    hl.Ranges.Add(new TextRange { StartIndex = selectedStart, Length = selectedLen });
                    rtb.TextHighlighters.Add(hl);
                }
            }

            var card = new WitnessCard(
                witnessName, "[ctx]",
                new SolidColorBrush(accent), new SolidColorBrush(bg),
                "", false, false,
                FontFamilyName, FontSizeValue, Theme, false,
                customRenderer: Renderer);

            WitnessContainer.Children.Add(card);
            added++;
        }

        if (added == 0)
        {
            ShowNoWitnesses();
        }

        WitnessScroll.ChangeView(null, 0, null, true);
    }
}
