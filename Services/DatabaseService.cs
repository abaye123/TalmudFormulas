using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using TalmudFormulas.Models;

namespace TalmudFormulas.Services;

/// <summary>
/// שירות גישה ל-talmud.db (SQLite).
/// מקביל ל-db.py בפרויקט המקורי.
/// </summary>
public static class DatabaseService
{
    private static string _dbPath = "";

    /// <summary>
    /// מאתר את talmud.db. סדר חיפוש:
    /// 1. ליד ה-EXE (Assets\talmud.db) — מצב מותקן
    /// 2. בתיקיית App של המשתמש
    /// 3. בתיקיית הפרויקט (לפיתוח)
    /// </summary>
    public static void Initialize()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Assets", "talmud.db"),
            Path.Combine(baseDir, "talmud.db"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TalmudFormulas", "talmud.db"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                _dbPath = candidate;
                return;
            }
        }

        // לא נמצא — נשאיר ריק; LoadMasechetList יחזיר רשימה ריקה
        _dbPath = "";
    }

    /// <summary>
    /// קובע ידנית את נתיב ה-DB (למקרה שהמשתמש בחר תיקייה אחרת).
    /// </summary>
    public static void SetDbPath(string path)
    {
        if (File.Exists(path))
        {
            _dbPath = path;
        }
        else if (Directory.Exists(path))
        {
            var combined = Path.Combine(path, "talmud.db");
            if (File.Exists(combined)) _dbPath = combined;
        }
    }

    public static string GetDbPath() => _dbPath;
    public static bool IsAvailable => !string.IsNullOrEmpty(_dbPath) && File.Exists(_dbPath);

    private static SqliteConnection OpenConnection()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("talmud.db לא נמצא");
        }
        var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();
        return conn;
    }

    /// <summary>
    /// טוען את כל המסכתות ממוינות לפי num.
    /// </summary>
    public static List<Masechet> LoadMasechetList()
    {
        var result = new List<Masechet>();
        if (!IsAvailable) return result;

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, num, name FROM masechtot ORDER BY num";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Masechet
            {
                Id = reader.GetInt32(0),
                Num = reader.GetInt32(1),
                Name = reader.GetString(2),
            });
        }
        return result;
    }

    /// <summary>
    /// טוען את עדי הנוסח והדפים של מסכת.
    /// </summary>
    public static (List<string> Witnesses, List<Page> Pages) FetchMasechet(int masechetId)
    {
        var witnesses = new List<string>();
        var pages = new List<Page>();
        if (!IsAvailable) return (witnesses, pages);

        using var conn = OpenConnection();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM witnesses WHERE masechet_id=$id ORDER BY position";
            cmd.Parameters.AddWithValue("$id", masechetId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                witnesses.Add(reader.GetString(0));
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, page_label FROM pages WHERE masechet_id=$id ORDER BY id";
            cmd.Parameters.AddWithValue("$id", masechetId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                pages.Add(new Page
                {
                    Id = reader.GetInt32(0),
                    PageLabel = reader.GetString(1),
                });
            }
        }

        return (witnesses, pages);
    }

    /// <summary>
    /// טוען את כל הקטעים של דף, עם המיפוי שלהם לעדי נוסח.
    /// </summary>
    public static List<Section> FetchPage(int pageId)
    {
        var sections = new List<Section>();
        if (!IsAvailable) return sections;

        using var conn = OpenConnection();

        var sectionRows = new List<(int Id, string Label)>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, section_label FROM sections WHERE page_id=$id ORDER BY id";
            cmd.Parameters.AddWithValue("$id", pageId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sectionRows.Add((reader.GetInt32(0), reader.GetString(1)));
            }
        }

        foreach (var (sectionId, sectionLabel) in sectionRows)
        {
            var section = new Section { SectionLabel = sectionLabel };
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT w.name, t.content
                FROM texts t
                JOIN witnesses w ON w.id = t.witness_id
                WHERE t.section_id=$sid";
            cmd.Parameters.AddWithValue("$sid", sectionId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(0);
                var content = reader.IsDBNull(1) ? null : reader.GetString(1);
                section.Witnesses[name] = content;
            }
            sections.Add(section);
        }

        return sections;
    }

    /// <summary>
    /// טוען את כל המילים של דף — לתצוגת המילים.
    /// תומך גם בפורמט הישן (sections_words_texts.content) וגם בפורמט החדש (words.word).
    /// </summary>
    public static List<WordEntry> FetchPageWords(int pageId)
    {
        var result = new List<WordEntry>();
        if (!IsAvailable) return result;

        using var conn = OpenConnection();

        // האם קיים פורמט חדש (טבלת words)?
        bool hasWordsTable = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT name FROM sqlite_master WHERE type='table' AND name='words'";
            using var reader = cmd.ExecuteReader();
            hasWordsTable = reader.Read();
        }

        // האם קיים sections_words?
        bool hasSwTable = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText =
                "SELECT name FROM sqlite_master WHERE type='table' AND name='sections_words'";
            using var reader = cmd.ExecuteReader();
            hasSwTable = reader.Read();
        }

        if (!hasSwTable) return result;

        // נבנה את השאילתה המתאימה
        string sql;
        if (hasWordsTable)
        {
            // פורמט חדש — words.word
            sql = @"
                SELECT sw.id, s.section_label, w.name, wd.word
                FROM sections_words sw
                JOIN sections s ON s.id = sw.section_id
                JOIN sections_words_texts swt ON swt.sections_word_id = sw.id
                JOIN witnesses w ON w.id = swt.witness_id
                JOIN words wd ON wd.id = swt.word_id
                WHERE sw.page_id = $pid
                ORDER BY sw.id, w.position";
        }
        else
        {
            // פורמט ישן — sections_words_texts.content
            sql = @"
                SELECT sw.id, sw.section_label, w.name, swt.content
                FROM sections_words sw
                JOIN sections_words_texts swt ON swt.sections_word_id = sw.id
                JOIN witnesses w ON w.id = swt.witness_id
                WHERE sw.page_id = $pid
                ORDER BY sw.id, w.position";
        }

        var ordered = new List<int>();
        var map = new Dictionary<int, WordEntry>();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("$pid", pageId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int swId = reader.GetInt32(0);
                string secLabel = reader.GetString(1);
                string witnessName = reader.GetString(2);
                string? content = reader.IsDBNull(3) ? null : reader.GetString(3);

                if (!map.TryGetValue(swId, out var entry))
                {
                    entry = new WordEntry { SectionLabel = secLabel };
                    map[swId] = entry;
                    ordered.Add(swId);
                }
                entry.Witnesses[witnessName] = content;
            }
        }

        foreach (var id in ordered)
        {
            result.Add(map[id]);
        }
        return result;
    }
}
