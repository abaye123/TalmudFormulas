using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;

namespace TalmudFormulas.Views.Controls;

/// <summary>
/// כרטיס תצוגה של עד נוסח בודד.
/// תומך בהצגת טקסט רגיל / מודגש / HTML מותאם (לתצוגת מילים עם הקשר).
/// </summary>
public sealed partial class WitnessCard : UserControl
{
    public string WitnessName { get; }
    public string? Text { get; }
    public string BaseText { get; private set; }
    public bool Highlight { get; private set; }
    public bool Clickable { get; private set; }
    public bool HideMinor { get; private set; }
    private bool _useCustomInlines;
    private Action<RichTextBlock>? _customRenderer;

    private string _fontFamily;
    private int _fontSize;
    private string _theme;
    private (Microsoft.UI.Xaml.Media.SolidColorBrush Accent, Microsoft.UI.Xaml.Media.SolidColorBrush Bg) _colorPair;

    public event EventHandler? Clicked;

    public WitnessCard(string name, string? text,
        Microsoft.UI.Xaml.Media.SolidColorBrush accentBrush,
        Microsoft.UI.Xaml.Media.SolidColorBrush backgroundBrush,
        string baseText, bool highlight, bool clickable,
        string fontFamily, int fontSize, string theme, bool hideMinor,
        Action<RichTextBlock>? customRenderer = null)
    {
        WitnessName = name;
        Text = text;
        BaseText = baseText;
        Highlight = highlight;
        Clickable = clickable;
        HideMinor = hideMinor;
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;
        _colorPair = (accentBrush, backgroundBrush);
        _customRenderer = customRenderer;
        _useCustomInlines = customRenderer is not null;

        InitializeComponent();

        NameText.Text = name;

        ApplyStyle();
        UpdateContent();

        if (clickable && !string.IsNullOrEmpty(text))
        {
            this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
                Microsoft.UI.Input.InputSystemCursorShape.Hand);
            // במצב Clickable לחיצה בכל מקום על הכרטיס (כולל הטקסט) צריכה
            // להפעיל את ה-Clicked. RichTextBlock עם IsTextSelectionEnabled
            // אוכל את הטאפים, אז מבטלים hit-test על הטקסט עצמו.
            ContentBlock.IsHitTestVisible = false;
            ContentBlock.IsTextSelectionEnabled = false;
        }
    }

    private void ApplyStyle()
    {
        var c = ThemeHelper.GetConfig(_theme);

        if (!string.IsNullOrEmpty(Text))
        {
            OuterBorder.Background = _colorPair.Bg;
            OuterBorder.BorderBrush = new SolidColorBrush(WithAlpha(_colorPair.Accent.Color, 60));
            NameBorder.BorderBrush = _colorPair.Accent;
            NameBorder.Background = new SolidColorBrush(Colors.Transparent);
            NameText.Foreground = _colorPair.Accent;
            AccentStrip.Background = _colorPair.Accent;
            AccentStrip.Visibility = Visibility.Visible;
        }
        else
        {
            // עדים ריקים - גוון ניטרלי שמכבד Light/Dark
            OuterBorder.Background = new SolidColorBrush(WithAlpha(Microsoft.UI.Colors.Gray, 25));
            OuterBorder.BorderBrush = new SolidColorBrush(WithAlpha(Microsoft.UI.Colors.Gray, 60));
            NameBorder.BorderBrush = new SolidColorBrush(WithAlpha(Microsoft.UI.Colors.Gray, 80));
            NameText.Foreground = new SolidColorBrush(WithAlpha(Microsoft.UI.Colors.Gray, 200));
            AccentStrip.Visibility = Visibility.Collapsed;
        }
    }

    private static Windows.UI.Color WithAlpha(Windows.UI.Color color, byte alpha)
        => Windows.UI.Color.FromArgb(alpha, color.R, color.G, color.B);

    private void UpdateContent()
    {
        ContentBlock.Blocks.Clear();
        ContentBlock.TextHighlighters.Clear();
        var c = ThemeHelper.GetConfig(_theme);

        if (string.IsNullOrEmpty(Text))
        {
            var p = new Paragraph();
            p.Inlines.Add(new Run
            {
                Text = "אין עד נוסח לקטע זה",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                FontSize = 12,
                Foreground = new SolidColorBrush(WithAlpha(Microsoft.UI.Colors.Gray, 200)),
            });
            ContentBlock.Blocks.Add(p);
            return;
        }

        if (_useCustomInlines && _customRenderer is not null)
        {
            _customRenderer(ContentBlock);
            return;
        }

        var paragraph = new Paragraph();

        if (Highlight && !string.IsNullOrEmpty(BaseText))
        {
            // ── Runs לכל סגמנט (להבדל ב-FontWeight) + TextHighlighter למסגרות צהובות ──
            var segments = DiffHelper.BuildDiffSegments(Text!, BaseText, HideMinor);
            int pos = 0;
            var highlighter = new TextHighlighter
            {
                Background = c.DiffYellowBrush,
                Foreground = new SolidColorBrush(ThemeHelper.ColorFromHex("#1A202C")),
            };
            foreach (var seg in segments)
            {
                paragraph.Inlines.Add(new Run
                {
                    Text = seg.Text,
                    FontFamily = new FontFamily(_fontFamily),
                    FontSize = _fontSize,
                    FontWeight = seg.IsHighlighted
                        ? Microsoft.UI.Text.FontWeights.SemiBold
                        : Microsoft.UI.Text.FontWeights.Normal,
                    Foreground = c.SectionTextBrush,
                });
                if (seg.IsHighlighted && seg.Text.Length > 0)
                {
                    highlighter.Ranges.Add(new TextRange { StartIndex = pos, Length = seg.Text.Length });
                }
                pos += seg.Text.Length;
            }
            if (highlighter.Ranges.Count > 0)
            {
                ContentBlock.TextHighlighters.Add(highlighter);
            }
        }
        else
        {
            paragraph.Inlines.Add(new Run
            {
                Text = Text!,
                FontFamily = new FontFamily(_fontFamily),
                FontSize = _fontSize,
                Foreground = c.SectionTextBrush,
            });
        }

        ContentBlock.Blocks.Add(paragraph);
    }

    public void UpdateTheme(
        Microsoft.UI.Xaml.Media.SolidColorBrush accent,
        Microsoft.UI.Xaml.Media.SolidColorBrush bg,
        string fontFamily, int fontSize, string theme, bool? hideMinor = null)
    {
        _colorPair = (accent, bg);
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;
        if (hideMinor.HasValue) HideMinor = hideMinor.Value;
        ApplyStyle();
        UpdateContent();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (Clickable && !string.IsNullOrEmpty(Text))
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
