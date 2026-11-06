using AlMithaq.Core.Logic;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Features.Quran.Services;
namespace AlMithaq.Features.Qiyam.Services;

public sealed record QiyamLog(int Rakaat = 0, bool Witr = false, bool Completed = false, int Goal = 2);
public static class QiyamSettings
{
    public static bool Enabled { get => Preferences.Get("qiyam.reminder", false); set => Preferences.Set("qiyam.reminder", value); }
    public static int LeadIndex { get => Math.Clamp(Preferences.Get("qiyam.lead", 0), 0, 3); set => Preferences.Set("qiyam.lead", Math.Clamp(value, 0, 3)); }
    public static int LeadMinutes => new[] { 0, 15, 30, 60 }[LeadIndex];
    public static int Goal { get => Math.Clamp(Preferences.Get("qiyam.goal", 2), 2, 100); set => Preferences.Set("qiyam.goal", value); }
    private static string Key(DateTime date) => $"qiyam.log.{date:yyyyMMdd}";
    public static QiyamLog Load(DateTime date)
    {
        try { var log = JsonSerializer.Deserialize<QiyamLog>(Preferences.Get(Key(date), "null")); return log is { Rakaat: >= 0 and <= 1000, Goal: >= 2 and <= 100 } && log.Goal % 2 == 0 ? log : new(Goal: Goal); }
        catch (JsonException) { return new(Goal: Goal); }
    }
    public static void Save(DateTime date, QiyamLog log) { Preferences.Set(Key(date), JsonSerializer.Serialize(log)); QiyamCoordinator.Refresh(); }
}
public static class QiyamService
{
    public static QiyamWindow Calculate(Location location, DateTime evening, IPrayerService? service = null)
    {
        service ??= new PrayerService(); evening = evening.Date;
        var day = service.GetDay(location, evening); var tomorrow = service.GetDay(location, evening.AddDays(1));
        var window = new QiyamWindow(QiyamWindow.Instant(day.First(p => p.Name == "المغرب").Time, TimeZoneInfo.Local),
            QiyamWindow.Instant(day.First(p => p.Name == "العشاء").Time, TimeZoneInfo.Local),
            QiyamWindow.Instant(tomorrow.First(p => p.Name == "الفجر").Time, TimeZoneInfo.Local));
        if (!window.IsValid) throw new InvalidOperationException("تعذر حساب وقت الليل لهذا الموقع. راجع إعدادات المواقيت.");
        return window;
    }
    public static DateTime CurrentEvening(Location? location)
    {
        var date = DateTime.Today;
        if (location is null) return date;
        try { return DateTimeOffset.Now < Calculate(location, date.AddDays(-1)).Fajr ? date.AddDays(-1) : date; }
        catch { return date; }
    }
}
public static class QiyamCoordinator
{
    public static void Refresh()
    {
#if ANDROID
        try { Platforms.Android.QiyamScheduler.Refresh(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
#endif
    }
    public static async Task<bool> EnableAsync()
    {
        if (!await WirdReminders.RequestPermissionAsync()) return false;
        QiyamSettings.Enabled = true; Refresh(); return true;
    }
    public static void Test()
    {
#if ANDROID
        Platforms.Android.QiyamScheduler.Test();
#endif
    }
}
