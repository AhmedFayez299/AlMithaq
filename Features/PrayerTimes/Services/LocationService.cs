namespace AlMithaq.Features.PrayerTimes.Services;

public sealed record DeviceLocation(Location Location, string Description, bool Cached);

public sealed class LocationService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public DeviceLocation? Saved()
    {
        if (!Preferences.ContainsKey("location.latitude")) return null;
        return new(new Location(Preferences.Get("location.latitude", 0d), Preferences.Get("location.longitude", 0d)),
            Preferences.Get("location.description", "موقع محفوظ"), true);
    }
    public void SaveManual(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        Save(new Location(latitude, longitude), $"موقع يدوي: {latitude:F3}, {longitude:F3}");
    }
    private static void Save(Location location, string description)
    {
        Preferences.Set("location.latitude", location.Latitude); Preferences.Set("location.longitude", location.Longitude);
        Preferences.Set("location.description", description); Preferences.Set("location.updated", DateTime.UtcNow.ToString("O"));
        AdhanCoordinator.Refresh();
    }
    public async Task<DeviceLocation> AcquireAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) throw new PermissionException("إذن الموقع غير متاح. يمكنك إدخال الإحداثيات يدويًا من الإعدادات.");
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(20)), ct)
                ?? throw new InvalidOperationException("تعذر تحديد الموقع. فعّل GPS أو أدخل الموقع يدويًا.");
            ct.ThrowIfCancellationRequested();
            var description = $"{location.Latitude:F3}, {location.Longitude:F3}";
            // Reverse-geocoding is optional: prayer calculations never depend on network geocoding.
            Save(location, description); return new(location, description, false);
        }
        finally { _gate.Release(); }
    }
}
