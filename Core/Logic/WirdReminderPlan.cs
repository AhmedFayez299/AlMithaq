namespace AlMithaq.Core.Logic;

public static class WirdReminderPlan
{
    // The last five clock hours of the local day, with no retrospective reminders.
    public static IEnumerable<DateTime> Build(DateTime now, int days = 7)
    {
        for (var day = 0; day < days; day++)
            for (var hour = 19; hour <= 23; hour++)
            {
                var at = now.Date.AddDays(day).AddHours(hour);
                if (at > now) yield return at;
            }
    }
    public static bool ShouldNotify(DailyWird? plan, DateTime now, DateTime scheduled)
        => plan?.IsValid == true && !plan.IsComplete(DateOnly.FromDateTime(now)) && now.Date == scheduled.Date
        && scheduled.Hour is >= 19 and <= 23 && now >= scheduled && now - scheduled < TimeSpan.FromMinutes(45);
}
