using AlMithaq.Shared.Controls;

namespace AlMithaq;

public partial class AppShell : FlyoutPage
{
    private readonly AppNavigator _navigator;
    public AppShell(AppNavigator navigator)
    {
        _navigator = navigator;
        StartupTrace.Write("Root constructor begin");
        InitializeComponent(); Title = "الميثاق";
        FlyoutLayoutBehavior = FlyoutLayoutBehavior.Popover;
        var title = Ui.Label("الميثاق", 21, true); title.TextColor = Colors.White;
        var subtitle = Ui.Label("لحظات تقربك... وتملأ يومك طمأنينة", 13); subtitle.TextColor = Color.FromArgb("#EEE1BE");
        var headerArt = new Grid { HeightRequest = 150, IsClippedToBounds = true };
        headerArt.Add(new Image { Source = "menu_reflection.png", Aspect = Aspect.AspectFill });
        var headerText = Ui.Stack(title, subtitle); headerText.Spacing = 6; headerText.FlowDirection = FlowDirection.RightToLeft; headerText.VerticalOptions = LayoutOptions.Center;
        var headerLayout = new Grid { Padding = 16, FlowDirection = FlowDirection.LeftToRight, ColumnDefinitions = { new(new GridLength(.85, GridUnitType.Star)), new(GridLength.Star) }, RowDefinitions = { new(48), new(GridLength.Star) } };
        var headerTheme = Ui.ThemeToggle(); headerTheme.HorizontalOptions = LayoutOptions.End; headerLayout.Add(headerTheme, 1); headerLayout.Add(headerText, 1, 1);
        headerArt.Add(headerLayout);
        var header = new Border { Content = headerArt, Padding = 0, StrokeThickness = 0, BackgroundColor = Ui.Green, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 } };
        var menu = Ui.Stack(); menu.Spacing = 4;
        var buttons = new Dictionary<string, NavigationMenuItem>();
        (string Title, string Caption, string Glyph, string Route)[] items = [
            ("الرئيسية", "وردك وصلاتك في مكان واحد", "\uf015", "home"),
            ("القرآن الكريم", "قراءة وتلاوة وتدبّر", "\uf02d", "quran"),
            ("خطة الختمة", "ورد يومي حتى موعدك", "\uf02d", "khatmah"),
            ("الحفظ والمراجعة", "تدرّب وراجع تقدمك", "\uf02e", "hifz"),
            ("الأذكار والأدعية", "طمأنينة في كل وقت", "\uf004", "azkar"),
            ("مواقيت الصلاة", "على موعد مع الصلاة", "\uf017", "prayer"),
            ("قيام الليل", "ورد الليلة وتذكير السحر", "\uf186", "qiyam"),
            ("اتجاه القبلة", "اعرف وجهتك", "\uf14e", "qibla"),
            ("السبحة", "ذكر حاضر في يومك", "\uf192", "tasbeeh"),
            ("حاسبة الزكاة", "المال والذهب وبقية الأنواع", "\uf1ec", "zakat"),
            ("الإعدادات", "المظهر والأذان والتفضيلات", "\uf013", "settings"),
            ("دليل استخدام الميثاق", "شرح الميزات خطوة بخطوة", "\uf059", "guide"),
            ("المصادر والتراخيص", "مراجع المحتوى وحقوقه", "\uf02d", "sources")];
        foreach (var item in items)
        {
            var button = new NavigationMenuItem(item.Title, item.Caption, item.Glyph, () => navigator.GoToAsync(item.Route));
            buttons[item.Route] = button; menu.Add(button);
        }
        navigator.Navigated += route =>
        {
            foreach (var pair in buttons)
            {
                pair.Value.SetSelected(pair.Key == route);
            }
        };
        var separator = new BoxView { HeightRequest = 1 }; Ui.ThemeProperty(separator, BoxView.ColorProperty, "AppStroke");
        var footerTitle = Ui.Label("الميثاق · رفيقك للذكر والقرآن", 12, true);
        var footerNote = Ui.Label("لحظات تقربك... وتملأ يومك طمأنينة", 10); Ui.ThemeProperty(footerNote, Label.TextColorProperty, "AppMuted");
        var version = Ui.Label($"الإصدار {AppInfo.Current.VersionString}", 10); Ui.ThemeProperty(version, Label.TextColorProperty, "AppMuted");
        var footer = Ui.Stack(separator, footerTitle, footerNote, version); footer.Spacing = 3; footer.Padding = new Thickness(8, 10, 8, 4);
        var layout = new Grid { Padding = new Thickness(14, 12, 14, 10), RowSpacing = 10, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        layout.Add(header); layout.Add(new ScrollView { Content = menu }, 0, 1); layout.Add(footer, 0, 2);
        Flyout = new ContentPage { Title = "الميثاق", BackgroundColor = Ui.Paper, Content = layout, FlowDirection = FlowDirection.RightToLeft };
        Ui.ThemeProperty(Flyout, ContentPage.BackgroundColorProperty, "AppPaper");
        StartupTrace.Write("Flyout menu ready");
        navigator.Attach(this);
        if (Preferences.Get("salawat.notification-open", false)) Dispatcher.Dispatch(async () => await OpenSalawatAsync());
        if (Preferences.Get("qiyam.notification-open", false)) Dispatcher.Dispatch(async () => await OpenQiyamAsync());
        if (Preferences.Get("wird.notification-open", false)) Dispatcher.Dispatch(async () => await OpenWirdAsync());
        FlowDirection = FlowDirection.RightToLeft;
        StartupTrace.Write("Root constructor complete");
    }
    public async Task OpenWirdAsync()
    {
        Preferences.Remove("wird.notification-open");
        if (AlMithaq.Features.Quran.Services.DailyWirdStore.Load() is not { } plan) return;
        Preferences.Set("quran.wird-page.request", plan.StartPage); await _navigator.GoToAsync("quran");
    }
    public Task OpenSalawatAsync() { Preferences.Remove("salawat.notification-open"); return _navigator.GoToAsync("settings"); }
    public Task OpenQiyamAsync()
    {
        Preferences.Remove("qiyam.notification-open"); return _navigator.GoToAsync("qiyam");
    }
    public Task GoBackAsync() => _navigator.GoBackAsync();
    protected override bool OnBackButtonPressed()
    {
        if (IsPresented) { IsPresented = false; return true; }
        if (Detail is NavigationPage current && current.CurrentPage is ContentPage content && PopupSheet.Dismiss(content)) return true;
        if (base.OnBackButtonPressed()) return true;
        if (Detail is NavigationPage navigation && navigation.CurrentPage is not AlMithaq.Pages.HomePage)
        { _ = _navigator.GoBackAsync(); return true; }
        _ = ExitSnackbar.ShowAsync(); return true;
    }
}

