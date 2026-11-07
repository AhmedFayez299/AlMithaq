#if IOS
using Foundation;
using UIKit;

namespace AlMithaq.Platforms.iOS;

internal static class AppStoreUpdates
{
#if !DEBUG
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static DateTimeOffset _lastCheck;
    private static string? _offeredVersion;
    private static bool _checking;
#endif

    internal static async Task CheckAsync(Window window)
    {
#if DEBUG
        await Task.CompletedTask;
#else
        // A sandbox receipt belongs to TestFlight. No receipt also covers local development installs.
        var receipt = NSBundle.MainBundle.AppStoreReceiptUrl;
        if (receipt is null || receipt.LastPathComponent != "receipt" || !File.Exists(receipt.Path)) return;
        if (DeviceInfo.Current.DeviceType == DeviceType.Virtual || _checking ||
            DateTimeOffset.UtcNow - _lastCheck < TimeSpan.FromMinutes(10) ||
            window.Page is not AppShell shell || shell.Navigation.ModalStack.Count > 0 ||
            Preferences.Get("adhan.notification-open", false) || Preferences.Get("wird.notification-open", false) ||
            Preferences.Get("salawat.notification-open", false) || Preferences.Get("qiyam.notification-open", false) ||
            UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active) return;

        _checking = true;
        _lastCheck = DateTimeOffset.UtcNow;
        try
        {
            var country = NSLocale.CurrentLocale.CountryCode;
            if (country is null || country.Length != 2 || !country.All(char.IsAsciiLetter)) country = "US";
            var bundleId = AppInfo.Current.PackageName;
            var json = await Client.GetStringAsync($"https://itunes.apple.com/lookup?bundleId={Uri.EscapeDataString(bundleId)}&country={country}");
            var update = AppStoreUpdateInfo.FromLookup(json, bundleId, AppInfo.Current.VersionString, DeviceInfo.Current.VersionString);
            if (update is null || update.Version == _offeredVersion) return;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (window.Page != shell || shell.CurrentPage is not ContentPage page ||
                    shell.Navigation.ModalStack.Count > 0 || UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active) return;
                var accepted = await AppStoreUpdatePrompt.ShowAsync(page, update.Version);
                if (accepted.HasValue) _offeredVersion = update.Version;
                if (accepted == true)
                    await Launcher.Default.OpenAsync(update.StoreUrl);
            });
        }
        catch (Exception ex)
        {
            // Store/network failures must not interrupt reading or show a false update prompt.
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally { _checking = false; }
#endif
    }
}
#endif
