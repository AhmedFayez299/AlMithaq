

namespace AlMithaq.Services.PrayerService
{
    public interface IPrayerService
    {
        IReadOnlyList<AlMithaq.Core.Logic.PrayerMoment> GetDay(Location location, DateTime date);
        Task<PrayerInfo> GetNextPrayer(Location location);
    }
}
