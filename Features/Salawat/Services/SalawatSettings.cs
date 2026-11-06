using AlMithaq.Core.Logic;
using AlMithaq.Features.Quran.Services;

namespace AlMithaq.Features.Salawat.Services;

public static class SalawatSettings
{
    public static readonly int[] Intervals = [15, 30, 60, 120, 180, 360];
    public static readonly string[] IntervalNames = ["كل ١٥ دقيقة", "كل نصف ساعة", "كل ساعة", "كل ساعتين", "كل ٣ ساعات", "كل ٦ ساعات"];
    public static bool Enabled { get => Preferences.Get("salawat.enabled", false); set => Preferences.Set("salawat.enabled", value); }
    public static double Volume { get => Math.Clamp(Preferences.Get("salawat.volume", .7), 0, 1); set => Preferences.Set("salawat.volume", Math.Clamp(value, 0, 1)); }
    public static bool Sound { get => Preferences.Get("salawat.sound", true); set => Preferences.Set("salawat.sound", value); }
    public static int IntervalIndex { get => Math.Clamp(Preferences.Get("salawat.interval", 2), 0, Intervals.Length - 1); set => Preferences.Set("salawat.interval", Math.Clamp(value, 0, Intervals.Length - 1)); }
    public static int Start { get => Math.Clamp(Preferences.Get("salawat.start", 540), 0, 1439); set => Preferences.Set("salawat.start", Math.Clamp(value, 0, 1439)); }
    public static int End { get => Math.Clamp(Preferences.Get("salawat.end", 1320), 0, 1439); set => Preferences.Set("salawat.end", Math.Clamp(value, 0, 1439)); }
    public static bool Valid => LocalReminderPlan.IsValid(Start, End, Intervals[IntervalIndex]);
}

public static class SalawatCoordinator
{
    public static void Refresh()
    {
#if ANDROID
        try { Platforms.Android.SalawatScheduler.Refresh(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Preferences.Set("salawat.error", "تعذر جدولة التذكير؛ راجع أوقات التذكير والأذونات."); }
#endif
    }
    public static async Task EnableAsync()
    {
        if (!SalawatSettings.Valid || !await WirdReminders.RequestPermissionAsync()) { SalawatSettings.Enabled = false; Refresh(); return; }
        SalawatSettings.Enabled = true;
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && !Platforms.Android.AdhanScheduler.CanSchedule)
        {
            using var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionRequestScheduleExactAlarm,
                global::Android.Net.Uri.Parse("package:" + global::Android.App.Application.Context.PackageName));
            Platform.CurrentActivity?.StartActivity(intent);
        }
#endif
        Refresh();
    }
    public static string Status
    {
        get
        {
            if (!SalawatSettings.Valid) return "اختر وقت بداية ونهاية مختلفين";
            if (!SalawatSettings.Enabled) return "التذكير غير مفعّل";
#if ANDROID
            if (!global::AndroidX.Core.App.NotificationManagerCompat.From(global::Android.App.Application.Context)!.AreNotificationsEnabled()) return "اسمح بإشعارات الميثاق من إعدادات الهاتف";
            if (!Platforms.Android.AdhanScheduler.CanSchedule) return "فعّل إذن المنبّهات والتذكيرات الدقيقة لالتزام الوقت";
#endif
            var error = Preferences.Get("salawat.error", ""); if (error.Length > 0) return error;
            var at = Preferences.Get("salawat.next", 0L);
            return at > 0 ? $"التذكير القادم · {DateTimeOffset.FromUnixTimeMilliseconds(at).ToLocalTime():hh:mm tt}" : "التذكير مفعّل";
        }
    }
    public static void Preview()
    {
#if ANDROID
        Platforms.Android.SalawatScheduler.Preview();
#endif
    }
    public static void StopPreview()
    {
#if ANDROID
        Platforms.Android.SalawatScheduler.StopPreview();
#endif
    }
    public static void UpdateVolume()
    {
#if ANDROID
        Platforms.Android.SalawatPlaybackService.UpdateVolume();
#endif
    }
    public static void Test()
    {
#if ANDROID
        Platforms.Android.SalawatScheduler.Test();
#endif
    }
}
