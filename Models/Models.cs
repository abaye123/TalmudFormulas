using System.Collections.Generic;

namespace TalmudFormulas.Models;

/// <summary>
/// מסכת בודדת.
/// </summary>
public class Masechet
{
    public int Id { get; set; }
    public int Num { get; set; }
    public string Name { get; set; } = "";

    public override string ToString() => Name;
}

/// <summary>
/// דף בודד במסכת (כולל id ולייבל - "ב.", "ב:", "ג." וכו').
/// </summary>
public class Page
{
    public int Id { get; set; }
    public string PageLabel { get; set; } = "";

    public override string ToString() => PageLabel;
}

/// <summary>
/// קטע בדף — כולל לייבל הקטע ומיפוי שם עד נוסח → טקסט.
/// </summary>
public class Section
{
    public string SectionLabel { get; set; } = "";
    public Dictionary<string, string?> Witnesses { get; set; } = new();
}

/// <summary>
/// יחידת מילה אחת בתצוגת המילים — לייבל הקטע + מיפוי עדים → מילה.
/// </summary>
public class WordEntry
{
    public string SectionLabel { get; set; } = "";
    public Dictionary<string, string?> Witnesses { get; set; } = new();
}
