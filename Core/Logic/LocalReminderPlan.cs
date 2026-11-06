namespace AlMithaq.Core.Logic;

public static class LocalReminderPlan
{
    public static bool IsValid(int startMinute, int endMinute, int interval) => startMinute is >= 0 and < 1440 && endMinute is >= 0 and < 1440 && startMinute != endMinute && interval is >= 15 and <= 360;
    public static bool IsWithinWindow(DateTime local, int startMinute, int endMinute)
    {
        var minute = local.TimeOfDay.TotalMinutes;
        return startMinute < endMinute ? minute >= startMinute && minute < endMinute : minute >= startMinute || minute < endMinute;
    }
    public static DateTimeOffset Next(DateTimeOffset now, TimeZoneInfo zone, int startMinute, int endMinute, int interval)
    {
        if (!IsValid(startMinute, endMinute, interval)) throw new ArgumentException("اختر وقت بداية ونهاية مختلفين وفاصل تذكير صحيحًا.");
        var day = TimeZoneInfo.ConvertTime(now, zone).Date;
        DateTimeOffset? next = null;
        for (var d = -1; d <= 2; d++)
        {
            var start = day.AddDays(d).AddMinutes(startMinute);
            var end = day.AddDays(d + (endMinute <= startMinute ? 1 : 0)).AddMinutes(endMinute);
            for (var local = start; local < end; local = local.AddMinutes(interval))
            {
                if (zone.IsInvalidTime(local)) continue;
                var instant = QiyamWindow.Instant(local, zone);
                if (instant > now && (next is null || instant < next)) next = instant;
            }
        }
        return next ?? throw new InvalidOperationException("تعذر تحديد التذكير القادم.");
    }
}
