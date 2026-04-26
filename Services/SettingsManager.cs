using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TalmudFormulas.Services;

/// <summary>
/// מודל ההגדרות הנשמרות בין הפעלות.
/// תואם ל-settings.json של הפרויקט המקורי + שדות חדשים.
/// </summary>
public class AppSettings
{
    [JsonPropertyName("font_family")]
    public string FontFamily { get; set; } = "David";

    [JsonPropertyName("font_size")]
    public int FontSize { get; set; } = 16;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "system"; // system | classic | colorful

    [JsonPropertyName("highlight_diffs")]
    public bool HighlightDiffs { get; set; } = false;

    [JsonPropertyName("hide_empty_witnesses")]
    public bool HideEmptyWitnesses { get; set; } = true;

    [JsonPropertyName("hide_minor_diffs")]
    public bool HideMinorDiffs { get; set; } = false;

    [JsonPropertyName("continuous_sections_view")]
    public bool ContinuousSectionsView { get; set; } = false;

    [JsonPropertyName("window_width")]
    public int WindowWidth { get; set; } = 1300;

    [JsonPropertyName("window_height")]
    public int WindowHeight { get; set; } = 820;

    [JsonPropertyName("window_maximized")]
    public bool WindowMaximized { get; set; } = true;
}

/// <summary>
/// מנהל הגדרות — קריאה/כתיבה ל-%APPDATA%\TalmudFormulas\settings.json.
/// מקביל ל-settings_manager.py.
/// </summary>
public static class SettingsManager
{
    private static AppSettings? _cache;
    private static readonly object _lock = new();

    public static string SettingsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TalmudFormulas");

    public static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        lock (_lock)
        {
            if (_cache is not null) return _cache;

            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    _cache = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _cache = new AppSettings();
                }
            }
            catch
            {
                _cache = new AppSettings();
            }
            return _cache;
        }
    }

    public static void Save(AppSettings settings)
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsPath, json);
                _cache = settings;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Settings save failed", ex);
            }
        }
    }

    /// <summary>
    /// עדכון חלקי — מעדכן רק שדות נתונים ושומר.
    /// </summary>
    public static void Update(Action<AppSettings> mutate)
    {
        var settings = Load();
        mutate(settings);
        Save(settings);
    }
}
