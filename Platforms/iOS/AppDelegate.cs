using Foundation;
using UIKit;
using UserNotifications;

namespace AlMithaq;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private readonly ReminderDelegate _reminders = new();

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        var launched = base.FinishedLaunching(application, launchOptions);
        if (launched) UNUserNotificationCenter.Current.Delegate = _reminders;
        return launched;
    }

    private sealed class ReminderDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification,
            Action<UNNotificationPresentationOptions> completionHandler)
            => completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);

        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response,
            Action completionHandler)
        {
            var id = response.Notification.Request.Identifier;
            // iOS exposes delivery time, but not the notification sound's playback position.
            // Snapshot elapsed time at the tap, before a cold launch or navigation adds delay.
            var parts = id.Split('.');
            var kind = parts.ElementAtOrDefault(2) == "test" ? parts.ElementAtOrDefault(3) : parts.ElementAtOrDefault(2);
            if (kind == "adhan")
            {
                var info = response.Notification.Request.Content.UserInfo;
                var voice = info[new NSString("adhan.voice")] is NSNumber selected
                    ? selected.Int32Value : Features.PrayerTimes.Services.AdhanSettings.Voice;
                var duration = info[new NSString("adhan.clip-seconds")] is NSNumber seconds
                    ? seconds.DoubleValue : voice == 0 ? 27.5 : 28.7;
                var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d - response.Notification.Date.SecondsSince1970;
                Preferences.Set("adhan.notification-resume-seconds", Math.Clamp(elapsed, 0, duration));
                Preferences.Set("adhan.notification-voice", voice);
            }
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (kind is not ("adhan" or "wird" or "qiyam" or "salawat")) return;
                    if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is AppShell shell)
                    {
                        await (kind switch
                        {
                            "adhan" => shell.OpenAdhanAsync(),
                            "wird" => shell.OpenWirdAsync(),
                            "qiyam" => shell.OpenQiyamAsync(),
                            _ => shell.OpenSalawatAsync()
                        });
                    }
                    else Preferences.Set(kind + ".notification-open", true);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            });
            completionHandler();
        }
    }
}
