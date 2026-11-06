using AlMithaq.Core.Models;

namespace AlMithaq.Core.Logic;

public static class BookmarkMerge
{
    // Imported notes replace the existing note for the same verse, regardless of timestamp.
    public static IReadOnlyList<VerseBookmark> Merge(IEnumerable<VerseBookmark> existing, IEnumerable<VerseBookmark> incoming) =>
        existing.Concat(incoming).GroupBy(b => (b.Surah, b.Ayah)).Select(group => group.Last())
            .OrderByDescending(b => b.Updated).ToList();
}
