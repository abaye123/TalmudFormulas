using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TalmudFormulas.Helpers;

/// <summary>
/// כלי השוואה בין נוסחאות — מקביל ל-utils.py בפרויקט המקורי.
/// כולל טוקניזציה, נורמליזציה, השוואת מילים, ויצירת תוצאות diff.
/// </summary>
public static class DiffHelper
{
    private static readonly Regex SplitWhitespace = new(@"(\s+)", RegexOptions.Compiled);
    private static readonly Regex NiqqudRegex = new(@"[\u05B0-\u05C7]", RegexOptions.Compiled);
    private static readonly Regex PunctuationRegex = new(
        "[\u05f3\u05f4\",.\\-:;!?()\\[\\]]", RegexOptions.Compiled);
    private static readonly Regex GershayimRegex = new(
        "[\"\u05f4\u05f3\u2019\u2018'\u05f3]", RegexOptions.Compiled);

    /// <summary>
    /// פיצול לטוקנים תוך שמירת המפרידים (רווחים).
    /// </summary>
    public static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();
        // אנחנו רוצים לשמור גם את הרווחים — נשתמש ב-Split עם Capturing group
        var parts = Regex.Split(text, @"(\s+)");
        return parts.Where(p => p != null).ToList();
    }

    /// <summary>
    /// נורמליזציה של מילה — הסרת ניקוד ופיסוק.
    /// </summary>
    public static string NormalizeWord(string? word)
    {
        if (string.IsNullOrEmpty(word)) return "";
        var w = NiqqudRegex.Replace(word, "");
        w = PunctuationRegex.Replace(w, "");
        return w.Trim();
    }

    /// <summary>
    /// בודק האם ההבדל הוא "שינוי קל" (גרשיים, חסר י' וכו').
    /// </summary>
    public static bool IsMinorDiff(string sourceWord, string refWord)
    {
        var s = NormalizeWord(sourceWord);
        var r = NormalizeWord(refWord);
        if (s.Length == 0 || r.Length == 0) return false;
        if (s == r) return true;

        // הסרת גרשיים
        var sNoQuotes = s.Replace("'", "").Replace("\"", "");
        var rNoQuotes = r.Replace("'", "").Replace("\"", "");

        // כלל 2: חסר רק י' (אפילו בלי גרש)
        if (sNoQuotes.Replace("י", "") == rNoQuotes.Replace("י", ""))
        {
            return true;
        }

        // כלל 1: חסר אותיות ויש גרש במקור
        if (sourceWord.Contains('\'') || sourceWord.Contains('"'))
        {
            if (sNoQuotes.Length > 0 && rNoQuotes.Length > 0 &&
                (rNoQuotes.Contains(sNoQuotes) || sNoQuotes.Contains(rNoQuotes)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// מקטעי "matching" של שתי רשימות — מקביל ל-difflib.SequenceMatcher.get_matching_blocks.
    /// משתמש באלגוריתם LCS.
    /// </summary>
    private static List<(int A, int B, int Size)> GetMatchingBlocks(
        List<string> a, List<string> b)
    {
        // dp[i,j] = LCS length של a[..i] ו-b[..j]
        int n = a.Count, m = b.Count;
        if (n == 0 || m == 0) return new List<(int, int, int)>();

        var dp = new int[n + 1, m + 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                if (a[i] == b[j])
                {
                    dp[i + 1, j + 1] = dp[i, j] + 1;
                }
                else
                {
                    dp[i + 1, j + 1] = Math.Max(dp[i + 1, j], dp[i, j + 1]);
                }
            }
        }

        // שחזור ה-matching blocks
        var blocks = new List<(int A, int B, int Size)>();
        int x = n, y = m;
        var matched = new List<(int, int)>();
        while (x > 0 && y > 0)
        {
            if (a[x - 1] == b[y - 1])
            {
                matched.Add((x - 1, y - 1));
                x--; y--;
            }
            else if (dp[x - 1, y] >= dp[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }
        matched.Reverse();

        // מיזוג רצפים סמוכים
        if (matched.Count == 0) return blocks;
        int startA = matched[0].Item1, startB = matched[0].Item2, size = 1;
        for (int i = 1; i < matched.Count; i++)
        {
            var (ai, bi) = matched[i];
            if (ai == startA + size && bi == startB + size)
            {
                size++;
            }
            else
            {
                blocks.Add((startA, startB, size));
                startA = ai; startB = bi; size = 1;
            }
        }
        blocks.Add((startA, startB, size));
        return blocks;
    }

    /// <summary>
    /// מחולל מערך של בוליאנים — אילו מילים בטקסט המקור נמצאו בייחוס.
    /// אם hideMinor=true, גם שינויים קלים נחשבים כתואמים.
    /// </summary>
    private static bool[] ComputeMatchedFlags(
        List<string> sourceTokens, List<string> refTokens,
        out List<string> sourceWordsOnly, out List<string> refWordsOnly,
        bool hideMinor)
    {
        sourceWordsOnly = sourceTokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeWord).ToList();
        refWordsOnly = refTokens
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(NormalizeWord).ToList();

        var matched = new bool[sourceWordsOnly.Count];
        var blocks = GetMatchingBlocks(sourceWordsOnly, refWordsOnly);
        foreach (var (a, _, size) in blocks)
        {
            for (int i = 0; i < size; i++) matched[a + i] = true;
        }

        if (hideMinor)
        {
            // עוברים על המילים הלא-מותאמות ובודקים אם הן שינוי קל
            // נמצא לכל מילה לא-מותאמת את המקבילה הקרובה ב-ref ע"י alignment גס
            // לפשטות — נעבור ברצף ונבדוק את המילה באותו אינדקס יחסי
            int j = 0;
            for (int i = 0; i < sourceWordsOnly.Count; i++)
            {
                if (matched[i])
                {
                    j++;
                    continue;
                }
                if (j < refWordsOnly.Count &&
                    IsMinorDiff(sourceWordsOnly[i], refWordsOnly[j]))
                {
                    matched[i] = true;
                }
            }
        }

        return matched;
    }

    /// <summary>
    /// מייצר רשימה של "DiffSegment" — קטעי טקסט עם דגל אם הם מודגשים או לא.
    /// זה מה שנשתמש בו ב-Inlines של RichTextBlock.
    /// </summary>
    public static List<DiffSegment> BuildDiffSegments(
        string sourceText, string referenceText, bool hideMinor = false)
    {
        var sourceTokens = Tokenize(sourceText);
        var refTokens = Tokenize(referenceText);

        var matched = ComputeMatchedFlags(
            sourceTokens, refTokens,
            out var sourceWords, out _, hideMinor);

        var segments = new List<DiffSegment>();
        int wordIdx = 0;
        foreach (var token in sourceTokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                // רווח / שורה — לא מודגש
                if (segments.Count > 0 && !segments[^1].IsHighlighted)
                {
                    segments[^1].Text += token;
                }
                else
                {
                    segments.Add(new DiffSegment { Text = token, IsHighlighted = false });
                }
            }
            else
            {
                bool isHighlighted = wordIdx < matched.Length && !matched[wordIdx];
                if (segments.Count > 0 && segments[^1].IsHighlighted == isHighlighted)
                {
                    segments[^1].Text += token;
                }
                else
                {
                    segments.Add(new DiffSegment { Text = token, IsHighlighted = isHighlighted });
                }
                wordIdx++;
            }
        }

        return segments;
    }

    // ── גימטריה והתאמת דפים ────────────────────────────────────

    private static readonly Dictionary<char, int> HebrewValues = new()
    {
        ['א'] = 1,  ['ב'] = 2,  ['ג'] = 3,  ['ד'] = 4,  ['ה'] = 5,
        ['ו'] = 6,  ['ז'] = 7,  ['ח'] = 8,  ['ט'] = 9,  ['י'] = 10,
        ['כ'] = 20, ['ך'] = 20, ['ל'] = 30, ['מ'] = 40, ['ם'] = 40,
        ['נ'] = 50, ['ן'] = 50, ['ס'] = 60, ['ע'] = 70, ['פ'] = 80,
        ['ף'] = 80, ['צ'] = 90, ['ץ'] = 90, ['ק'] = 100,['ר'] = 200,
        ['ש'] = 300,['ת'] = 400,
    };

    public static int HebToInt(string s)
    {
        s = GershayimRegex.Replace(s, "").Trim();
        if (string.IsNullOrEmpty(s)) return 0;
        int total = 0;
        foreach (var ch in s)
        {
            if (!HebrewValues.TryGetValue(ch, out var v) || v == 0) return 0;
            total += v;
        }
        return total;
    }

    private static string NormalizePage(string raw)
    {
        raw = (raw ?? "").Trim();
        // הסר "דף" בהתחלה
        if (raw.StartsWith("דף"))
        {
            raw = raw[2..].Trim();
        }
        raw = GershayimRegex.Replace(raw, "").Trim();
        return raw;
    }

    public static bool PageMatches(string pageStr, string queryPage)
    {
        var normData = NormalizePage(pageStr);
        var normQuery = NormalizePage(queryPage);
        if (normData == normQuery) return true;

        var valData = HebToInt(normData);
        var valQuery = HebToInt(normQuery);
        if (valData != 0 && valQuery != 0 && valData == valQuery) return true;

        if (int.TryParse(normQuery, out var asInt) && valData == asInt) return true;
        return false;
    }

    public static bool MasechetMatches(string msName, string queryName)
    {
        var nameClean = msName.StartsWith("מסכת") ? msName[4..].Trim() : msName.Trim();
        var q = (queryName ?? "").Trim();
        return nameClean == q || nameClean.StartsWith(q) || q.StartsWith(nameClean);
    }
}

/// <summary>
/// מקטע diff בתוצאת ההשוואה.
/// </summary>
public class DiffSegment
{
    public string Text { get; set; } = "";
    public bool IsHighlighted { get; set; }
}
