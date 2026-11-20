#if IOS
using AlMithaq.Shared.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace AlMithaq;

public sealed class AppShell : Microsoft.Maui.Controls.NavigationPage
{
    private readonly AppNavigator _navigator;
    private ContentPage? _menu;
    private bool _openingMenu;
    public AppShell(AppNavigator navigator) : base(navigator.CreateHome())
    {
        _navigator = navigator;
        BarBackgroundColor = Ui.Green; BarTextColor = Colors.White; Ui.ThemeProperty(this,BackgroundColorProperty,"AppPaper");
        FlowDirection = FlowDirection.LeftToRight;
        navigator.Attach(this);
    }

    private bool _checkedLaunchRequests;
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Window is { } window) _ = Platforms.iOS.AppStoreUpdates.CheckAsync(window);
        if (_checkedLaunchRequests) return;
        _checkedLaunchRequests = true;
        Dispatcher.Dispatch(async () =>
        {
            if (Preferences.Get("adhan.notification-open", false)) await OpenAdhanAsync();
            if (Preferences.Get("salawat.notification-open", false)) await OpenSalawatAsync();
            if (Preferences.Get("qiyam.notification-open", false)) await OpenQiyamAsync();
            if (Preferences.Get("wird.notification-open", false)) await OpenWirdAsync();
        });
    }




    public async Task ShowMenuAsync()
    {
        if (_menu is not null || _openingMenu) return;
        _openingMenu = true;
        try
        {
            var menu = new ContentPage { BackgroundColor = Color.FromArgb("#99061C15"), FlowDirection = FlowDirection.RightToLeft };
            var items = Ui.Stack(); items.Spacing = 4;
            var header = new MenuBrandHeader(CloseMenuAsync);
            (string Title, string Glyph, string Route)[] routes = [
                ("الرئيسية", "\uf015", "home"), ("القرآن الكريم", "\uf02d", "quran"),
                ("خطة الختمة", "\uf02d", "khatmah"), ("الحفظ والمراجعة", "\uf02e", "hifz"),
                ("الأذكار والأدعية", "\uf004", "azkar"), ("مواقيت الصلاة", "\uf017", "prayer"),
                ("قيام الليل", "\uf186", "qiyam"), ("اتجاه القبلة", "\uf14e", "qibla"),
                ("السبحة", "\uf192", "tasbeeh"), ("حاسبة الزكاة", "\uf1ec", "zakat"),
                ("الإعدادات", "\uf013", "settings"), ("دليل استخدام الميثاق", "\uf059", "guide"),
                ("المصادر والتراخيص", "\uf02d", "sources")];
            foreach (var route in routes)
            {
                if (route.Route is "home" or "prayer" or "settings") items.Add(MenuBrandHeader.Group(route.Route == "home" ? "تلاوة وذكر" : route.Route == "prayer" ? "عبادات وأدوات" : "تفضيلات ومساعدة"));
                items.Add(new NavigationMenuItem(route.Title,"",route.Glyph,()=>_navigator.GoToAsync(route.Route)));
            }
            var separator = new BoxView { HeightRequest = 1 }; Ui.ThemeProperty(separator, BoxView.ColorProperty, "AppStroke");
            var footerTitle = Ui.Label("الميثاق", 12, true);
            var footerNote = Ui.Label("لحظات تقربك... وتملأ يومك طمأنينة", 10); Ui.ThemeProperty(footerNote, Label.TextColorProperty, "AppMuted");
            var version = Ui.Label($"الإصدار {AppInfo.Current.VersionString}", 10); Ui.ThemeProperty(version, Label.TextColorProperty, "AppMuted");
            var footer = Ui.Stack(separator,Ui.Row(footerTitle,version),footerNote); footer.Spacing = 3;
            footer.FlowDirection = FlowDirection.RightToLeft;
            var layout = new Grid { RowSpacing = 10, FlowDirection = FlowDirection.RightToLeft,
                RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
            layout.Add(header); layout.Add(new Microsoft.Maui.Controls.ScrollView { Content = items }, 0, 1); layout.Add(footer, 0, 2);
            var overlay = new Grid { FlowDirection = FlowDirection.LeftToRight };
            var dismiss = new Button { BackgroundColor = Colors.Transparent };
            dismiss.Clicked += async (_, _) => await CloseMenuAsync();
            SemanticProperties.SetDescription(dismiss, "إغلاق القائمة");
            overlay.Add(dismiss);
            var panel = new Border
            {
                WidthRequest = Math.Min(340, Math.Max(240, Width - 48)), HorizontalOptions = LayoutOptions.End,
                Padding = new Thickness(16, 16), StrokeThickness = 0, BackgroundColor = Ui.Paper,
                Content = layout
            };
            Ui.ThemeProperty(panel, Border.BackgroundColorProperty, "AppPaper");
            overlay.Add(panel); menu.Content = overlay;
            menu.On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);
            _menu = menu;
            try { await Navigation.PushModalAsync(menu, false); }
            catch { _menu = null; throw; }
        }
        finally { _openingMenu = false; }
    }

    public async Task CloseMenuAsync()
    {
        if (_menu is not { } menu || Navigation.ModalStack.LastOrDefault() != menu) return;
        await Navigation.PopModalAsync(false); _menu = null;
    }

    public async Task OpenAdhanAsync()
    {
        Preferences.Remove("adhan.notification-open");
        var position = Preferences.Get("adhan.notification-resume-seconds", 29.8);
        var voice = Preferences.Get("adhan.notification-voice", Features.PrayerTimes.Services.AdhanSettings.Voice);
        Preferences.Remove("adhan.notification-resume-seconds");
        Preferences.Remove("adhan.notification-voice");
        await _navigator.GoToAsync("settings");
        Platforms.iOS.ReminderAudio.Play(Features.PrayerTimes.Services.AdhanSettings.AudioName(voice),
            Features.PrayerTimes.Services.AdhanSettings.AudioExtension(voice), Features.PrayerTimes.Services.AdhanSettings.Volume, position);
    }

    public Task GoBackAsync() => _navigator.GoBackAsync();
    public Task OpenSalawatAsync() { Preferences.Remove("salawat.notification-open"); return _navigator.GoToAsync("settings"); }
    public Task OpenQiyamAsync() { Preferences.Remove("qiyam.notification-open"); return _navigator.GoToAsync("qiyam"); }
    public Task OpenWirdAsync()
    {
        Preferences.Remove("wird.notification-open");
        if (Features.Quran.Services.DailyWirdStore.Load() is not { } plan) return Task.CompletedTask;
        Preferences.Set("quran.wird-page.request", plan.StartPage); return _navigator.GoToAsync("quran");
    }
}

#endif
