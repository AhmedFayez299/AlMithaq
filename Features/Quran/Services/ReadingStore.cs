using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Models;

namespace AlMithaq.Features.Quran.Services;

public sealed class ReadingStore : IReadingStore
{
    private const string PositionKey = "quran.position.v2";
    private const string BookmarksKey = "quran.bookmarks.v2";
    private static T? Read<T>(string key)
    {
        try { return JsonSerializer.Deserialize<T>(Preferences.Default.Get(key, "null")); }
        catch (JsonException) { return default; }
    }

    public ReadingPosition? GetPosition()
    {
        var position = Read<ReadingPosition>(PositionKey);
        return position is { Surah: >= 1 and <= 114, Ayah: >= 1 and <= 286 } ? position : null;
    }
    public void SavePosition(QuranVerse verse) => Preferences.Default.Set(PositionKey,
        JsonSerializer.Serialize(new ReadingPosition(verse.Surah, verse.Ayah)));
    public IReadOnlyList<VerseBookmark> GetBookmarks() => (Read<List<VerseBookmark>>(BookmarksKey) ?? [])
        .Where(b => b is { Surah: >= 1 and <= 114, Ayah: >= 1 and <= 286, Note: not null }).ToList();
    public void SaveBookmark(QuranVerse verse, string note)
    {
        var list = GetBookmarks().Where(b => b.Surah != verse.Surah || b.Ayah != verse.Ayah).ToList();
        list.Insert(0, new(verse.Surah, verse.Ayah, note, DateTime.UtcNow));
        Preferences.Default.Set(BookmarksKey, JsonSerializer.Serialize(list));
    }
    public void MergeBookmarks(IReadOnlyList<VerseBookmark> bookmarks) => Preferences.Default.Set(BookmarksKey,
        JsonSerializer.Serialize(AlMithaq.Core.Logic.BookmarkMerge.Merge(GetBookmarks(), bookmarks)));
    public void RemoveBookmark(QuranVerse verse) => Preferences.Default.Set(BookmarksKey,
        JsonSerializer.Serialize(GetBookmarks().Where(b => b.Surah != verse.Surah || b.Ayah != verse.Ayah)));
}
