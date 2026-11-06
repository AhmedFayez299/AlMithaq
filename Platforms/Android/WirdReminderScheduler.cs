using global::Android.App;
using global::Android.Content;
using global::Android.OS;
using global::AndroidX.Core.App;
using AlMithaq.Core.Logic;
using AlMithaq.Features.Quran.Services;
namespace AlMithaq.Platforms.Android;

public static class WirdReminderScheduler
{
    internal const string Channel = "daily_wird";
    internal static PendingIntentFlags Flags => PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
    private static Context Context => global::Android.App.Application.Context;
    public static void Refresh()
    {
        var manager = (AlarmManager)Context.GetSystemService(Context.AlarmService)!;
        for (var slot = 0; slot < 35; slot++)
        {
            using var oldIntent = new Intent(Context, typeof(WirdReminderReceiver));
            using var old = PendingIntent.GetBroadcast(Context, 9200 + slot, oldIntent, Flags)!; manager.Cancel(old);
        }
        if (!WirdReminders.Enabled) NotificationManagerCompat.From(Context)!.Cancel(9200);
        if (!WirdReminders.Enabled) return;
        // Derive a linked khatmah assignment without recursively saving/rescheduling.
        var plan = DailyWirdStore.ForReminders();
        if (plan?.IsValid != true) return;
        foreach (var at in WirdReminderPlan.Build(DateTime.Now))
        {
            if (plan.IsComplete(DateOnly.FromDateTime(at))) continue;
            var slot = (at.Date - DateTime.Today).Days * 5 + at.Hour - 19;
            using var intent = new Intent(Context, typeof(WirdReminderReceiver));
            intent.PutExtra("at", new DateTimeOffset(at).ToUnixTimeMilliseconds());
            using var pending = PendingIntent.GetBroadcast(Context, 9200 + slot, intent, Flags)!;
            var epoch = new DateTimeOffset(at).ToUnixTimeMilliseconds();
            if (!OperatingSystem.IsAndroidVersionAtLeast(31) || manager.CanScheduleExactAlarms()) manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, epoch, pending);
            else manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, epoch, pending);
        }
        if (plan.IsComplete(DateOnly.FromDateTime(DateTime.Today))) NotificationManagerCompat.From(Context)!.Cancel(9200);
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class WirdReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null || !WirdReminders.Enabled) return;
        var scheduled = DateTimeOffset.FromUnixTimeMilliseconds(intent.GetLongExtra("at", 0)).LocalDateTime;
        var plan = DailyWirdStore.Load();
        if (!WirdReminderPlan.ShouldNotify(plan, DateTime.Now, scheduled)) return;
        var last = Preferences.Get("wird.last-notified", 0L);
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - last < 55 * 60 * 1000) return;
        if (!NotificationManagerCompat.From(context)!.AreNotificationsEnabled()) return;
        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        using var channel = new NotificationChannel(WirdReminderScheduler.Channel, "تذكير الورد اليومي", NotificationImportance.Default);
        manager!.CreateNotificationChannel(channel);
        using var open = new Intent(context, typeof(MainActivity)); open.PutExtra("open_wird", true); open.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        using var pending = PendingIntent.GetActivity(context, 9200, open, WirdReminderScheduler.Flags)!;
        var icon = context.Resources!.GetIdentifier("notification_wird", "drawable", context.PackageName);
        using var builder = new NotificationCompat.Builder(context, WirdReminderScheduler.Channel);
        builder.SetSmallIcon(icon); builder.SetContentTitle("لحظات مع وردك اليومي");
        builder.SetContentText($"بقي وردك: الصفحات {plan!.StartPage}–{plan.EndPage}. افتح المصحف وأكمل بهدوء.");
        builder.SetContentIntent(pending); builder.SetAutoCancel(true); builder.SetOnlyAlertOnce(false);
        using var notification = builder.Build();
        manager.Notify(9200, notification);
        Preferences.Set("wird.last-notified", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        WirdReminders.Refresh();
    }
}
