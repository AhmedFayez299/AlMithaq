using AlMithaq.Core.Models;
using System.Net.Http.Json;
using AlMithaq.Core.Logic;

namespace AlMithaq.Features.Quran.Services;

public sealed class QuranMediaService(HttpClient client)
{
    public static readonly Reciter[] Reciters = new Reciter[]
    {
        new("ar.abdulbasitmurattal", "عبد الباسط عبد الصمد المرتل"),
        new("ar.abdullahbasfar", "عبد الله بصفر"),
        new("ar.abdurrahmaansudais", "عبدالرحمن السديس"),
        new("ar.abdulsamad", "عبدالباسط عبدالصمد"),
        new("ar.shaatree", "أبو بكر الشاطري"),
        new("ar.ahmedajamy", "أحمد بن علي العجمي", "Ahmed_ibn_Ali_al-Ajamy_128kbps_ketaballah.net"),
        new("ar.alafasy", "مشاري العفاسي"),
        new("ar.hanirifai", "هاني الرفاعي"),
        new("ar.husary", "محمود خليل الحصري"),
        new("ar.husarymujawwad", "محمود خليل الحصري (المجود)"),
        new("ar.hudhaify", "علي بن عبدالرحمن الحذيفي"),
        new("ar.mahermuaiqly", "ماهر المعيقلي"),
        new("ar.minshawi", "محمد صديق المنشاوي"),
        new("ar.minshawimujawwad", "محمد صديق المنشاوي (المجود)"),
        new("ar.muhammadayyoub", "محمد أيوب"),
        new("ar.muhammadjibreel", "محمد جبريل"),
        new("ar.saoodshuraym", "سعود الشريم"),
        new("ar.aymanswoaid", "أيمن سويد"),
        new("ea.16", "سعد الغامدي", "Ghamadi_40kbps"),
        new("ea.35", "محمد الطبلاوي", "Mohammad_al_Tablaway_64kbps"),
        new("ea.49", "شهريار برهيزكار", "Parhizgar_48kbps"),
        new("ea.51", "صلاح بوخاطر", "Salaah_AbdulRahman_Bukhatir_128kbps"),
        new("ea.52", "عبد المحسن القاسم", "Muhsin_Al_Qasim_192kbps"),
        new("ea.53", "عبد الله عواد الجهني", "Abdullaah_3awwaad_Al-Juhaynee_128kbps"),
        new("ea.54", "صلاح البدير", "Salah_Al_Budair_128kbps"),
        new("ea.55", "عبد الله المطرود", "Abdullah_Matroud_128kbps"),
        new("ea.56", "أحمد نعينع", "Ahmed_Neana_128kbps"),
        new("ea.57", "محمد عبد الكريم", "Muhammad_AbdulKareem_128kbps"),
        new("ea.58", "خليفة الطنيجي", "khalefa_al_tunaiji_64kbps"),
        new("ea.59", "محمود علي البنا", "mahmoud_ali_al_banna_32kbps"),
        new("ea.64", "كريم منصوري", "Karim_Mansoori_40kbps"),
        new("ea.65", "محمود خليل الحصري (المعلم)", "Husary_Muallim_128kbps"),
        new("ea.66", "خالد القحطاني", "Khaalid_Abdullaah_al-Qahtaanee_192kbps"),
        new("ea.67", "ياسر الدوسري", "Yasser_Ad-Dussary_128kbps"),
        new("ea.68", "ناصر القطامي", "Nasser_Alqatami_128kbps"),
        new("ea.69", "علي حجاج السويسي", "Ali_Hajjaj_AlSuesy_128kbps"),
        new("ea.70", "سهل ياسين", "Sahl_Yassin_128kbps"),
        new("ea.73", "عزيز عليلي", "aziz_alili_128kbps"),
        new("ea.74", "ياسر سلامة", "Yaser_Salamah_128kbps"),
        new("ea.75", "أكرم العلاقمي", "Akram_AlAlaqimy_128kbps"),
        new("ea.76", "علي جابر", "Ali_Jaber_64kbps"),
        new("ea.77", "فارس عباد", "Fares_Abbad_64kbps"),
    }.OrderBy(r => r.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("ar"), false)).ToArray();
    // Available CDN bitrates differ by recording; both streaming and downloads use the same source.
    private static string AudioUrl(string reciter, QuranVerse verse)
    {
        var recording = Reciters.First(r => r.Id == reciter);
        if (recording.EveryAyahFolder is { } folder) return $"https://everyayah.com/data/{folder}/{verse.Surah:000}{verse.Ayah:000}.mp3";
        var bitrate = reciter is "ar.shaatree" or "ar.alafasy" or "ar.husary" or "ar.husarymujawwad" or "ar.hudhaify" or "ar.mahermuaiqly" or "ar.minshawi" or "ar.muhammadayyoub" or "ar.muhammadjibreel" ? 128 : 64;
        return $"https://cdn.islamic.network/quran/audio/{bitrate}/{reciter}/{verse.Number}.mp3";
    }
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private readonly SemaphoreSlim _prefetchLock = new(1, 1);
    private string AudioPath(string reciter, int verse) => Path.Combine(FileSystem.AppDataDirectory, "audio", reciter, $"{verse}.mp3");
    private static string PrefetchPath(string reciter, int verse) => Path.Combine(FileSystem.CacheDirectory, "quran-prefetch", $"{reciter}-{verse}.mp3");
    public string GetAudioSource(string reciter, QuranVerse verse)
    {
        ValidateReciter(reciter);
        var path = AudioPath(reciter, verse.Number);
        if (File.Exists(path)) return path;
        var prefetched = PrefetchPath(reciter, verse.Number);
        return File.Exists(prefetched) ? prefetched : AudioUrl(reciter, verse);
    }
    public async Task PrefetchAsync(string reciter, QuranVerse verse, CancellationToken cancellationToken)
    {
        if (IsDownloaded(reciter, verse.Number) || !await _prefetchLock.WaitAsync(0, cancellationToken)) return;
        var path = PrefetchPath(reciter, verse.Number);
        var partial = path + ".part";
        try
        {
            if (File.Exists(path)) return;
            var folder = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(folder);
            using var response = await client.GetAsync(AudioUrl(reciter, verse), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentType?.MediaType is not ("audio/mpeg" or "audio/mp3" or "application/octet-stream")) return;
            await using (var output = File.Create(partial))
                await response.Content.CopyToAsync(output, cancellationToken);
            if (new FileInfo(partial).Length < 100) return;
            File.Move(partial, path, true);
            foreach (var old in new DirectoryInfo(folder).EnumerateFiles("*.mp3").OrderByDescending(f => f.LastWriteTimeUtc).Skip(24))
                old.Delete();
        }
        catch (OperationCanceledException) { }
        catch (HttpRequestException ex) { System.Diagnostics.Debug.WriteLine(ex); }
        catch (IOException ex) { System.Diagnostics.Debug.WriteLine(ex); }
        finally
        {
            try { File.Delete(partial); }
            catch (IOException) { }
            finally { _prefetchLock.Release(); }
        }
    }
    public bool IsDownloaded(string reciter, int verse) => File.Exists(AudioPath(reciter, verse));
    private static void ValidateReciter(string reciter)
    {
        if (!Reciters.Any(r => r.Id == reciter)) throw new ArgumentOutOfRangeException(nameof(reciter));
    }
    public Task DownloadAsync(string reciter, IReadOnlyList<QuranVerse> verses, IProgress<double> progress, CancellationToken ct) =>
        BackgroundWork.Run(() => DownloadCoreAsync(reciter, verses, progress, ct), ct);
    private async Task DownloadCoreAsync(string reciter, IReadOnlyList<QuranVerse> verses, IProgress<double> progress, CancellationToken ct)
    {
        ValidateReciter(reciter);
        await _cacheLock.WaitAsync(ct);
        try
        {
            for (var i = 0; i < verses.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var path = AudioPath(reciter, verses[i].Number);
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    try
                    {
                        using var response = await client.GetAsync(AudioUrl(reciter, verses[i]), HttpCompletionOption.ResponseHeadersRead, ct);
                        response.EnsureSuccessStatusCode();
                        var mediaType = response.Content.Headers.ContentType?.MediaType;
                        if (mediaType is not ("audio/mpeg" or "audio/mp3" or "application/octet-stream"))
                            throw new InvalidDataException("استجابة التلاوة ليست ملفًا صوتيًا.");
                        await using (var output = File.Create(path + ".part"))
                            await response.Content.CopyToAsync(output, ct);
                        if (new FileInfo(path + ".part").Length < 100) throw new InvalidDataException("ملف صوتي غير مكتمل.");
                        File.Move(path + ".part", path, true);
                    }
                    finally { if (File.Exists(path + ".part")) File.Delete(path + ".part"); }
                }
                progress.Report((i + 1d) / verses.Count);
            }
        }
        finally { _cacheLock.Release(); }
    }
    public Task<string> GetTafsirAsync(QuranVerse verse, CancellationToken ct) => BackgroundWork.Run(() => GetTafsirCoreAsync(verse, ct), ct);
    private async Task<string> GetTafsirCoreAsync(QuranVerse verse, CancellationToken ct)
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "tafsir", $"{verse.Number}.txt");
        if (File.Exists(path)) return await File.ReadAllTextAsync(path, ct);
        using var response = await client.GetAsync($"https://api.alquran.cloud/v1/ayah/{verse.Surah}:{verse.Ayah}/ar.muyassar", ct);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var data = json.RootElement.GetProperty("data");
        if (data.GetProperty("number").GetInt32() != verse.Number) throw new InvalidDataException("مرجع التفسير لا يطابق الآية.");
        var text = data.GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidDataException("التفسير غير متاح.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".part";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, text, ct);
            ct.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, true);
        }
        finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
        return text;
    }
    public long AudioBytes() => Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, "audio"))
        ? Directory.EnumerateFiles(Path.Combine(FileSystem.AppDataDirectory, "audio"), "*.mp3", SearchOption.AllDirectories).Sum(p => new FileInfo(p).Length) : 0;
    public async Task ClearAudioAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "audio");
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        finally { _cacheLock.Release(); }
    }
}
