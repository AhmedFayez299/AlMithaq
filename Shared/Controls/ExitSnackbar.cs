using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
namespace AlMithaq.Shared.Controls;

public static class ExitSnackbar
{
    private static bool _showing;
    public static async Task ShowAsync()
    {
        if (_showing) return;
        _showing = true;
        try
        {
            var options = new SnackbarOptions
            {
                BackgroundColor = Ui.Green, TextColor = Colors.White, ActionButtonTextColor = Color.FromArgb("#F1D8A0"),
                CornerRadius = new CornerRadius(10), Font = Microsoft.Maui.Font.OfSize("Almarai-Regular", 14),
                ActionButtonFont = Microsoft.Maui.Font.OfSize("Almarai-Bold", 14)
            };
            await Snackbar.Make("هل تريد الخروج من الميثاق؟", () =>
            {
#if ANDROID
                Platform.CurrentActivity?.Finish();
#else
                Application.Current?.Quit();
#endif
            }, "خروج", TimeSpan.FromSeconds(3), options).Show();
        }
        finally { _showing = false; }
    }
}
