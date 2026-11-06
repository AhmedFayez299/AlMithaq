using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using global::AndroidX.Core.App;
using AlMithaq.Core.Logic;
using AlMithaq.Features.PrayerTimes.Services;

namespace AlMithaq.Platforms.Android;

public static class AdhanScheduler
{
    private static Context AppContext => global::Android.App.Application.Context;
    private static AlarmManager Manager => (AlarmManager)AppContext.GetSystemService(Context.AlarmService)!;
    public static bool CanSchedule => !OperatingSystem.IsAndroidVersionAtLeast(31) || Manager.CanScheduleExactAlarms();
    public const string PermissionChanged = "android.app.action.SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED";
    public static string Status
    {
        get
        {
            if (!AdhanSettings.Enabled) return "الأذان غير مفعّل";
            if (!NotificationManagerCompat.From(AppContext)!.AreNotificationsEnabled()) return "اسمح بإشعارات التطبيق لتفعيل تنبيه الأذان";
            if (!CanSchedule) return "يلزم السماح بالمنبهات والتذكيرات الدقيقة";
            if (new LocationService().Saved() is null) return "حدد موقعك أولًا من مواقيت الصلاة";
            if (!Enumerable.Range(0, 5).Any(AdhanSettings.ForPrayer)) return "اختر صلاة واحدة على الأقل";
            var error = Preferences.Get("adhan.error", "");
            return error.Length > 0 ? error : "مفعّل · يعمل دون إنترنت حتى عند إغلاق الشاشة";
        }
    }
    internal static PendingIntentFlags PendingFlags => PendingIntentFlags.UpdateCurrent | (OperatingSystem.IsAndroidVersionAtLeast(23) ? PendingIntentFlags.Immutable : 0);
    private static PendingIntent Pending(int slot, Intent intent) => PendingIntent.GetBroadcast(AppContext, 8100 + slot, intent, PendingFlags)!;
    private static void Schedule(long at, PendingIntent pending)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(23)) Manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
        else Manager.SetExact(AlarmType.RtcWakeup, at, pending);
    }
    public static void Refresh()
    {
        // Stable request IDs let settings changes cancel every previously scheduled event.
        for (var i = 0; i < 36; i++)
        {
            using var old = Pending(i, new Intent(AppContext, typeof(AdhanAlarmReceiver)));
            Manager.Cancel(old);
        }
        var generation = Guid.NewGuid().ToString("N"); Preferences.Set("adhan.generation", generation);
        Preferences.Set("adhan.error", "");
        if (!AdhanSettings.Enabled || !CanSchedule || !NotificationManagerCompat.From(AppContext)!.AreNotificationsEnabled()) return;
        var location = new LocationService().Saved(); if (location is null) return;
        var selected = Enumerable.Range(0, 5).Where(AdhanSettings.ForPrayer).Select(i => AdhanSettings.Prayers[i]).ToHashSet();
        var service = new PrayerService();
        var alarms = AdhanPlan.Build(DateTime.Now, TimeZoneInfo.Local, d => service.GetDay(location.Location, d), selected);
        foreach (var alarm in alarms)
        {
            using var intent = new Intent(AppContext, typeof(AdhanAlarmReceiver));
            intent.PutExtra("generation", generation); intent.PutExtra("name", alarm.Name); intent.PutExtra("at", alarm.At);
            using var pending = Pending(alarm.Slot, intent);
            Schedule(alarm.At, pending);
        }
        global::Android.Util.Log.Info("AlMithaq.Adhan", $"Scheduled {alarms.Count} alarms; voice={AdhanSettings.Voice}");
    }
    public static void TestAlarm()
    {
        if (!AdhanSettings.Enabled || !CanSchedule || !NotificationManagerCompat.From(AppContext)!.AreNotificationsEnabled()) throw new InvalidOperationException("فعّل الأذان واسمح بالتنبيهات الدقيقة أولًا");
        var at = DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds();
        using var intent = new Intent(AppContext, typeof(AdhanAlarmReceiver)); intent.PutExtra("name", "اختبار الأذان"); intent.PutExtra("at", at); intent.PutExtra("generation", Preferences.Get("adhan.generation", ""));
        using var pending = Pending(35, intent); Schedule(at, pending);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class AdhanAlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null || !AdhanSettings.Enabled) return;
        var generation = intent.GetStringExtra("generation");
        if (generation != Preferences.Get("adhan.generation", "") || !AdhanPlan.CanPlay(intent.GetLongExtra("at", 0), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())) return;
        AdhanCoordinator.Refresh();
        try
        {
            using var playback = new Intent(context, typeof(AdhanPlaybackService));
            playback.PutExtra("name", intent.GetStringExtra("name") ?? "الصلاة");
            global::AndroidX.Core.Content.ContextCompat.StartForegroundService(context, playback);
        }
        catch (Exception ex) { Preferences.Set("adhan.error", "تعذر تشغيل الأذان؛ افتح إعداداته للتحقق."); global::Android.Util.Log.Error("AlMithaq.Adhan", ex.ToString()); }
    }
}

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionTimeChanged, Intent.ActionTimezoneChanged, Intent.ActionMyPackageReplaced, AdhanScheduler.PermissionChanged])]
public sealed class AdhanRestoreReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action is not (Intent.ActionBootCompleted or Intent.ActionTimeChanged or Intent.ActionTimezoneChanged or Intent.ActionMyPackageReplaced or AdhanScheduler.PermissionChanged)) return;
        TimeZoneInfo.ClearCachedData(); AdhanCoordinator.Refresh(); AlMithaq.Features.Quran.Services.WirdReminders.Refresh();
    }
}
