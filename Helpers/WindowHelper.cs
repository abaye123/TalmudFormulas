using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using TalmudFormulas.Services;
using Windows.Graphics;
using WinRT.Interop;

namespace TalmudFormulas.Helpers;

/// <summary>
/// כלי עזר לעבודה עם חלון WinUI 3 - גודל, הגדלה, איקון וכו'.
/// </summary>
public static class WindowHelper
{
    public static IntPtr GetHwnd(Window window)
    {
        return WindowNative.GetWindowHandle(window);
    }

    // ── PInvoke לעבודה עם סגנון RTL של החלון ─────────────────
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_LAYOUTRTL = 0x00400000L;
    private const long WS_EX_NOINHERITLAYOUT = 0x00100000L;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>
    /// מפעיל פריסת RTL ברמת ה-Win32 — לחצני המערכת (X / מזער / מקסם)
    /// יעברו לצד שמאל של החלון, מתאים לאפליקציה עברית.
    /// WinUI 3 XAML לא מושפע (ה-FlowDirection במשאבים מטפל בזה).
    /// </summary>
    public static void EnableRtlTitleBarLayout(Window window)
    {
        try
        {
            var hwnd = GetHwnd(window);
            if (Environment.Is64BitProcess)
            {
                var ex = GetWindowLongPtr64(hwnd, GWL_EXSTYLE).ToInt64();
                ex |= WS_EX_LAYOUTRTL | WS_EX_NOINHERITLAYOUT;
                SetWindowLongPtr64(hwnd, GWL_EXSTYLE, new IntPtr(ex));
            }
            else
            {
                var ex = (long)(uint)GetWindowLong32(hwnd, GWL_EXSTYLE);
                ex |= WS_EX_LAYOUTRTL | WS_EX_NOINHERITLAYOUT;
                SetWindowLong32(hwnd, GWL_EXSTYLE, unchecked((int)ex));
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("EnableRtlTitleBarLayout failed", ex);
        }
    }

    public static AppWindow GetAppWindow(Window window)
    {
        var hwnd = GetHwnd(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    /// <summary>
    /// קובע את גודל החלון בפיקסלים.
    /// </summary>
    public static void SetSize(Window window, int width, int height)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            appWindow.Resize(new SizeInt32(width, height));
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("SetSize failed", ex);
        }
    }

    /// <summary>
    /// ממקם את החלון במרכז המסך הראשי.
    /// </summary>
    public static void CenterOnScreen(Window window)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            int x = workArea.X + (workArea.Width - appWindow.Size.Width) / 2;
            int y = workArea.Y + (workArea.Height - appWindow.Size.Height) / 2;
            appWindow.Move(new PointInt32(x, y));
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("CenterOnScreen failed", ex);
        }
    }

    /// <summary>
    /// מגדיל את החלון.
    /// </summary>
    public static void Maximize(Window window)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("Maximize failed", ex);
        }
    }

    /// <summary>
    /// מגדיר אייקון לחלון מתוך .ico.
    /// </summary>
    public static void SetIcon(Window window, string iconPath)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            appWindow.SetIcon(iconPath);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("SetIcon failed", ex);
        }
    }

    /// <summary>
    /// קובע כיוון פריסה ימין-לשמאל ברמת ה-OS — עוטף את ה-style של החלון.
    /// בפועל ב-WinUI הזרימה מטופלת ע"י FlowDirection ב-XAML, אבל כפתורי
    /// המערכת (מזער, מקסם, סגור) מצריכים TitleBar customization.
    /// </summary>
    public static void EnableRtlTitleBar(Window window)
    {
        try
        {
            // נשתמש ב-AppWindow.TitleBar עם ExtendsContentIntoTitleBar=true
            // כדי שנוכל למקם כפתורי חלון בצד שמאל (RTL).
            var appWindow = GetAppWindow(window);
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                // ב-RTL כפתורי שליטה צריכים להיות בצד שמאל; WinUI לא תומך
                // בזה ישירות — אז נשאיר ברירת מחדל (ימין) והכותרת תזרום RTL
                // ע"י FlowDirection של ה-Content.
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("EnableRtlTitleBar failed", ex);
        }
    }
}
