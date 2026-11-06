namespace AlMithaq.Core.Logic;

public sealed record QiyamWindow(DateTimeOffset Sunset, DateTimeOffset Isha, DateTimeOffset Fajr)
{
    public bool IsValid => Fajr > Sunset && Fajr - Sunset < TimeSpan.FromHours(24) && Isha >= Sunset && Isha < Fajr;
    public DateTimeOffset LastThird => Fajr - TimeSpan.FromTicks((Fajr - Sunset).Ticks / 3);
    public bool IsPrayerTime(DateTimeOffset now) => IsValid && now >= Isha && now < Fajr;
    public DateTimeOffset ReminderAt(int minutesBefore)
    {
        if (!IsValid || minutesBefore is < 0 or > 60) throw new ArgumentOutOfRangeException(nameof(minutesBefore));
        var at = LastThird.AddMinutes(-minutesBefore); return at < Isha ? Isha : at;
    }
    public static DateTimeOffset Instant(DateTime local, TimeZoneInfo zone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) throw new ArgumentException("وقت محلي غير متاح بسبب تغيير الساعة");
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return new(local, offset);
    }
}
