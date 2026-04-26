using System;
using System.IO;

namespace TalmudFormulas.Services;

/// <summary>
/// לוגר שגיאות פשוט — כותב ל-%APPDATA%\TalmudFormulas\errors.log.
/// </summary>
public static class ErrorLogger
{
    private static readonly object _lock = new();

    public static string LogPath => Path.Combine(
        SettingsManager.SettingsDir, "errors.log");

    public static void LogError(string context, Exception ex)
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(SettingsManager.SettingsDir);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
            catch
            {
                // אם הלוג עצמו נכשל, אין הרבה מה לעשות
            }
        }
    }

    public static void LogInfo(string message)
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(SettingsManager.SettingsDir);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}{Environment.NewLine}";
                File.AppendAllText(LogPath, line);
            }
            catch { }
        }
    }
}
