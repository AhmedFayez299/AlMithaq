namespace AlMithaq.Core.Logic;

public sealed record AdhanAlarm(int Slot, string Name, long At);
public static class AdhanPlan
{
    public static IReadOnlyList<AdhanAlarm> Build(DateTime now, TimeZoneInfo zone, Func<DateTime, IReadOnlyList<PrayerMoment>> calculate, IReadOnlySet<string> enabled)
    {
        var nowMs = new DateTimeOffset(now, zone.GetUtcOffset(now)).ToUnixTimeMilliseconds();
        var result = new List<AdhanAlarm>();
        for (var day = 0; day < 7; day++)
        {
            var prayers = calculate(now.Date.AddDays(day)).Where(p => p.IsPrayer).ToArray();
            for (var i = 0; i < prayers.Length; i++)
            {
                var prayer = prayers[i];
                var at = new DateTimeOffset(prayer.Time, zone.GetUtcOffset(prayer.Time)).ToUnixTimeMilliseconds();
                if (at > nowMs && enabled.Contains(prayer.Name)) result.Add(new(day * 5 + i, prayer.Name, at));
            }
        }
        return result.OrderBy(a => a.At).ToArray();
    }
    public static bool CanPlay(long scheduled, long now) => now >= scheduled && now - scheduled <= 5 * 60_000;
}
