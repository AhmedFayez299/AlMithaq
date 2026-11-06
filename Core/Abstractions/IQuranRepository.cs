using AlMithaq.Core.Models;

namespace AlMithaq.Core.Abstractions;

public interface IQuranRepository
{
    Task<QuranDocument> LoadAsync();
    Task<IReadOnlyList<QuranVerse>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

public interface IReadingStore
{
    ReadingPosition? GetPosition();
    void SavePosition(QuranVerse verse);
    IReadOnlyList<VerseBookmark> GetBookmarks();
    void SaveBookmark(QuranVerse verse, string note);
    void MergeBookmarks(IReadOnlyList<VerseBookmark> bookmarks);
    void RemoveBookmark(QuranVerse verse);
}
