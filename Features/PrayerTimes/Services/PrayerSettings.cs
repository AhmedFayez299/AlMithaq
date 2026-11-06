using Batoulapps.Adhan;

namespace AlMithaq.Features.PrayerTimes.Services;

public static class PrayerSettings
{
    public static readonly (string Name, CalculationMethod Method)[] Methods =
    [ ("الهيئة المصرية للمساحة", CalculationMethod.EGYPTIAN), ("أم القرى — مكة", CalculationMethod.UMM_AL_QURA),
      ("رابطة العالم الإسلامي", CalculationMethod.MUSLIM_WORLD_LEAGUE), ("كراتشي", CalculationMethod.KARACHI),
      ("دبي", CalculationMethod.DUBAI), ("الكويت", CalculationMethod.KUWAIT), ("قطر", CalculationMethod.QATAR),
      ("أمريكا الشمالية", CalculationMethod.NORTH_AMERICA), ("سنغافورة", CalculationMethod.SINGAPORE) ];
    public static int MethodIndex { get => Math.Clamp(Preferences.Get("prayer.method", 0), 0, Methods.Length - 1); set => Preferences.Set("prayer.method", value); }
    public static CalculationMethod Method => Methods[MethodIndex].Method;
    public static Madhab Madhab => Preferences.Get("prayer.hanafi", false) ? Madhab.HANAFI : Madhab.SHAFI;
    public static int Adjustment => Math.Clamp(Preferences.Get("prayer.adjustment", 0), -30, 30);
    public static bool RamadanIsha => Preferences.Get("prayer.ramadan", false);
}

public sealed class PrayerService : IPrayerService
{
    public IReadOnlyList<AlMithaq.Core.Logic.PrayerMoment> GetDay(Location location, DateTime date) =>
        AlMithaq.Core.Logic.PrayerCalculator.Calculate(location.Latitude, location.Longitude, date, TimeZoneInfo.Local,
            PrayerSettings.Method, PrayerSettings.Madhab, PrayerSettings.Adjustment, PrayerSettings.RamadanIsha);
    public Task<PrayerInfo> GetNextPrayer(Location location)
    {
        var prayer = AlMithaq.Core.Logic.PrayerCalculator.Next(location.Latitude, location.Longitude, DateTime.Now, TimeZoneInfo.Local,
            PrayerSettings.Method, PrayerSettings.Madhab, PrayerSettings.Adjustment, PrayerSettings.RamadanIsha);
        return Task.FromResult(new PrayerInfo { Name = prayer.Name, Time = prayer.Time });
    }
}
