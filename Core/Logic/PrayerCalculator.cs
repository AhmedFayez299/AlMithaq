using Batoulapps.Adhan;
using Batoulapps.Adhan.Internal;

namespace AlMithaq.Core.Logic;

public sealed record PrayerMoment(string Name, DateTime Time, bool IsPrayer = true);

public static class PrayerCalculator
{
    public static IReadOnlyList<PrayerMoment> Calculate(double latitude, double longitude, DateTime date,
        TimeZoneInfo zone, CalculationMethod method, Madhab madhab, int adjustmentMinutes = 0, bool ramadanIsha = false)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        var parameters = method.GetParameters(); parameters.Madhab = madhab;
        if (method == CalculationMethod.UMM_AL_QURA && ramadanIsha) parameters.IshaInterval += 30;
        var times = new PrayerTimes(new Coordinates(latitude, longitude), new DateComponents(date.Year, date.Month, date.Day), parameters);
        DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone).AddMinutes(adjustmentMinutes);
        if (times.Fajr == default || times.Sunrise == default || times.Isha == default)
            throw new InvalidOperationException("تعذر حساب المواقيت لهذا الموقع والتاريخ. راجع الجهة المحلية للمواقيت في مناطق خطوط العرض العليا.");
        return [new("الفجر", Local(times.Fajr)), new("الشروق", Local(times.Sunrise), false), new("الظهر", Local(times.Dhuhr)),
            new("العصر", Local(times.Asr)), new("المغرب", Local(times.Maghrib)), new("العشاء", Local(times.Isha))];
    }

    public static PrayerMoment Next(double latitude, double longitude, DateTime now, TimeZoneInfo zone,
        CalculationMethod method, Madhab madhab, int adjustmentMinutes = 0, bool ramadanIsha = false)
    {
        // Calculate tomorrow independently; adding 24 hours to today's fajr is not correct.
        return Calculate(latitude, longitude, now.Date, zone, method, madhab, adjustmentMinutes, ramadanIsha)
            .Where(p => p.IsPrayer && p.Time > now).FirstOrDefault()
            ?? Calculate(latitude, longitude, now.Date.AddDays(1), zone, method, madhab, adjustmentMinutes, ramadanIsha).First();
    }
}
