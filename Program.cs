using System;
using System.IO;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace TalmudFormulas;

/// <summary>
/// נקודת הכניסה של האפליקציה.
/// אחראי על אתחול Windows App Runtime (Bootstrap) לפני יצירת Application.
/// </summary>
public static class Program
{
    // גרסת Windows App Runtime הנדרשת — חייבת להתאים לזו שב-csproj
    // 0x00010007 = major 1, minor 7
    private const uint MajorMinorVersion = 0x00010007;

    [STAThread]
    public static int Main(string[] args)
    {
        // לוג שגיאות גלובלי - חיוני לאפליקציית unpackaged
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        bool bootstrapInitialized = false;
        try
        {
            // אתחול Windows App SDK Bootstrap (חובה ל-unpackaged)
            // הסיגנטורה הפשוטה: רק major.minor
            Bootstrap.Initialize(MajorMinorVersion);
            bootstrapInitialized = true;
        }
        catch (Exception ex)
        {
            LogStartupError(ex);
            ShowRuntimeMissingError();
            return -1;
        }

        try
        {
            ComWrappersSupport.InitializeComWrappers();

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });

            return 0;
        }
        catch (Exception ex)
        {
            LogStartupError(ex);
            return -1;
        }
        finally
        {
            if (bootstrapInitialized)
            {
                try { Bootstrap.Shutdown(); } catch { /* swallow */ }
            }
        }
    }

    private static void OnUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogStartupError(ex);
        }
    }

    private static void LogStartupError(Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TalmudFormulas");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "errors.log");
            File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // אם גם הלוג נכשל, אין הרבה מה לעשות
        }
    }

    private static void ShowRuntimeMissingError()
    {
        // הצגת הודעה למשתמש על Windows App Runtime חסר
        try
        {
            const string message =
                "לא ניתן לאתחל את Windows App Runtime 1.7.\n\n" +
                "ייתכן שהרכיב חסר במחשב.\n" +
                "ניתן להוריד אותו מאתר Microsoft:\n" +
                "https://aka.ms/windowsappsdk/1.7/latest/windowsappruntimeinstall-x64.exe";

            // PInvoke ל-MessageBox כדי לא לדרוש את WinAppSDK עצמו
            NativeMessageBox.Show(IntPtr.Zero, message,
                "סינופסיס תלמוד בבלי - שגיאה", 0x10);
        }
        catch
        {
            // last resort
        }
    }
}

internal static class NativeMessageBox
{
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public static int Show(IntPtr hWnd, string text, string caption, uint type)
    {
        return MessageBoxW(hWnd, text, caption, type);
    }
}

/// <summary>
/// תמיכה ב-COM wrappers — חיוני ל-WinUI 3 unpackaged.
/// </summary>
internal static class ComWrappersSupport
{
    public static void InitializeComWrappers()
    {
        // ב-WinUI 3 חדש, הרישום מטופל אוטומטית ע"י WinRT.
        // המתודה הזו קיימת כ-extension point למקרה הצורך.
    }
}
