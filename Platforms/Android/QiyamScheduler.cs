using global::Android.App;
using global::Android.Content;
using global::AndroidX.Core.App;
using AlMithaq.Features.Qiyam.Services;
using AlMithaq.Features.PrayerTimes.Services;
namespace AlMithaq.Platforms.Android;

public static class QiyamScheduler
{
    private static Context Context => global::Android.App.Application.Context;
    private static AlarmManager Manager => (AlarmManager)Context.GetSystemService(Context.AlarmService)!;
    private static PendingIntent Pending(int slot, Intent intent) => PendingIntent.GetBroadcast(Context, 9400 + slot, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    private static void Schedule(long at, PendingIntent pending)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31) || Manager.CanScheduleExactAlarms()) Manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
        else Manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
    }
    public static void Refresh()
    {
        for (var i = 0; i < 9; i++) { using var old = Pending(i, new Intent(Context, typeof(QiyamReceiver))); Manager.Cancel(old); }
        var generation = Guid.NewGuid().ToString("N"); Preferences.Set("qiyam.generation", generation);
        if (!QiyamSettings.Enabled) { NotificationManagerCompat.From(Context)!.Cancel(9400); return; }
        if (new LocationService().Saved() is not { } location || !NotificationManagerCompat.From(Context)!.AreNotificationsEnabled()) return;
        if (QiyamSettings.Load(QiyamService.CurrentEvening(location.Location)).Completed) NotificationManagerCompat.From(Context)!.Cancel(9400);
        for (var i = 0; i < 8; i++)
        {
            var evening = DateTime.Today.AddDays(i - 1);
            if (QiyamSettings.Load(evening).Completed) continue;
            try
            {
                var window = QiyamService.Calculate(location.Location, evening);
                var at = window.ReminderAt(QiyamSettings.LeadMinutes).ToUnixTimeMilliseconds(); if (at <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) continue;
                using var intent = new Intent(Context, typeof(QiyamReceiver)); intent.PutExtra("at", at); intent.PutExtra("night", evening.ToString("yyyy-MM-dd")); intent.PutExtra("generation", generation);
                using var pending = Pending(i, intent); Schedule(at, pending);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }
    public static void Test()
    {
        if (!QiyamSettings.Enabled) throw new InvalidOperationException("فعّل تذكير قيام الليل أولًا");
        using var intent = new Intent(Context, typeof(QiyamReceiver)); intent.PutExtra("at", DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds()); intent.PutExtra("test", true); intent.PutExtra("generation", Preferences.Get("qiyam.generation", ""));
        using var pending = Pending(8, intent); Schedule(intent.GetLongExtra("at", 0), pending);
    }
}
[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class QiyamReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null || !QiyamSettings.Enabled || intent.GetStringExtra("generation") != Preferences.Get("qiyam.generation", "")) return;
        var at = intent.GetLongExtra("at", 0); var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now < at || now - at > 30 * 60_000) return;
        var test = intent.GetBooleanExtra("test", false);
        if (!test)
        {
            if (!DateTime.TryParseExact(intent.GetStringExtra("night"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var night) || QiyamSettings.Load(night).Completed) return;
            try { var location = new LocationService().Saved(); if (location is null || DateTimeOffset.Now >= QiyamService.Calculate(location.Location, night).Fajr) return; } catch { return; }
        }
        if (!NotificationManagerCompat.From(context)!.AreNotificationsEnabled()) return;
        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        using var channel = new NotificationChannel("qiyam", "تذكير قيام الليل", NotificationImportance.Default); manager.CreateNotificationChannel(channel);
        using var open = new Intent(context, typeof(MainActivity)); open.PutExtra("open_qiyam", true); open.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        using var pending = PendingIntent.GetActivity(context, 9400, open, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        using var builder = new NotificationCompat.Builder(context, "qiyam"); builder.SetSmallIcon(context.Resources!.GetIdentifier("notification_wird", "drawable", context.PackageName));
        builder.SetContentTitle(test ? "اختبار تذكير قيام الليل" : "لحظات هادئة لقيام الليل");
        builder.SetContentText(test ? "التذكير المحلي يعمل. اضغط لفتح قيام الليل." : "اقترب موعدك المختار. افتح خطة الليلة ومواقيتها."); builder.SetAutoCancel(true); builder.SetContentIntent(pending);
        using var notification = builder.Build(); manager.Notify(9400, notification); if (!test) QiyamCoordinator.Refresh();
    }
}
