using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TalmudFormulas.Helpers;

namespace TalmudFormulas.Views.Controls;

/// <summary>
/// מילה לחיצה בודדת בתצוגת המילים. מקביל ל-_ClickableWord ב-words_view.py.
/// </summary>
public class ClickableWord : UserControl
{
    public int Index { get; }
    public bool IsPresent { get; }
    private bool _isSelected;
    private bool _isSearchMatch;
    private bool _isHover;
    private string _theme;
    private string _fontFamily;
    private int _fontSize;
    private readonly TextBlock _label;
    private readonly Border _border;

    public string DisplayText => _label.Text;

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; ApplyStyle(); }
    }

    public bool IsSearchMatch
    {
        get => _isSearchMatch;
        set { _isSearchMatch = value; ApplyStyle(); }
    }

    public event EventHandler<int>? Clicked;

    public ClickableWord(string text, int idx, bool isPresent,
        string fontFamily, int fontSize, string theme)
    {
        Index = idx;
        IsPresent = isPresent;
        _theme = theme;
        _fontFamily = fontFamily;
        _fontSize = fontSize;

        _label = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily(fontFamily),
            FontSize = fontSize,
        };

        _border = new Border
        {
            Padding = new Thickness(2, 1, 2, 1),
            CornerRadius = new CornerRadius(3),
            Child = _label,
        };
        Content = _border;

        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
            Microsoft.UI.Input.InputSystemCursorShape.Hand);

        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        Tapped += OnTapped;

        ApplyStyle();
    }

    private void ApplyStyle()
    {
        var c = ThemeHelper.GetConfig(_theme);
        _label.FontFamily = new FontFamily(_fontFamily);
        _label.FontSize = _fontSize;

        if (_isSelected)
        {
            _border.Background = c.WordSelectedBgBrush;
            _label.Foreground = c.WordSelectedTextBrush;
            _label.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
            _label.FontStyle = Windows.UI.Text.FontStyle.Normal;
        }
        else if (_isSearchMatch)
        {
            _border.Background = new SolidColorBrush(ThemeHelper.ColorFromHex("#FFD700"));
            _label.Foreground = new SolidColorBrush(ThemeHelper.ColorFromHex("#1A202C"));
            _label.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            _label.FontStyle = Windows.UI.Text.FontStyle.Normal;
        }
        else if (_isHover)
        {
            _border.Background = c.WordHoverBgBrush;
            _label.Foreground = c.WordHoverTextBrush;
            _label.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            _label.FontStyle = Windows.UI.Text.FontStyle.Normal;
        }
        else if (!IsPresent)
        {
            _border.Background = new SolidColorBrush(Colors.Transparent);
            _label.Foreground = c.WordMissingTextBrush;
            _label.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            _label.FontStyle = Windows.UI.Text.FontStyle.Italic;
        }
        else
        {
            _border.Background = new SolidColorBrush(Colors.Transparent);
            _label.Foreground = c.WordNormalTextBrush;
            _label.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
            _label.FontStyle = Windows.UI.Text.FontStyle.Normal;
        }
    }

    public void UpdateFont(string fontFamily, int fontSize, string theme)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _theme = theme;
        ApplyStyle();
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHover = true;
        ApplyStyle();
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHover = false;
        ApplyStyle();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        Clicked?.Invoke(this, Index);
    }
}
