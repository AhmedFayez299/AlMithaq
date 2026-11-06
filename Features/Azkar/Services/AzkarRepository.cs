using System.Text.Json.Serialization;

namespace AlMithaq.Features.Azkar.Services;

public sealed class Dhikr
{
    [JsonPropertyName("chapter")] public int Chapter { get; set; }
    [JsonPropertyName("chapterTitle")] public string ChapterTitle { get; set; } = "";
    [JsonPropertyName("order")] public int Id { get; set; }
    [JsonPropertyName("content")] public string Text { get; set; } = "";
    [JsonPropertyName("count")] public int Repeat { get; set; }
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("fadl")] public string Benefit { get; set; } = "";
    [JsonPropertyName("type")] public int Type { get; set; }
}

public sealed class AzkarRepository
{
    private readonly Lazy<Task<List<Dhikr>>> _data = new(LoadCoreAsync);
    public Task<List<Dhikr>> LoadAsync() => _data.Value;
    private static Task<List<Dhikr>> LoadCoreAsync() => AlMithaq.Core.Logic.BackgroundWork.Run(() =>
    {
        using var stream = typeof(AzkarRepository).Assembly.GetManifestResourceStream("AlMithaq.Data.azkar.json")
            ?? throw new InvalidDataException("ملف الأذكار المضمّن غير موجود");
        var data = JsonSerializer.Deserialize<List<Dhikr>>(stream) ?? throw new InvalidDataException("تعذر قراءة الأذكار");
        data.AddRange([
            new() { Id = 101, Type = 3, Repeat = 1, Text = "بِاسْمِكَ اللَّهُمَّ أَمُوتُ وَأَحْيَا", Source = "صحيح البخاري، 6324 — ذكر النوم" },
            new() { Id = 102, Type = 3, Repeat = 33, Text = "سُبْحَانَ اللَّهِ", Source = "صحيح البخاري، 6318؛ صحيح مسلم، 2727 — تسبيح النوم: 33، والتحميد: 33، والتكبير: 34" },
            new() { Id = 103, Type = 3, Repeat = 33, Text = "الْحَمْدُ لِلَّهِ", Source = "صحيح البخاري، 6318؛ صحيح مسلم، 2727" },
            new() { Id = 104, Type = 3, Repeat = 34, Text = "اللَّهُ أَكْبَرُ", Source = "صحيح البخاري، 6318؛ صحيح مسلم، 2727" },
            new() { Id = 201, Type = 4, Repeat = 3, Text = "أَسْتَغْفِرُ اللَّهَ", Source = "صحيح مسلم، 591 — بعد الصلاة" },
            new() { Id = 202, Type = 4, Repeat = 1, Text = "اللَّهُمَّ أَنْتَ السَّلَامُ، وَمِنْكَ السَّلَامُ، تَبَارَكْتَ ذَا الْجَلَالِ وَالْإِكْرَامِ", Source = "صحيح مسلم، 591" },
            new() { Id = 203, Type = 4, Repeat = 33, Text = "سُبْحَانَ اللَّهِ", Source = "صحيح مسلم، 597 — التسبيح والتحميد والتكبير 33، ثم التهليل تمام المائة" },
            new() { Id = 204, Type = 4, Repeat = 33, Text = "الْحَمْدُ لِلَّهِ", Source = "صحيح مسلم، 597" },
            new() { Id = 205, Type = 4, Repeat = 33, Text = "اللَّهُ أَكْبَرُ", Source = "صحيح مسلم، 597" },
            new() { Id = 206, Type = 4, Repeat = 1, Text = "لَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ، وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ", Source = "صحيح مسلم، 597" }
        ]);
        using var quranStream = typeof(AzkarRepository).Assembly.GetManifestResourceStream("AlMithaq.Data.quran.json")!;
        var quran = JsonSerializer.Deserialize(quranStream, AlMithaq.Core.Models.QuranJsonContext.Default.QuranDocument)!;
        (int Surah, int Ayah)[] references = [(2, 201), (2, 286), (3, 8), (3, 16), (3, 147), (7, 23), (14, 40), (14, 41), (17, 24), (20, 114), (23, 118), (25, 74), (59, 10)];
        for (var i = 0; i < references.Length; i++)
        {
            var (surah, ayah) = references[i]; var chapter = quran.Chapters[surah - 1];
            data.Add(new() { Id = 1001 + i, Type = 5, Repeat = 1, Text = chapter.Verses.First(v => v.Ayah == ayah).Text, Source = $"{chapter.Name} · الآية {ayah} — دعاء من القرآن الكريم" });
        }
        data.AddRange([
            new() { Id = 2001, Type = 6, Repeat = 1, Text = "اللَّهُمَّ إِنِّي أَسْأَلُكَ الْهُدَى وَالتُّقَى وَالْعَفَافَ وَالْغِنَى", Source = "صحيح مسلم، 2721 — دعاء نبوي" },
            new() { Id = 2002, Type = 6, Repeat = 1, Text = "اللَّهُمَّ اغْفِرْ لِي وَارْحَمْنِي وَاهْدِنِي وَارْزُقْنِي", Source = "صحيح مسلم، 2697 — دعاء نبوي" },
            new() { Id = 2003, Type = 6, Repeat = 1, Text = "اللَّهُمَّ اهْدِنِي وَسَدِّدْنِي", Source = "صحيح مسلم، 2725 — دعاء نبوي" }
        ]);
        using var hisn = typeof(AzkarRepository).Assembly.GetManifestResourceStream("AlMithaq.Data.hisn.json") ?? throw new InvalidDataException("مكتبة حصن المسلم غير موجودة");
        data.AddRange(JsonSerializer.Deserialize<List<Dhikr>>(hisn) ?? throw new InvalidDataException("تعذر قراءة حصن المسلم"));
        return data;
    });
}
