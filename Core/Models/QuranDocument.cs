namespace AlMithaq.Core.Models;

public sealed class QuranDocument
{
    public string Source { get; set; } = "";
    public string Version { get; set; } = "";
    public string CopyrightNotice { get; set; } = "";
    public List<QuranChapter> Chapters { get; set; } = [];
}

public sealed class QuranChapter
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public string EnglishName { get; set; } = "";
    public string Revelation { get; set; } = "";
    public List<QuranVerse> Verses { get; set; } = [];
    public string Display => $"{Number}. {Name} · {Verses.Count} آية";
}

public sealed class QuranVerse
{
    public int Number { get; set; }
    public int Surah { get; set; }
    public int Ayah { get; set; }
    public int Page { get; set; }
    public int Juz { get; set; }
    public string Text { get; set; } = "";
    // Independent, unmodified Tanzil simple-clean text used only for searching.
    public string SearchText { get; set; } = "";
    public string Key => $"{Surah}:{Ayah}";
    public string Reference => $"{Surah}:{Ayah} · صفحة {Page} · جزء {Juz}";
}

public sealed record ReadingPosition(int Surah, int Ayah);
public sealed record VerseBookmark(int Surah, int Ayah, string Note, DateTime Updated);
public sealed record Reciter(string Id, string Name, string? EveryAyahFolder = null)
{
    public override string ToString() => Name;
}

[System.Text.Json.Serialization.JsonSerializable(typeof(QuranDocument))]
internal partial class QuranJsonContext : System.Text.Json.Serialization.JsonSerializerContext { }
