using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;
using TalmudFormulas.Models;

namespace TalmudFormulas.Views.Controls;

/// <summary>
/// בלוק תצוגה של קטע בודד בדף. מקביל ל-section_block.py.
/// תומך ב: בחירה, hover, הצגת diff מול עד אחר, חיפוש בדף.
/// </summary>
public sealed partial class SectionBlock : UserControl
{
    public Section Section { get; }
    public string MainWitness { get; private set; }
    public bool HasSearchMatch { get; private set; }

    private string _fontFamily;
    private int _fontSize;
    private string _theme;
    private bool _continuousView;
    private string? _activeDiffWitness;
    private string _plainText;

    private bool _isSelected;
    private bool _isPointerOver;

    public event EventHandler? Clicked;

    public SectionBlock(Section section, string mainWitness,
        string fontFamily, int fontSize, string theme, bool continuousView)
    {
        Section = section;
        MainWitness = mainWitness;
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;
        _continuousView = continuousView;

        var raw = section.Witnesses.GetValueOrDefault(mainWitness) ?? "";
        _plainText = raw;

        InitializeComponent();

        // מצביע יד בעת ריחוף — מסמן שהקטע ניתן ללחיצה
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
            Microsoft.UI.Input.InputSystemCursorShape.Hand);

        TagText.Text = section.SectionLabel;

        if (_continuousView)
        {
            TagBorder.Visibility = Visibility.Collapsed;
            OuterBorder.Margin = new Thickness(4, 0, 4, 0);
            OuterBorder.Padding = new Thickness(18, 6, 18, 6);
            OuterBorder.CornerRadius = new CornerRadius(0);
        }

        UpdateColors();
        UpdateTextContent();
    }

    private void UpdateColors()
    {
        var c = ThemeHelper.GetConfig(_theme);
        TagBorder.Background = c.SectionTagBgBrush;
        TagText.Foreground = c.SectionTagTextBrush;
        ApplyBorderStyle();
    }

    private void ApplyBorderStyle()
    {
        var c = ThemeHelper.GetConfig(_theme);
        SolidColorBrush bg, border;
        Thickness thickness;

        if (!string.IsNullOrEmpty(_activeDiffWitness))
        {
            bg = c.SectionDiffBgBrush;
            border = c.SectionDiffBorderBrush;
            // border-right=4 ב-RTL זה הצד הימני בפועל - בלוגית "התחלה" של RTL
            // ב-WinUI עם FlowDirection=RTL, "Right" של Thickness זה ימין הפיזי.
            // נשתמש ב-Thickness עם הצד הימני עבה יותר (ימין = 4 ב-RTL).
            thickness = new Thickness(1, _continuousView ? 1 : 1, 4, _continuousView ? 0 : 1);
        }
        else if (_isSelected)
        {
            bg = c.SectionSelectedBgBrush;
            border = c.SectionSelectedBorderBrush;
            thickness = new Thickness(1, _continuousView ? 1 : 1, 4, _continuousView ? 0 : 1);
        }
        else if (_isPointerOver)
        {
            bg = c.SectionHoverBgBrush;
            border = c.SectionHoverBorderBrush;
            thickness = new Thickness(1, _continuousView ? 1 : 1, 4, _continuousView ? 0 : 1);
        }
        else
        {
            bg = c.SectionNormalBgBrush;
            border = c.SectionNormalBorderBrush;
            thickness = _continuousView
                ? new Thickness(0, 1, 0, 0)
                : new Thickness(1);
        }

        OuterBorder.Background = bg;
        OuterBorder.BorderBrush = border;
        OuterBorder.BorderThickness = thickness;

        if (_continuousView)
        {
            OuterBorder.CornerRadius = new CornerRadius(0);
        }
    }

    private void UpdateTextContent()
    {
        TextBlockMain.Blocks.Clear();
        TextBlockMain.TextHighlighters.Clear();
        var c = ThemeHelper.GetConfig(_theme);

        var paragraph = new Paragraph();
        var displayText = string.IsNullOrEmpty(_plainText) ? "(אין טקסט)" : _plainText;

        if (!string.IsNullOrEmpty(_activeDiffWitness))
        {
            var refText = Section.Witnesses.GetValueOrDefault(_activeDiffWitness) ?? "";
            // ── הדגשת מילים מבסיס שאינן בעד הנוסח: TextHighlighter שומר על baseline ──
            var segments = DiffHelper.BuildDiffSegments(_plainText, refText, hideMinor: false);
            var fullText = string.Concat(segments.Select(s => s.Text));
            paragraph.Inlines.Add(new Run
            {
                Text = fullText,
                FontFamily = new FontFamily(_fontFamily),
                FontSize = _fontSize,
                Foreground = c.SectionTextBrush,
            });
            var diffHighlighter = new TextHighlighter
            {
                Background = c.DiffRedBrush,
                Foreground = new SolidColorBrush(Colors.White),
            };
            int pos = 0;
            foreach (var seg in segments)
            {
                if (seg.IsHighlighted && seg.Text.Length > 0)
                {
                    diffHighlighter.Ranges.Add(new TextRange { StartIndex = pos, Length = seg.Text.Length });
                }
                pos += seg.Text.Length;
            }
            if (diffHighlighter.Ranges.Count > 0)
            {
                TextBlockMain.TextHighlighters.Add(diffHighlighter);
            }
        }
        else if (HasSearchMatch && !string.IsNullOrEmpty(_pageSearchTerm))
        {
            // ── הדגשת חיפוש בצהוב דרך TextHighlighter ──
            paragraph.Inlines.Add(new Run
            {
                Text = displayText,
                FontFamily = new FontFamily(_fontFamily),
                FontSize = _fontSize,
                Foreground = c.SectionTextBrush,
            });
            var searchHighlighter = new TextHighlighter
            {
                Background = c.DiffYellowBrush,
                Foreground = new SolidColorBrush(Colors.Black),
            };
            int idx = 0;
            while (idx < displayText.Length)
            {
                int found = displayText.IndexOf(_pageSearchTerm, idx, StringComparison.Ordinal);
                if (found < 0) break;
                searchHighlighter.Ranges.Add(new TextRange { StartIndex = found, Length = _pageSearchTerm.Length });
                idx = found + _pageSearchTerm.Length;
            }
            if (searchHighlighter.Ranges.Count > 0)
            {
                TextBlockMain.TextHighlighters.Add(searchHighlighter);
            }
        }
        else
        {
            paragraph.Inlines.Add(new Run
            {
                Text = displayText,
                FontFamily = new FontFamily(_fontFamily),
                FontSize = _fontSize,
                Foreground = c.SectionTextBrush,
            });
        }

        TextBlockMain.Blocks.Add(paragraph);
    }

    private string _pageSearchTerm = "";

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        ApplyBorderStyle();
    }

    public void ShowWitnessDiff(string witnessName)
    {
        if (_activeDiffWitness == witnessName)
        {
            // toggle off
            _activeDiffWitness = null;
            UpdateTextContent();
            ApplyBorderStyle();
            return;
        }
        _activeDiffWitness = witnessName;
        UpdateTextContent();
        ApplyBorderStyle();
    }

    public void ClearDiff()
    {
        _activeDiffWitness = null;
        UpdateTextContent();
        ApplyBorderStyle();
    }

    public void UpdateFont(string fontFamily, int fontSize, string theme)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;
        UpdateColors();
        UpdateTextContent();
    }

    public bool SearchHighlight(string term)
    {
        _pageSearchTerm = term ?? "";
        if (string.IsNullOrEmpty(_plainText) || string.IsNullOrEmpty(term))
        {
            HasSearchMatch = false;
            UpdateTextContent();
            return false;
        }
        HasSearchMatch = _plainText.Contains(term, StringComparison.Ordinal);
        UpdateTextContent();
        return HasSearchMatch;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        ApplyBorderStyle();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        ApplyBorderStyle();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}
