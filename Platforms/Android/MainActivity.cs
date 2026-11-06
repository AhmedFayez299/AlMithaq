using Android.App;
using Android.Content.PM;
using Android.OS;

namespace AlMithaq
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = false, ScreenOrientation = ScreenOrientation.Portrait, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public partial class MainActivity : MauiAppCompatActivity
    {
        public override bool DispatchTouchEvent(Android.Views.MotionEvent? touch)
        {
            if (touch?.ActionMasked == Android.Views.MotionEventActions.Down && CurrentFocus is Android.Widget.EditText input)
            {
                var origin = new int[2];
                input.GetLocationOnScreen(origin);
                if (touch.RawX < origin[0] || touch.RawX >= origin[0] + input.Width ||
                    touch.RawY < origin[1] || touch.RawY >= origin[1] + input.Height)
                {
                    if (GetSystemService(Android.Content.Context.InputMethodService) is Android.Views.InputMethods.InputMethodManager keyboard)
                        keyboard.HideSoftInputFromWindow(input.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
                    input.ClearFocus();
                }
            }
            return base.DispatchTouchEvent(touch);
        }
        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            base.OnNewIntent(intent); if (intent is not null) Intent = intent;
        }
        protected override void OnResume()
        {
            base.OnResume();
            CheckPlayUpdate();
            if (Intent?.GetBooleanExtra("open_wird", false) == true)
            {
                Intent.RemoveExtra("open_wird"); Preferences.Set("wird.notification-open", true);
                if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is AppShell shell) _ = shell.OpenWirdAsync();
            }
            if (Intent?.GetBooleanExtra("open_qiyam", false) == true)
            {
                Intent.RemoveExtra("open_qiyam"); Preferences.Set("qiyam.notification-open", true);
                if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is AppShell qiyamShell) _ = qiyamShell.OpenQiyamAsync();
            }
            if (Intent?.GetBooleanExtra("open_salawat", false) == true)
            {
                Intent.RemoveExtra("open_salawat"); Preferences.Set("salawat.notification-open", true);
                if (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page is AppShell salawatShell) _ = salawatShell.OpenSalawatAsync();
            }
            AlMithaq.Features.Quran.Services.WirdReminders.Refresh();
            AlMithaq.Features.PrayerTimes.Services.AdhanCoordinator.Refresh();
        }
    }
}
