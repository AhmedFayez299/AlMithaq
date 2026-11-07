using AlMithaq.Core.Logic;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Features.Qiyam.Services;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Features.Salawat.Services;
using UserNotifications;

namespace AlMithaq.Platforms.iOS;

internal static class LocalReminders
{
    private const string Prefix = "almithaq.reminder.";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static UNUserNotificationCenter Center => UNUserNotificationCenter.Current;

    internal static async Task<bool> RequestPermissionAsync()
    {
        var result = await Center.RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound);
        return result.Item1;
    }

    internal static async Task RefreshAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var pending = await Center.GetPendingNotificationRequestsAsync();
            var settings = await Center.GetNotificationSettingsAsync();
            var existing = pending.Select(request => request.Identifier)
                .Where(id => id.StartsWith(Prefix, StringComparison.Ordinal) && !id.StartsWith(Prefix + "test.", StringComparison.Ordinal)).ToArray();
            var reminders = settings.AuthorizationStatus == UNAuthorizationStatus.Authorized
                ? Build(DateTimeOffset.Now).Take(Math.Max(0, 63 - (pending.Length - existing.Length))).ToArray()
                : [];
            var desired = reminders.Select(Id).ToHashSet(StringComparer.Ordinal);
            Center.RemovePendingNotificationRequests(existing.Where(id => !desired.Contains(id)).ToArray());
            foreach (var reminder in reminders)
                await ScheduleAsync(Id(reminder), reminder);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        finally { Gate.Release(); }
    }

    private static string Id(Reminder reminder) => $"{Prefix}{reminder.Kind}.{reminder.At.ToUnixTimeSeconds()}";

    private static IEnumerable<Reminder> Build(DateTimeOffset now)
    {
        var zone = TimeZoneInfo.Local;
        var local = now.LocalDateTime;
        if (new LocationService().Saved() is { } location)
        {
            if (AdhanSettings.Enabled)
            {
                var prayer = new PrayerService();
                var selected = Enumerable.Range(0, 5).Where(AdhanSettings.ForPrayer).Select(i => AdhanSettings.Prayers[i]).ToHashSet();
                foreach (var alarm in AdhanPlan.Build(local, zone, date => prayer.GetDay(location.Location, date), selected).Take(30))
                    yield return new("adhan", DateTimeOffset.FromUnixTimeMilliseconds(alarm.At), "حان وقت الصلاة", $"حان الآن موعد صلاة {alarm.Name}.");
            }
            if (QiyamSettings.Enabled)
            {
                var evening = QiyamService.CurrentEvening(location.Location);
                for (var day = 0; day < 7; day++)
                {
                    var date = evening.AddDays(day);
                    if (QiyamSettings.Load(date).Completed) continue;
                    DateTimeOffset at;
                    try { at = QiyamService.Calculate(location.Location, date).ReminderAt(QiyamSettings.LeadMinutes); }
                    catch (InvalidOperationException) { continue; }
                    if (at > now) yield return new("qiyam", at, "قيام الليل", "حان موعد تذكيرك بقيام الليل.");
                }
            }
        }
        if (WirdReminders.Enabled && DailyWirdStore.ForReminders() is { IsValid: true } plan)
            foreach (var at in WirdReminderPlan.Build(local).Where(at => !plan.IsComplete(DateOnly.FromDateTime(at))).Take(10))
                yield return new("wird", new DateTimeOffset(at), "لحظات مع وردك اليومي", "بقي وردك لهذا اليوم. افتح المصحف وأكمل القراءة.");

        if (SalawatSettings.Enabled && SalawatSettings.Valid)
        {
            var at = now;
            for (var i = 0; i < 16; i++)
            {
                at = LocalReminderPlan.Next(at, zone, SalawatSettings.Start, SalawatSettings.End, SalawatSettings.Intervals[SalawatSettings.IntervalIndex]);
                yield return new("salawat", at, "تذكير بالصلاة على النبي", "صلِّ على النبي ﷺ", SalawatSettings.Sound);
            }
        }
    }

    internal static async Task TestAsync(string kind, string title, bool sound = true)
    {
        if (!await RequestPermissionAsync()) return;
        await ScheduleAsync(Prefix + "test." + kind, new(kind, DateTimeOffset.Now.AddSeconds(10), title, "هذا تنبيه تجريبي من الميثاق.", sound));
    }

    private static async Task ScheduleAsync(string id, Reminder reminder)
    {
        var seconds = (reminder.At - DateTimeOffset.Now).TotalSeconds;
        if (seconds <= 0) return;
        using var content = new UNMutableNotificationContent { Title = reminder.Title, Body = reminder.Body };
        if (reminder.Sound)
        {
            content.Sound = reminder.Kind switch
            {
                "adhan" => UNNotificationSound.GetSound(AdhanSettings.Voice == 0 ? "adhan_ali_notice.caf" : "adhan_haddad_notice.caf"),
                "salawat" => UNNotificationSound.GetSound("salawat_notice.caf"),
                _ => UNNotificationSound.Default
            };
        }
        using var trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(Math.Max(1, seconds), false);
        using var request = UNNotificationRequest.FromIdentifier(id, content, trigger);
        await Center.AddNotificationRequestAsync(request);
    }

    private sealed record Reminder(string Kind, DateTimeOffset At, string Title, string Body, bool Sound = true);
}
