namespace AlMithaq.Features.Quran.Services;

public static class WirdReminders
{
    public static bool Enabled { get => Preferences.Get("wird.reminders", true); set { Preferences.Set("wird.reminders", value); Refresh(); } }
    public static void Refresh()
    {
#if ANDROID
        try { Platforms.Android.WirdReminderScheduler.Refresh(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
#elif IOS
        _ = Platforms.iOS.LocalReminders.RefreshAsync();
#endif
    }
    public static async Task<bool> RequestPermissionAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return await Permissions.RequestAsync<PrayerTimes.Services.AdhanNotificationPermission>() == PermissionStatus.Granted;
#elif IOS
        return await Platforms.iOS.LocalReminders.RequestPermissionAsync();
#endif
#if !IOS
        await Task.CompletedTask; return true;
#endif
    }
    public static string Status
    {
        get
        {
#if ANDROID
            if (!global::AndroidX.Core.App.NotificationManagerCompat.From(global::Android.App.Application.Context)!.AreNotificationsEnabled()) return "اسمح بإشعارات التطبيق لتلقي تذكير الورد";
#endif
            return Enabled ? "تذكير من ٧ إلى ١١ مساءً كل ساعة حتى إتمام الورد" : "تذكير الورد غير مفعّل";
        }
    }
}
