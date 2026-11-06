using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Models;
using System.Globalization;
using System.Text.Json;
using AlMithaq.Core.Logic;

namespace AlMithaq.Features.Quran.Services;

public sealed class QuranRepository : IQuranRepository
{
    private readonly Lazy<Task<QuranDocument>> _document;
    private sealed record SearchEntry(QuranVerse Verse, string Search);
    private readonly Lazy<Task<SearchEntry[]>> _index;

    public QuranRepository()
    {
        _document = new(ReadAsync);
        _index = new(async () =>
        {
            var document = await LoadAsync().ConfigureAwait(false);
            return await BackgroundWork.Run(() => document.Chapters.SelectMany(s => s.Verses)
                .Select(v => new SearchEntry(v, Normalize(string.IsNullOrEmpty(v.SearchText) ? v.Text : v.SearchText))).ToArray()).ConfigureAwait(false);
        });
    }

    private static Task<QuranDocument> ReadAsync() => BackgroundWork.Run(() =>
    {
        using var stream = typeof(QuranRepository).Assembly.GetManifestResourceStream("AlMithaq.Data.quran.json")
            ?? throw new InvalidDataException("ملف القرآن المضمّن غير موجود.");
        var document = JsonSerializer.Deserialize(stream, QuranJsonContext.Default.QuranDocument)
            ?? throw new InvalidDataException("تعذر قراءة بيانات القرآن.");
        if (document.Chapters.Count != 114 || document.Chapters.Sum(s => s.Verses.Count) != 6236)
            throw new InvalidDataException("بيانات القرآن غير مكتملة.");
        return document;
    });

    public Task<QuranDocument> LoadAsync() => _document.Value;

    // Search normalization is applied only to a separate index, never to displayed Quran text.
    public static string Normalize(string text) => AlMithaq.Core.Logic.ArabicSearch.Normalize(text);

    public async Task<IReadOnlyList<QuranVerse>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(query);
        if (normalized.Length < 2) return [];
        var index = await _index.Value;
        return await BackgroundWork.Run(() =>
        {
            var matches = new List<QuranVerse>();
            foreach (var item in index)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (item.Search.Contains(normalized, StringComparison.Ordinal)) matches.Add(item.Verse);
            }
            return (IReadOnlyList<QuranVerse>)matches;
        }, cancellationToken);
    }
}
