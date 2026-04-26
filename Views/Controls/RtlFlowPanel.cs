using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace TalmudFormulas.Views.Controls;

/// <summary>
/// Custom Panel המסדר children בזרימת RTL — שורה משמאל לימין (כלומר התחלה בימין),
/// עם justified text (פיזור הרווחים) פרט לשורה האחרונה.
/// מקביל ל-_FlowWidget._do_layout ב-words_view.py.
/// </summary>
public class RtlFlowPanel : Panel
{
    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double),
            typeof(RtlFlowPanel), new PropertyMetadata(2.0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(nameof(VerticalSpacing), typeof(double),
            typeof(RtlFlowPanel), new PropertyMetadata(6.0, OnLayoutPropertyChanged));

    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RtlFlowPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0 || double.IsInfinity(availableSize.Width))
        {
            return new Size(0, 0);
        }

        double usable = availableSize.Width;
        var infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);

        double rowWidth = 0;
        double rowHeight = 0;
        double totalHeight = 0;
        bool firstInRow = true;

        foreach (var child in Children)
        {
            child.Measure(infinite);
            var sz = child.DesiredSize;
            double needed = firstInRow ? sz.Width : sz.Width + HorizontalSpacing;

            if (!firstInRow && rowWidth + needed > usable)
            {
                totalHeight += rowHeight + VerticalSpacing;
                rowWidth = sz.Width;
                rowHeight = sz.Height;
                firstInRow = false;
            }
            else
            {
                rowWidth += needed;
                if (sz.Height > rowHeight) rowHeight = sz.Height;
                firstInRow = false;
            }
        }
        totalHeight += rowHeight;

        return new Size(usable, totalHeight + 4);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
        {
            return finalSize;
        }

        double usable = finalSize.Width;

        // ── שלב 1: לאסוף children לשורות ──
        var rows = new System.Collections.Generic.List<System.Collections.Generic.List<UIElement>>();
        var current = new System.Collections.Generic.List<UIElement>();
        double rowWidth = 0;

        foreach (var child in Children)
        {
            var sz = child.DesiredSize;
            double needed = current.Count == 0 ? sz.Width : sz.Width + HorizontalSpacing;

            if (current.Count > 0 && rowWidth + needed > usable)
            {
                rows.Add(current);
                current = new System.Collections.Generic.List<UIElement> { child };
                rowWidth = sz.Width;
            }
            else
            {
                current.Add(child);
                rowWidth += needed;
            }
        }
        if (current.Count > 0) rows.Add(current);

        // ── שלב 2: סידור הפריטים ב-RTL עם justify ──
        double y = 0;
        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            var row = rows[rowIdx];
            double rowHeight = 0;
            double totalWordsWidth = 0;
            foreach (var lbl in row)
            {
                if (lbl.DesiredSize.Height > rowHeight) rowHeight = lbl.DesiredSize.Height;
                totalWordsWidth += lbl.DesiredSize.Width;
            }

            bool isLastRow = rowIdx == rows.Count - 1;
            double gap;
            if (row.Count > 1 && !isLastRow)
            {
                gap = (usable - totalWordsWidth) / (row.Count - 1);
                if (gap < HorizontalSpacing) gap = HorizontalSpacing;
            }
            else
            {
                gap = HorizontalSpacing;
            }

            // RTL: התחלה מימין
            double x = usable;
            foreach (var lbl in row)
            {
                var w = lbl.DesiredSize.Width;
                var h = lbl.DesiredSize.Height;
                lbl.Arrange(new Rect(x - w, y, w, h));
                x -= w + gap;
            }

            y += rowHeight + VerticalSpacing;
        }

        return finalSize;
    }
}
