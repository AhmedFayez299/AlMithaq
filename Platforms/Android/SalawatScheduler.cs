using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using global::AndroidX.Core.App;
using AlMithaq.Core.Logic;
using AlMithaq.Features.Salawat.Services;

namespace AlMithaq.Platforms.Android;

public static class SalawatScheduler
{
    private static Context Context => global::Android.App.Application.Context;
    private static AlarmManager Manager => (AlarmManager)Context.GetSystemService(Context.AlarmService)!;
    private static PendingIntent Pending(int slot, Intent intent) => PendingIntent.GetBroadcast(Context, 9500 + slot, intent, AdhanScheduler.PendingFlags)!;
    private static void Schedule(long at, PendingIntent pending)
    {
        if (AdhanScheduler.CanSchedule) Manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
        else Manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
    }
    public static void Refresh()
    {
        for (var i = 0; i < 2; i++) { using var old = Pending(i, new Intent(Context, typeof(SalawatReceiver))); Manager.Cancel(old); }
        var generation = Guid.NewGuid().ToString("N"); Preferences.Set("salawat.generation", generation); Preferences.Set("salawat.error", ""); Preferences.Set("salawat.next", 0L);
        if (!SalawatSettings.Enabled) { SalawatPlaybackService.Stop(); NotificationManagerCompat.From(Context)!.Cancel(9500); return; }
        if (!SalawatSettings.Valid || !NotificationManagerCompat.From(Context)!.AreNotificationsEnabled()) return;
        var next = LocalReminderPlan.Next(DateTimeOffset.Now, TimeZoneInfo.Local, SalawatSettings.Start, SalawatSettings.End, SalawatSettings.Intervals[SalawatSettings.IntervalIndex]);
        using var intent = new Intent(Context, typeof(SalawatReceiver)); intent.PutExtra("at", next.ToUnixTimeMilliseconds()); intent.PutExtra("generation", generation);
        using var pending = Pending(0, intent); Schedule(next.ToUnixTimeMilliseconds(), pending); Preferences.Set("salawat.next", next.ToUnixTimeMilliseconds());
    }
    public static void Test()
    {
        if (!SalawatSettings.Enabled || !NotificationManagerCompat.From(Context)!.AreNotificationsEnabled()) throw new InvalidOperationException("فعّل التذكير واسمح بإشعارات التطبيق أولًا");
        using var intent = new Intent(Context, typeof(SalawatReceiver)); intent.PutExtra("at", DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds()); intent.PutExtra("test", true); intent.PutExtra("generation", Preferences.Get("salawat.generation", ""));
        using var pending = Pending(1, intent); Schedule(intent.GetLongExtra("at", 0), pending);
    }
    public static void Preview() => SalawatPlaybackService.Preview();
    public static void StopPreview() => SalawatPlaybackService.StopPreview();
    internal static void Notify(Context context, bool test)
    {
        using var channel = new NotificationChannel("salawat_text", "الصلاة على النبي", NotificationImportance.Default); channel.SetSound(null, null);
        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!; manager.CreateNotificationChannel(channel);
        using var open = new Intent(context, typeof(MainActivity)); open.PutExtra("open_salawat", true); open.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        using var pending = PendingIntent.GetActivity(context, 9500, open, AdhanScheduler.PendingFlags)!;
        using var builder = new NotificationCompat.Builder(context, "salawat_text"); builder.SetSmallIcon(Resource.Drawable.notification_wird); builder.SetContentTitle(test ? "اختبار تذكير الصلاة على النبي ﷺ" : "الصلاة على النبي ﷺ"); builder.SetContentText("اللهم صل وسلم على نبينا محمد"); builder.SetContentIntent(pending); builder.SetAutoCancel(true);
        manager.Notify(9500, builder.Build());
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class SalawatReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null || !SalawatSettings.Enabled || intent.GetStringExtra("generation") != Preferences.Get("salawat.generation", "")) return;
        var at = intent.GetLongExtra("at", 0); var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); var test = intent.GetBooleanExtra("test", false);
        var allowed = SalawatSettings.Valid && at > 0 && now >= at && now - at < 15 * 60_000 && (test || LocalReminderPlan.IsWithinWindow(DateTime.Now, SalawatSettings.Start, SalawatSettings.End));
        if (allowed && NotificationManagerCompat.From(context)!.AreNotificationsEnabled())
        {
            SalawatScheduler.Notify(context, test);
            if (SalawatSettings.Sound && SalawatSettings.Volume > 0 && !AdhanPlaybackService.IsPlaying)
            {
                try { using var service = new Intent(context, typeof(SalawatPlaybackService)); global::AndroidX.Core.Content.ContextCompat.StartForegroundService(context, service); }
                catch (Exception ex) { Preferences.Set("salawat.error", "اسمح بالتذكيرات الدقيقة لتشغيل الصوت في الخلفية"); global::Android.Util.Log.Error("AlMithaq.Salawat", ex.ToString()); }
            }
        }
        // One future alarm, never a queue of catch-up reminders.
        if (!test) SalawatCoordinator.Refresh();
    }
}
