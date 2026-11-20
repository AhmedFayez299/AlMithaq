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
    public async Task RefreshNameAsync(CancellationToken ct)
    {
        if (Saved() is not { } saved || saved.Description.StartsWith("موقع يدوي")) return;
#if IOS
        if (saved.Description.Any(IsArabic) && Preferences.Get("location.ios-detail.v1", false)) return;
#else
        if (saved.Description.Any(IsArabic)) return;
#endif
        var description = await CityNameAsync(saved.Location, saved.Description, ct);
        var current = Saved();
        if (current?.Location.Latitude == saved.Location.Latitude && current.Location.Longitude == saved.Location.Longitude)
            Preferences.Set("location.description", description);
    }
    private static async Task<string> CityNameAsync(Location location, string fallback, CancellationToken ct)
    {
        try
        {
#if ANDROID
            using var locale = Java.Util.Locale.ForLanguageTag("ar");
            using var geocoder = new global::Android.Locations.Geocoder(global::Android.App.Application.Context, locale);
            IList<global::Android.Locations.Address>? places;
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                using var listener = new ArabicGeocodeListener();
                geocoder.GetFromLocation(location.Latitude, location.Longitude, 1, listener);
                places = await listener.Result.Task.WaitAsync(TimeSpan.FromSeconds(6), ct);
            }
            else
            {
#pragma warning disable CA1422 // Older Android devices require the legacy geocoder overload.
                places = await geocoder.GetFromLocationAsync(location.Latitude, location.Longitude, 1).WaitAsync(TimeSpan.FromSeconds(6), ct);
#pragma warning restore CA1422
            }
            var place = places?.FirstOrDefault();
            var candidates = new[] { place?.Locality, place?.SubAdminArea, place?.AdminArea };
#elif IOS
#pragma warning disable CA1422 // Same reverse-geocoding service as MAUI, explicitly requesting Arabic.
            using var geocoder = new CoreLocation.CLGeocoder();
            using var coordinate = new CoreLocation.CLLocation(location.Latitude, location.Longitude);
            using var locale = new Foundation.NSLocale("ar");
            var places = await geocoder.ReverseGeocodeLocationAsync(coordinate, locale).WaitAsync(TimeSpan.FromSeconds(6), ct);
#pragma warning restore CA1422
            var place = places.FirstOrDefault();
            var details = new[] { place?.SubLocality, place?.Locality, place?.SubAdministrativeArea, place?.AdministrativeArea }
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray();
            if (details.Length > 0)
            {
                Preferences.Set("location.ios-detail.v1", true);
                return string.Join("، ", details.Take(2));
            }
            var candidates = Array.Empty<string>();
#else
            var places = await Geocoding.Default.GetPlacemarksAsync(location).WaitAsync(TimeSpan.FromSeconds(6), ct);
            var place = places.FirstOrDefault();
            var candidates = new[] { place?.Locality, place?.SubAdminArea, place?.AdminArea };
#endif
            return candidates.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && value.Any(IsArabic))
                ?? fallback;

        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); return fallback; }
    }
    private static bool IsArabic(char character) => character is >= '\u0600' and <= '\u06ff';
#if ANDROID
    [System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private sealed class ArabicGeocodeListener : Java.Lang.Object, global::Android.Locations.Geocoder.IGeocodeListener
    {
        public TaskCompletionSource<IList<global::Android.Locations.Address>> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnGeocode(IList<global::Android.Locations.Address> addresses) => Result.TrySetResult(addresses);
        public void OnError(string? message) => Result.TrySetResult([]);
    }
#endif
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
            Save(location, description);
            description = await CityNameAsync(location, description, ct);
            Preferences.Set("location.description", description);
            return new(location, description, false);
        }
        finally { _gate.Release(); }
    }
}
