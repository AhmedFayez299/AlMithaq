using Android.App;
using AlMithaq.Shared.Controls;
using Microsoft.Maui.Platform;
using Android.Content;
using Android.Gms.Tasks;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;

namespace AlMithaq;

public partial class MainActivity
{
    private bool _checkingPlayUpdate;
    private int _offeredUpdateVersion;
    private DateTimeOffset _lastPlayCheck;
    private bool _foreground;

    private void CheckPlayUpdate()
    {
        _foreground = true;
        if (PackageName != "com.companyname.almithaq" || _checkingPlayUpdate || DateTimeOffset.UtcNow - _lastPlayCheck < TimeSpan.FromMinutes(10)) return;
        _lastPlayCheck = DateTimeOffset.UtcNow;
        _checkingPlayUpdate = true;
        try
        {
            var manager = AppUpdateManagerFactory.Create(this);
            manager.GetAppUpdateInfo().AddOnCompleteListener(new UpdateResult(result =>
            {
                _checkingPlayUpdate = false;
                if (!_foreground || IsFinishing || IsDestroyed || !result.IsSuccessful || result.Result is not AppUpdateInfo info) return;
                if (info.UpdateAvailability() != (int)UpdateAvailability.UpdateAvailable) return;
                var version = info.AvailableVersionCode();
                if (version <= _offeredUpdateVersion) return;
                if (ShowPlayUpdate()) _offeredUpdateVersion = version;
            }));
        }
        catch (Exception ex)
        {
            _checkingPlayUpdate = false;
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    protected override void OnPause()
    {
        _foreground = false;
        base.OnPause();
    }

    private bool ShowPlayUpdate()
    {
        var context = Microsoft.Maui.Controls.Application.Current?.Windows
            .FirstOrDefault(window => window.Handler?.MauiContext?.Context == this)?.Handler?.MauiContext
            ?? Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext;
        if (context is null) return false;

        var dialog = new Dialog(this);
        var badge = Ui.Card(new Image { Source = "mithaqbrand.png", WidthRequest = 46, HeightRequest = 46 });
        badge.HorizontalOptions = LayoutOptions.Center;
        badge.Padding = 8;
        badge.BackgroundColor = Ui.Green;

        var title = Ui.Label("تحديث الميثاق متاح", 21, true);
        title.HorizontalTextAlignment = TextAlignment.Center;
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
        var description = Ui.Label("يتوفر إصدار أحدث على Google Play. يمكنك تثبيته الآن أو المتابعة لاحقًا.", 14);
        description.HorizontalTextAlignment = TextAlignment.Center;
        Ui.ThemeProperty(description, Label.TextColorProperty, "AppMuted");

        var update = Ui.Button("تحديث الآن", () =>
        {
            dialog.Dismiss();
            OpenPlayListing();
        });
        var later = Ui.Button("لاحقًا", dialog.Dismiss, secondary: true);
        var actions = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        actions.Add(update);
        actions.Add(later, 1);

        var card = Ui.Card(Ui.Stack(badge, title, description, actions));
        card.FlowDirection = FlowDirection.RightToLeft;
        card.Margin = 0;
        card.Padding = 20;
        dialog.SetContentView(card.ToPlatform(context));
        dialog.SetCanceledOnTouchOutside(true);
        dialog.DismissEvent += (_, _) =>
        {
            card.Handler?.DisconnectHandler();
            dialog.Dispose();
        };
        dialog.Show();
        if (dialog.Window is { } window)
        {
            window.SetBackgroundDrawable(new global::Android.Graphics.Drawables.ColorDrawable(global::Android.Graphics.Color.Transparent));
            var metrics = Resources!.DisplayMetrics!;
            window.SetLayout(Math.Min(metrics.WidthPixels - (int)(32 * metrics.Density), (int)(400 * metrics.Density)), -2);
        }
        return true;
    }

    private void OpenPlayListing()
    {
        var appId = PackageName;
        try
        {
            StartActivity(new Intent(global::Android.Content.Intent.ActionView, global::Android.Net.Uri.Parse($"market://details?id={appId}")).SetPackage("com.android.vending"));
        }
        catch (ActivityNotFoundException)
        {
            try { StartActivity(new Intent(global::Android.Content.Intent.ActionView, global::Android.Net.Uri.Parse($"https://play.google.com/store/apps/details?id={appId}"))); }
            catch (ActivityNotFoundException ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
    }

    private sealed class UpdateResult(Action<global::Android.Gms.Tasks.Task> callback) : Java.Lang.Object, IOnCompleteListener
    {
        public void OnComplete(global::Android.Gms.Tasks.Task task) => callback(task);
    }
}
