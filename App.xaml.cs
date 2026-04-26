using System;
using Microsoft.UI.Xaml;
using TalmudFormulas.Helpers;
using TalmudFormulas.Services;
using TalmudFormulas.Views;

namespace TalmudFormulas;

/// <summary>
/// מחלקת ה-Application הראשית.
/// </summary>
public partial class App : Application
{
    public static new App? Current { get; private set; }
    public static MainWindow? MainAppWindow { get; private set; }

    public App()
    {
        Current = this;
        InitializeComponent();

        UnhandledException += OnAppUnhandledException;
    }

    /// <summary>
    /// נקודת הכניסה של ה-Application — נקראת ע"י WinUI אחרי Application.Start.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // טעינת ההגדרות לפני יצירת החלון
            var settings = SettingsManager.Load();

            // החלת ערכת הנושא הגלובלית של המערכת
            ThemeHelper.ApplyTheme(settings.Theme);

            // אתחול שירות ה-DB — מצביע על Assets\talmud.db
            DatabaseService.Initialize();

            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogError("OnLaunched failed", ex);
            throw;
        }
    }

    private void OnAppUnhandledException(object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        ErrorLogger.LogError("UnhandledException", e.Exception);
        e.Handled = true;
    }
}
