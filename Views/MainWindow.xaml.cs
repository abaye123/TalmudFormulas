using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;
using TalmudFormulas.Models;
using TalmudFormulas.Services;
using TalmudFormulas.Views.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace TalmudFormulas.Views;

/// <summary>
/// החלון הראשי של האפליקציה.
/// מקביל ל-main_window.py - כולל את כל הלוגיקה: ניווט, חיפוש, מצבי תצוגה, hotkeys.
/// </summary>
public sealed partial class MainWindow : Window
{
    // ── מצב ראשי ──────────────────────────────────
    private List<Masechet> _masechtot = new();
    private List<string> _witnesses = new();
    private List<Models.Page> _pages = new();
    private string _mainWitness = "";
    private string _currentMasechetName = "";
    private int _currentPageIdx = 0;

    // ── מצב תצוגה ──────────────────────────────────
    private DisplayMode _displayMode = DisplayMode.Sections;
    private SectionBlock? _selectedBlock;
    private readonly List<SectionBlock> _sectionBlocks = new();
    private WordsView? _wordsView;
    private List<WordEntry> _currentWordsData = new();
    private int _currentWordIdx = -1;

    // ── חיפוש בדף ──────────────────────────────────
    private string _pageSearchTerm = "";
    private int _pageSearchIdx = -1;

    // ── הגדרות ──────────────────────────────────
    private AppSettings _settings;
    private ThemeConfig _config;

    // ── צבע ה-LEDים בכפתורי ניווט ───────────────────
    private bool _navPanelVisible = true;

    public MainWindow()
    {
        _settings = SettingsManager.Load();
        _config = ThemeHelper.GetConfig(_settings.Theme);

        InitializeComponent();

        // ── סרגל כותרת מותאם: Mica יזרח, לחצני מערכת בצד שמאל (RTL) ──
        WindowHelper.EnableRtlTitleBarLayout(this);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // אייקון
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                WindowHelper.SetIcon(this, iconPath);
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Set icon failed", ex);
        }

        // גודל / הגדלה
        if (_settings.WindowMaximized)
        {
            WindowHelper.SetSize(this, _settings.WindowWidth, _settings.WindowHeight);
            WindowHelper.CenterOnScreen(this);
            WindowHelper.Maximize(this);
        }
        else
        {
            WindowHelper.SetSize(this, _settings.WindowWidth, _settings.WindowHeight);
            WindowHelper.CenterOnScreen(this);
        }

        // Mica
        ThemeHelper.TrySetMica(this);

        // ערכת נושא של החלון (Light/Dark לפי הגדרות)
        ApplyElementTheme();

        Closed += OnWindowClosed;

        // טעינת מסכתות בעת אתחול
        RootGrid.Loaded += OnRootLoaded;

        // הקצאת WitnessPanel
        WitnessPanelView.Theme = _settings.Theme;
        WitnessPanelView.FontFamilyName = _settings.FontFamily;
        WitnessPanelView.FontSizeValue = _settings.FontSize;
        WitnessPanelView.HighlightDiffs = _settings.HighlightDiffs;
        WitnessPanelView.HideEmptyWitnesses = _settings.HideEmptyWitnesses;
        WitnessPanelView.HideMinorDiffs = _settings.HideMinorDiffs;
        WitnessPanelView.WitnessClicked += OnWitnessCardClicked;
        WitnessPanelView.SettingsChanged += OnPanelSettingsChanged;

        // קולט לאירועי מקלדת — לתצוגת מילים
        RootGrid.KeyDown += OnRootKeyDown;
        RootGrid.IsTabStop = true;
    }

    private void ApplyElementTheme()
    {
        var elementTheme = ThemeHelper.GetElementTheme(_settings.Theme);
        if (RootGrid is FrameworkElement fe)
        {
            fe.RequestedTheme = elementTheme;
        }
    }

    private void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        LoadMasechetList();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        // שמור את גודל החלון האחרון
        try
        {
            var appWindow = WindowHelper.GetAppWindow(this);
            _settings.WindowWidth = appWindow.Size.Width;
            _settings.WindowHeight = appWindow.Size.Height;
            _settings.WindowMaximized = (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                && p.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized;
            SettingsManager.Save(_settings);
        }
        catch { /* swallow */ }
    }

    // ── טעינת מסכתות ראשונית ──────────────────────────

    private void LoadMasechetList()
    {
        try
        {
            _masechtot = DatabaseService.LoadMasechetList();

            if (_masechtot.Count == 0)
            {
                _ = ShowDbMissingError();
                return;
            }

            MasechetList.ItemsSource = _masechtot.Select(m => m.Name).ToList();
            if (_masechtot.Count > 0)
            {
                MasechetList.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("LoadMasechetList failed", ex);
            _ = ShowError("שגיאה בטעינת רשימת מסכתות", ex.Message);
        }
    }

    private async Task ShowDbMissingError()
    {
        await ShowError("מסד נתונים חסר",
            "לא נמצא קובץ talmud.db.\n\nאנא ודא שהקובץ קיים בתיקיית Assets ליד התוכנה.");
    }

    private async Task ShowError(string title, string message)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "סגור",
            XamlRoot = RootGrid.XamlRoot,
            FlowDirection = FlowDirection.RightToLeft,
        };
        await dlg.ShowAsync();
    }

    // ── טעינת מסכת ──────────────────────────────────

    private void OnMasechetSelected(object sender, SelectionChangedEventArgs e)
    {
        var idx = MasechetList.SelectedIndex;
        if (idx < 0 || idx >= _masechtot.Count) return;

        try
        {
            var ms = _masechtot[idx];
            (var witnesses, var pages) = DatabaseService.FetchMasechet(ms.Id);
            _witnesses = witnesses;
            _pages = pages;
            _currentMasechetName = ms.Name;
            _mainWitness = witnesses.FirstOrDefault() ?? "";
            _selectedBlock = null;
            _sectionBlocks.Clear();
            _wordsView = null;

            WitnessPanelView.UpdateWitnesses(witnesses);
            WitnessPanelView.Reset();

            PageSub.Text = string.IsNullOrEmpty(_mainWitness) ? "" : $"טקסט: {_mainWitness}";

            // טעינת רשימת דפים
            PageList.SelectionChanged -= OnPageSelected;
            PageList.ItemsSource = pages.Select(p => p.PageLabel).ToList();
            PageList.SelectionChanged += OnPageSelected;

            ClearText();
            PageTitle.Text = _currentMasechetName;

            if (pages.Count > 0)
            {
                PageList.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("OnMasechetSelected failed", ex);
            PageTitle.Text = $"Error: {ex.Message}";
        }
    }

    // ── טעינת דף ──────────────────────────────────

    private void OnPageSelected(object sender, SelectionChangedEventArgs e)
    {
        var idx = PageList.SelectedIndex;
        if (idx < 0 || idx >= _pages.Count) return;
        LoadPage(idx);
    }

    private void LoadPage(int idx)
    {
        if (idx < 0 || idx >= _pages.Count) return;
        try
        {
            _currentPageIdx = idx;
            _selectedBlock = null;
            _sectionBlocks.Clear();
            _wordsView = null;
            _pageSearchTerm = "";
            _pageSearchIdx = -1;
            PageSearchBox.Text = "";

            var page = _pages[idx];
            PageTitle.Text = $"{_currentMasechetName} · דף {page.PageLabel}";
            UpdateNavButtons(idx);
            ClearText();

            var sections = DatabaseService.FetchPage(page.Id);

            if (_displayMode == DisplayMode.Words)
            {
                LoadPageWords(page, sections);
            }
            else
            {
                LoadPageSections(sections, page.PageLabel);
            }

            TextScroll.ChangeView(null, 0, null, true);
            WitnessPanelView.Reset();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("LoadPage failed", ex);
        }
    }

    private void LoadPageSections(List<Section> sections, string pageLabel)
    {
        TextContainer.Spacing = _settings.ContinuousSectionsView ? 0 : 8;

        foreach (var section in sections)
        {
            var block = new SectionBlock(section, _mainWitness,
                _settings.FontFamily, _settings.FontSize,
                _settings.Theme, _settings.ContinuousSectionsView);

            block.Clicked += (s, _) => SelectSection(section, block, pageLabel);
            TextContainer.Children.Add(block);
            _sectionBlocks.Add(block);
        }
    }

    private void LoadPageWords(Models.Page page, List<Section> sections)
    {
        var wordsData = DatabaseService.FetchPageWords(page.Id);
        _currentWordsData = wordsData;
        _currentWordIdx = -1;

        if (wordsData.Count == 0)
        {
            var lbl = new TextBlock
            {
                Text = "אין נתוני מילים לדף זה",
                FontSize = 14,
                Foreground = _config.WordMissingTextBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(40),
            };
            TextContainer.Children.Add(lbl);
            return;
        }

        var wv = new WordsView(wordsData, _mainWitness,
            _settings.FontFamily, _settings.FontSize, _settings.Theme);
        wv.WordClicked += (s, idx) => SelectWord(idx, wordsData, page.PageLabel);
        _wordsView = wv;
        TextContainer.Children.Add(wv);
    }

    private void SelectSection(Section section, SectionBlock block, string page)
    {
        if (_selectedBlock is not null && !ReferenceEquals(_selectedBlock, block))
        {
            _selectedBlock.SetSelected(false);
            _selectedBlock.ClearDiff();
        }
        block.SetSelected(true);
        _selectedBlock = block;

        var baseText = section.Witnesses.GetValueOrDefault(_mainWitness) ?? "";
        WitnessPanelView.ShowSection(section, page, baseText);
    }

    private void SelectWord(int idx, List<WordEntry> wordsData, string page)
    {
        _currentWordIdx = idx;
        _wordsView?.SelectWord(idx);
        WitnessPanelView.ShowWord(wordsData[idx], page, _mainWitness, wordsData, idx);
    }

    private void OnWitnessCardClicked(object? sender, string witnessName)
    {
        _selectedBlock?.ShowWitnessDiff(witnessName);
    }

    private void OnPanelSettingsChanged(object? sender, EventArgs e)
    {
        _settings.HighlightDiffs = WitnessPanelView.HighlightDiffs;
        _settings.HideEmptyWitnesses = WitnessPanelView.HideEmptyWitnesses;
        _settings.HideMinorDiffs = WitnessPanelView.HideMinorDiffs;
        SettingsManager.Save(_settings);
    }

    // ── ניווט בין דפים ──────────────────────────────

    private void OnPrevPage(object sender, RoutedEventArgs e)
    {
        if (PageList.SelectedIndex > 0)
        {
            PageList.SelectedIndex--;
        }
    }

    private void OnNextPage(object sender, RoutedEventArgs e)
    {
        if (PageList.SelectedIndex < PageList.Items.Count - 1)
        {
            PageList.SelectedIndex++;
        }
    }

    private void UpdateNavButtons(int idx)
    {
        PrevBtn.IsEnabled = idx > 0;
        NextBtn.IsEnabled = idx < _pages.Count - 1;
    }

    // ── חיפוש מהיר "מסכת דף" ──────────────────────────

    private void OnQuickNavQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var raw = (args.QueryText ?? sender.Text ?? "").Trim();
        if (string.IsNullOrEmpty(raw)) return;

        // תבנית: <מסכת> [דף] <מספר_או_עברית>
        var match = Regex.Match(raw,
            @"^([א-ת]+(?:\s[א-ת]+)*)" +
            @"(?:\s+דף)?" +
            @"\s+([א-ת""״׳’']+|\d+)$");

        if (!match.Success)
        {
            FlashError(QuickNavBox);
            return;
        }

        var msQuery = match.Groups[1].Value.Trim();
        var pgQuery = match.Groups[2].Value.Trim();

        var msIdx = -1;
        for (int i = 0; i < _masechtot.Count; i++)
        {
            if (DiffHelper.MasechetMatches(_masechtot[i].Name, msQuery))
            {
                msIdx = i;
                break;
            }
        }

        if (msIdx < 0)
        {
            FlashError(QuickNavBox);
            return;
        }

        if (MasechetList.SelectedIndex != msIdx)
        {
            MasechetList.SelectedIndex = msIdx;
        }

        var pgIdx = -1;
        for (int i = 0; i < _pages.Count; i++)
        {
            if (DiffHelper.PageMatches(_pages[i].PageLabel, pgQuery))
            {
                pgIdx = i;
                break;
            }
        }

        if (pgIdx < 0)
        {
            FlashError(QuickNavBox);
            return;
        }

        PageList.SelectedIndex = pgIdx;
        QuickNavBox.Text = "";
    }

    private void FlashError(Control ctrl)
    {
        ctrl.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
        ctrl.BorderThickness = new Thickness(1);
    }

    // ── חיפוש בתוך הדף ──────────────────────────────

    private void OnPageSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput
            && args.Reason != AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
        {
            return;
        }

        _pageSearchTerm = (PageSearchBox.Text ?? "").Trim();
        _pageSearchIdx = -1;

        List<UIElement> matching;
        if (_displayMode == DisplayMode.Words && _wordsView is not null)
        {
            _wordsView.SearchHighlight(_pageSearchTerm);
            matching = _wordsView.GetMatchWidgets().Cast<UIElement>().ToList();
        }
        else
        {
            foreach (var block in _sectionBlocks)
            {
                block.SearchHighlight(_pageSearchTerm);
            }
            matching = _sectionBlocks.Where(b => b.HasSearchMatch).Cast<UIElement>().ToList();
        }

        if (string.IsNullOrEmpty(_pageSearchTerm))
        {
            return;
        }

        if (matching.Count > 0)
        {
            _pageSearchIdx = 0;
            ScrollToSearchResult(0);
        }
    }

    private void OnPageSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (string.IsNullOrEmpty(_pageSearchTerm)) return;

        List<UIElement> matching;
        if (_displayMode == DisplayMode.Words && _wordsView is not null)
        {
            matching = _wordsView.GetMatchWidgets().Cast<UIElement>().ToList();
        }
        else
        {
            matching = _sectionBlocks.Where(b => b.HasSearchMatch).Cast<UIElement>().ToList();
        }

        if (matching.Count == 0) return;
        _pageSearchIdx = (_pageSearchIdx + 1) % matching.Count;
        ScrollToSearchResult(_pageSearchIdx);
    }

    private void ScrollToSearchResult(int idx)
    {
        List<UIElement> matching;
        if (_displayMode == DisplayMode.Words && _wordsView is not null)
        {
            matching = _wordsView.GetMatchWidgets().Cast<UIElement>().ToList();
        }
        else
        {
            matching = _sectionBlocks.Where(b => b.HasSearchMatch).Cast<UIElement>().ToList();
        }

        if (idx < 0 || idx >= matching.Count) return;

        var widget = matching[idx] as FrameworkElement;
        if (widget is null) return;

        // חישוב המיקום של ה-widget בתוך ה-ScrollViewer
        try
        {
            var transform = widget.TransformToVisual(TextContainer);
            var pos = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            TextScroll.ChangeView(null, pos.Y - 30, null, false);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("ScrollToSearchResult failed", ex);
        }
    }

    // ── מצבי תצוגה (sections / words) ────────────────

    private void OnModeToggled(object sender, RoutedEventArgs e)
    {
        var prevMode = _displayMode;
        string? prevSectionLabel = null;

        if (prevMode == DisplayMode.Sections && _selectedBlock is not null)
        {
            prevSectionLabel = _selectedBlock.Section.SectionLabel;
        }
        else if (prevMode == DisplayMode.Words && _currentWordIdx >= 0 &&
                 _currentWordsData.Count > _currentWordIdx)
        {
            prevSectionLabel = _currentWordsData[_currentWordIdx].SectionLabel;
        }

        _displayMode = ModeBtn.IsChecked == true ? DisplayMode.Words : DisplayMode.Sections;
        if (_displayMode == DisplayMode.Words)
        {
            ModeText.Text = "תצוגת קטעים";
            ModeIcon.Glyph = "";
        }
        else
        {
            ModeText.Text = "תצוגת מילים";
            ModeIcon.Glyph = "";
        }

        if (_pages.Count == 0) return;
        LoadPage(_currentPageIdx);

        if (prevSectionLabel is null) return;

        var page = _pages[_currentPageIdx].PageLabel;

        if (_displayMode == DisplayMode.Words && _currentWordsData.Count > 0)
        {
            for (int i = 0; i < _currentWordsData.Count; i++)
            {
                if (_currentWordsData[i].SectionLabel == prevSectionLabel)
                {
                    SelectWord(i, _currentWordsData, page);
                    break;
                }
            }
        }
        else if (_displayMode == DisplayMode.Sections && _sectionBlocks.Count > 0)
        {
            foreach (var block in _sectionBlocks)
            {
                if (block.Section.SectionLabel == prevSectionLabel)
                {
                    SelectSection(block.Section, block, page);
                    break;
                }
            }
        }
    }

    private void ClearText()
    {
        TextContainer.Children.Clear();
    }

    // ── מקלדת בתצוגת מילים ───────────────────────────

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_displayMode != DisplayMode.Words || _currentWordsData.Count == 0) return;

        int newIdx = _currentWordIdx;
        switch (e.Key)
        {
            case VirtualKey.Left:
                newIdx = _currentWordIdx < 0 ? 0 : _currentWordIdx + 1;
                break;
            case VirtualKey.Right:
                newIdx = _currentWordIdx < 0 ? _currentWordsData.Count - 1 : _currentWordIdx - 1;
                break;
            case VirtualKey.Down:
                if (_currentWordIdx < 0 || _wordsView is null) { newIdx = 0; }
                else
                {
                    var adj = _wordsView.GetWordAtAdjacentRow(_currentWordIdx, 1);
                    newIdx = adj >= 0 ? adj : _currentWordIdx;
                }
                break;
            case VirtualKey.Up:
                if (_currentWordIdx < 0 || _wordsView is null) { newIdx = 0; }
                else
                {
                    var adj = _wordsView.GetWordAtAdjacentRow(_currentWordIdx, -1);
                    newIdx = adj >= 0 ? adj : _currentWordIdx;
                }
                break;
            default:
                return;
        }

        newIdx = Math.Max(0, Math.Min(newIdx, _currentWordsData.Count - 1));
        if (newIdx != _currentWordIdx)
        {
            var page = _pages[_currentPageIdx].PageLabel;
            SelectWord(newIdx, _currentWordsData, page);
            e.Handled = true;
        }
    }

    // ── סרגל צד ──────────────────────────────────

    private void OnSidebarToggle(object sender, RoutedEventArgs e)
    {
        _navPanelVisible = !_navPanelVisible;
        if (_navPanelVisible)
        {
            NavColumn.Width = new GridLength(260);
            SidebarToggleIcon.Glyph = "";
        }
        else
        {
            NavColumn.Width = new GridLength(0);
            SidebarToggleIcon.Glyph = "";
        }
    }

    // ── הגדרות ──────────────────────────────────

    private async void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog(_settings)
        {
            XamlRoot = RootGrid.XamlRoot,
        };
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ApplySettings(dlg.Result);
        }
    }

    private void ApplySettings(AppSettings newSettings)
    {
        var themeChanged = _settings.Theme != newSettings.Theme;
        var continuousChanged = _settings.ContinuousSectionsView != newSettings.ContinuousSectionsView;

        _settings = newSettings;
        _config = ThemeHelper.GetConfig(_settings.Theme);
        SettingsManager.Save(_settings);
        ThemeHelper.ApplyTheme(_settings.Theme);

        if (themeChanged) ApplyElementTheme();

        WitnessPanelView.UpdateFont(_settings.FontFamily, _settings.FontSize, _settings.Theme);

        foreach (var block in _sectionBlocks)
        {
            block.UpdateFont(_settings.FontFamily, _settings.FontSize, _settings.Theme);
        }
        _wordsView?.UpdateFont(_settings.FontFamily, _settings.FontSize, _settings.Theme);

        if ((themeChanged || continuousChanged) && _pages.Count > 0)
        {
            LoadPage(_currentPageIdx);
        }
    }

    // ── הערת זכויות ──────────────────────────────

    private void OnShowCopyrightNotice(object sender, RoutedEventArgs e)
    {
        CopyrightOverlay.Visibility = Visibility.Visible;
    }

    private void OnOverlayTapped(object sender, TappedRoutedEventArgs e)
    {
        CopyrightOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnCardTapped(object sender, TappedRoutedEventArgs e)
    {
        // עוצר את ה-bubble לכן לחיצה על הקארד עצמו לא סוגרת
        e.Handled = true;
    }

    private async void OnOpenMail(object sender, RoutedEventArgs e)
    {
        try
        {
            var uri = new Uri("mailto:talmud1239@gmail.com");
            await Launcher.LaunchUriAsync(uri);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("OnOpenMail failed", ex);
        }
    }

    private void OnCopyMail(object sender, RoutedEventArgs e)
    {
        try
        {
            var pkg = new DataPackage();
            pkg.SetText("talmud1239@gmail.com");
            Clipboard.SetContent(pkg);

            // משוב חזותי קצר
            if (sender is Button btn && btn.Content is FontIcon icon)
            {
                var prevGlyph = icon.Glyph;
                icon.Glyph = ""; // checkmark
                _ = Task.Delay(1200).ContinueWith(_ =>
                {
                    DispatcherQueue.TryEnqueue(() => icon.Glyph = prevGlyph);
                });
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("OnCopyMail failed", ex);
        }
    }
}

public enum DisplayMode
{
    Sections,
    Words,
}
