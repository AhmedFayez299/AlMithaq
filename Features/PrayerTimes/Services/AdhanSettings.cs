namespace AlMithaq.Features.PrayerTimes.Services;

public static class AdhanSettings
{
    public static readonly string[] Prayers = ["الفجر", "الظهر", "العصر", "المغرب", "العشاء"];
    public static readonly string[] Voices = ["علي بن أحمد ملا · الحرم المكي", "أحمد الحداد"];
    public static bool Enabled { get => Preferences.Get("adhan.enabled", false); set => Preferences.Set("adhan.enabled", value); }
    public static int Voice { get => Math.Clamp(Preferences.Get("adhan.voice", 0), 0, Voices.Length - 1); set => Preferences.Set("adhan.voice", Math.Clamp(value, 0, Voices.Length - 1)); }
    public static double Volume { get => Math.Clamp(Preferences.Get("adhan.volume", .85), 0, 1); set => Preferences.Set("adhan.volume", Math.Clamp(value, 0, 1)); }
    public static bool ForPrayer(int index) => Preferences.Get($"adhan.prayer.{index}", true);
    public static void SetPrayer(int index, bool enabled) => Preferences.Set($"adhan.prayer.{index}", enabled);
}

public static class AdhanCoordinator
{
    public static void Refresh()
    {
#if ANDROID
        try { Platforms.Android.AdhanScheduler.Refresh(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); Preferences.Set("adhan.error", "تعذر جدولة الأذان؛ راجع الأذونات والموقع."); }
#endif
        AlMithaq.Features.Qiyam.Services.QiyamCoordinator.Refresh();
        AlMithaq.Features.Salawat.Services.SalawatCoordinator.Refresh();
    }
    public static string Status
    {
        get
        {
#if ANDROID
            return Platforms.Android.AdhanScheduler.Status;
#else
            return "الأذان المجدول متاح على أندرويد";
#endif
        }
    }
    public static async Task EnableAsync()
    {
#if ANDROID
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
        {
            var granted = await Permissions.RequestAsync<AdhanNotificationPermission>();
            if (granted != PermissionStatus.Granted) { AdhanSettings.Enabled = false; return; }
        }
        AdhanSettings.Enabled = true;
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && !Platforms.Android.AdhanScheduler.CanSchedule)
        {
            var intent = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionRequestScheduleExactAlarm,
                global::Android.Net.Uri.Parse("package:" + global::Android.App.Application.Context.PackageName));
            Platform.CurrentActivity?.StartActivity(intent);
        }
#else
        await Task.CompletedTask;
#endif
        Refresh();
    }
    public static void Preview()
    {
#if ANDROID
        Platforms.Android.AdhanPlaybackService.PlayPreview();
#endif
    }
    public static void StopPreview()
    {
#if ANDROID
        Platforms.Android.AdhanPlaybackService.StopPreview();
#endif
    }
    public static void Stop()
    {
#if ANDROID
        Platforms.Android.AdhanPlaybackService.Stop();
#endif
    }
    public static void TestAlarm()
    {
#if ANDROID
        Platforms.Android.AdhanScheduler.TestAlarm();
#endif
    }
    public static void UpdateVolume()
    {
#if ANDROID
        Platforms.Android.AdhanPlaybackService.UpdateVolume();
#endif
    }
}
#if ANDROID
public sealed class AdhanNotificationPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions => [("android.permission.POST_NOTIFICATIONS", true)];
}
#endif
