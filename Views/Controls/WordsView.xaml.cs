using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;
using TalmudFormulas.Models;

namespace TalmudFormulas.Views.Controls;

/// <summary>
/// תצוגת מילים - מציגה את כל המילים של הדף ברצף.
/// לחיצה על מילה מציגה את עדי הנוסח עבורה בפאנל הצדדי.
/// מקביל ל-WordsView ב-words_view.py.
/// </summary>
public sealed partial class WordsView : UserControl
{
    private readonly List<WordEntry> _wordsData;
    private readonly string _mainWitness;
    private string _fontFamily;
    private int _fontSize;
    private string _theme;
    private int _selectedIdx = -1;

    public List<ClickableWord> WordLabels { get; } = new();

    public event EventHandler<int>? WordClicked;

    public WordsView(List<WordEntry> wordsData, string mainWitness,
        string fontFamily, int fontSize, string theme)
    {
        _wordsData = wordsData;
        _mainWitness = mainWitness;
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;

        InitializeComponent();

        BuildWords();
        UpdateBackground();
    }

    private void BuildWords()
    {
        WordLabels.Clear();
        WordsPanel.Children.Clear();
        for (int i = 0; i < _wordsData.Count; i++)
        {
            var wd = _wordsData[i];
            var text = wd.Witnesses.GetValueOrDefault(_mainWitness);
            bool isPresent = !string.IsNullOrEmpty(text) && text != "None";
            var display = isPresent ? text! : "—";

            var lbl = new ClickableWord(display, i, isPresent, _fontFamily, _fontSize, _theme);
            lbl.Clicked += OnWordClicked;
            WordLabels.Add(lbl);
            WordsPanel.Children.Add(lbl);
        }
    }

    private void UpdateBackground()
    {
        var c = ThemeHelper.GetConfig(_theme);
        Background = c.MainBgBrush;
    }

    private void OnWordClicked(object? sender, int idx)
    {
        SelectWord(idx);
        WordClicked?.Invoke(this, idx);
    }

    public void SelectWord(int idx)
    {
        if (_selectedIdx >= 0 && _selectedIdx < WordLabels.Count)
        {
            WordLabels[_selectedIdx].IsSelected = false;
        }
        _selectedIdx = idx;
        if (idx >= 0 && idx < WordLabels.Count)
        {
            WordLabels[idx].IsSelected = true;
        }
    }

    public void ClearSelection() => SelectWord(-1);

    public void UpdateFont(string fontFamily, int fontSize, string theme)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;
        UpdateBackground();
        foreach (var lbl in WordLabels)
        {
            lbl.UpdateFont(fontFamily, fontSize, theme);
        }
    }

    public bool SearchHighlight(string term)
    {
        if (string.IsNullOrEmpty(term))
        {
            foreach (var lbl in WordLabels) lbl.IsSearchMatch = false;
            return false;
        }
        bool found = false;
        foreach (var lbl in WordLabels)
        {
            bool match = lbl.DisplayText.Contains(term, StringComparison.OrdinalIgnoreCase);
            lbl.IsSearchMatch = match;
            if (match) found = true;
        }
        return found;
    }

    public List<ClickableWord> GetMatchWidgets()
    {
        return WordLabels.Where(l => l.IsSearchMatch).ToList();
    }

    /// <summary>
    /// מחזיר את האינדקס של המילה בשורה הסמוכה (-1 שורה מעל, +1 מתחת)
    /// הקרובה ביותר אופקית למילה הנוכחית.
    /// </summary>
    public int GetWordAtAdjacentRow(int currentIdx, int direction)
    {
        if (currentIdx < 0 || currentIdx >= WordLabels.Count) return -1;
        var current = WordLabels[currentIdx];

        try
        {
            var currentTransform = current.TransformToVisual(this);
            var currentPos = currentTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
            var currentCenterX = currentPos.X + current.ActualWidth / 2;
            var currentY = currentPos.Y;
            var rowThreshold = Math.Max(current.ActualHeight * 0.6, 5);

            // חיפוש המילה בשורה הסמוכה הקרובה ביותר אופקית
            ClickableWord? best = null;
            double bestXDiff = double.MaxValue;

            foreach (var lbl in WordLabels)
            {
                if (ReferenceEquals(lbl, current)) continue;
                var t = lbl.TransformToVisual(this);
                var pos = t.TransformPoint(new Windows.Foundation.Point(0, 0));

                double yDiff = pos.Y - currentY;
                // מחפשים שורה אחת מעל / מתחת בלבד
                if (direction > 0 && yDiff < rowThreshold) continue;
                if (direction < 0 && yDiff > -rowThreshold) continue;

                // האם השורה הסמוכה? (לא ריחוק של 2 שורות+)
                double approxRow = Math.Abs(yDiff) / Math.Max(current.ActualHeight + 5, 10);
                if (approxRow > 1.6) continue;

                var lblCenterX = pos.X + lbl.ActualWidth / 2;
                var xDiff = Math.Abs(lblCenterX - currentCenterX);
                if (xDiff < bestXDiff)
                {
                    bestXDiff = xDiff;
                    best = lbl;
                }
            }

            return best?.Index ?? -1;
        }
        catch
        {
            return -1;
        }
    }
}
