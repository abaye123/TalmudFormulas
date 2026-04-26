using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;
using TalmudFormulas.Services;

namespace TalmudFormulas.Views;

/// <summary>
/// דיאלוג הגדרות תצוגה: גופן, גודל, ערכת נושא, תצוגה רציפה.
/// מקביל ל-settings_dialog.py.
/// </summary>
public sealed partial class SettingsDialog : ContentDialog
{
    public AppSettings Result { get; private set; }

    private readonly AppSettings _input;
    private List<string> _allFonts = new();
    private bool _initializing = true;

    public SettingsDialog(AppSettings current)
    {
        _input = current;
        // יוצרים עותק כדי שלא נשנה את המקור עד שהמשתמש ילחץ "שמור"
        Result = new AppSettings
        {
            FontFamily = current.FontFamily,
            FontSize = current.FontSize,
            Theme = current.Theme,
            HighlightDiffs = current.HighlightDiffs,
            HideEmptyWitnesses = current.HideEmptyWitnesses,
            HideMinorDiffs = current.HideMinorDiffs,
            ContinuousSectionsView = current.ContinuousSectionsView,
            WindowWidth = current.WindowWidth,
            WindowHeight = current.WindowHeight,
            WindowMaximized = current.WindowMaximized,
        };

        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _allFonts = FontsHelper.GetHebrewFonts();
        FillFontCombo(_allFonts);

        var idx = _allFonts.IndexOf(_input.FontFamily);
        if (idx < 0) idx = _allFonts.IndexOf("David");
        if (idx < 0 && _allFonts.Count > 0) idx = 0;
        if (idx >= 0) FontCombo.SelectedIndex = idx;

        SizeBox.Value = _input.FontSize;
        SizeSlider.Value = _input.FontSize;

        switch (_input.Theme)
        {
            case ThemeHelper.ThemeColorful:
                ColorfulRadio.IsChecked = true;
                break;
            case ThemeHelper.ThemeClassic:
                ClassicRadio.IsChecked = true;
                break;
            default:
                SystemRadio.IsChecked = true;
                break;
        }

        ContinuousToggle.IsOn = _input.ContinuousSectionsView;

        _initializing = false;
        UpdatePreview();
    }

    private void FillFontCombo(List<string> fonts)
    {
        FontCombo.ItemsSource = fonts;
    }

    private void OnFontSearchChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        var query = sender.Text?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            FillFontCombo(_allFonts);
        }
        else
        {
            var filtered = _allFonts.Where(f => f.ToLowerInvariant().Contains(query)).ToList();
            FillFontCombo(filtered);
            sender.ItemsSource = filtered.Take(8).ToList();
        }
    }

    private void OnFontSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string name)
        {
            var idx = _allFonts.IndexOf(name);
            if (idx >= 0)
            {
                FillFontCombo(_allFonts);
                FontCombo.SelectedIndex = idx;
            }
        }
    }

    private void OnFontSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontCombo.SelectedItem is string name)
        {
            Result.FontFamily = name;
            UpdatePreview();
        }
    }

    private void OnSizeChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (double.IsNaN(args.NewValue)) return;
        var size = Math.Max(8, Math.Min(36, (int)args.NewValue));
        if (Result.FontSize == size) return;
        Result.FontSize = size;
        if ((int)SizeSlider.Value != size) SizeSlider.Value = size;
        UpdatePreview();
    }

    private void OnSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_initializing) return;
        var size = (int)e.NewValue;
        if (Result.FontSize == size) return;
        Result.FontSize = size;
        if ((int)SizeBox.Value != size) SizeBox.Value = size;
        UpdatePreview();
    }

    private void OnThemeChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_initializing) return;
        if (ColorfulRadio.IsChecked == true) Result.Theme = ThemeHelper.ThemeColorful;
        else if (ClassicRadio.IsChecked == true) Result.Theme = ThemeHelper.ThemeClassic;
        else Result.Theme = ThemeHelper.ThemeSystem;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewText is null) return;
        try
        {
            PreviewText.FontFamily = new FontFamily(Result.FontFamily);
            PreviewText.FontSize = Result.FontSize;
        }
        catch
        {
            PreviewText.FontFamily = new FontFamily("David");
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Result.ContinuousSectionsView = ContinuousToggle.IsOn;
        // הערכים האחרים כבר התעדכנו בעת שינוי ה-controls
    }
}
