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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var kind = id.Split('.').ElementAtOrDefault(2);
                    if (kind is not ("wird" or "qiyam" or "salawat")) return;
                    if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is AppShell shell)
                    {
                        await (kind switch
                        {
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
