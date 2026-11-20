using AlMithaq.Core.Abstractions;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Features.Azkar.Services;
using AlMithaq.Shared.Controls;
using System.Globalization;

namespace AlMithaq.Pages;

public partial class HomePage : ContentPage
{
    private readonly IAppNavigator _navigator;
    private readonly IQuranRepository _quran;
    private readonly IReadingStore _reading;
    private readonly LocationService _locations;
    private readonly IPrayerService _prayers;
    private readonly AzkarRepository _azkar;
    private readonly DailyCardsView _verseCards = new("آية وذكر اليوم", true);
    private DailyCard[] _dailyVerses = [], _dailyAdhkar = [];
    private readonly Label _date = Ui.Label("", 14);
    private readonly Label _gregorianDate = Ui.Label("", 12);
    private readonly Label _prayer = Ui.Label("حدد موقعك من مواقيت الصلاة", 23, true);
    private readonly Label _remaining = Ui.Label("", 16);
    private readonly Label _last = Ui.Label("ابدأ رحلتك مع القرآن", 15);
    private readonly Label _verse = new() { FontFamily = "AmiriQuran", FontSize = 24, LineHeight = 1.25 };
    private readonly Label _dua = Ui.Label("جارٍ تحميل ذكر اليوم…", 18);
    private readonly Label _duaReference = Ui.Label("", 11);
    private readonly Label _reference = Ui.Label("", 12);
    private readonly Grid _todayTimes = new() { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 4 };
    private readonly Label _azkarStat = Ui.Label("", 15, true);
    private readonly Label _tasbeehStat = Ui.Label("", 15, true);
    private readonly Label _marksStat = Ui.Label("", 15, true);
    private readonly Label _adhanStatus = Ui.Label("", 10);
    private readonly Label _locationName = Ui.Label("حدد موقعك", 12, true);
    private readonly Label _prayerName = Ui.Label("الصلاة القادمة", 23, true);
    private readonly Label _wirdSummary = Ui.Label("", 12);
    private readonly Label _khatmahSummary = Ui.Label("", 12);
    private readonly Label _daySummary = Ui.Label("", 12);
    private readonly Label _contextTitle = Ui.Label("", 15, true);
    private readonly Label _azkarContextTitle = Ui.Label("", 15, true);
    private readonly HomeProgressRing _dayProgress = new();
    private readonly ProgressBar _readingProgress = new() { ProgressColor = Ui.Green, HeightRequest = 4 };
    private readonly ProgressBar _wirdProgress = new() { ProgressColor = Ui.Green, HeightRequest = 5 };
    private readonly ProgressBar _khatmahProgress = new() { ProgressColor = Ui.Gold, HeightRequest = 5 };
    private DateTime _timesDate, _cardsDate;
    private string _timesLocation = "";
    private IDispatcherTimer? _timer;
    private bool _active, _updating, _wirdEditorOpen;
    private Border? _timesCard;
    private PrayerInfo? _next;
    private readonly List<(Border Cell, Label Name, Label Time, string Prayer)> _prayerCells = [];
    private string? _highlightedPrayer;
    private sealed record ClockSnapshot(DeviceLocation? Location, DateTime Now);
    protected override bool OnBackButtonPressed() => AlMithaq.Features.Quran.Views.WirdCelebration.Dismiss(this) || PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    public HomePage(IQuranRepository quran, IReadingStore reading, LocationService locations, IPrayerService prayers, IAppNavigator navigator, AzkarRepository azkar)
    {
        StartupTrace.Write("Home constructor begin");
        InitializeComponent(); Ui.Page(this, "الميثاق", nativeHeader: false); _navigator = navigator; _quran = quran; _reading = reading; _locations = locations; _prayers = prayers;
        _azkar = azkar;
        _verseCards.SaveFavoriteAsync = async item =>
        {
            if (item.IsQuran)
            {
                var document = await _quran.LoadAsync();
                var verse = document.Chapters[item.Surah - 1].Verses.First(v => v.Ayah == item.Ayah);
                var note = _reading.GetBookmarks().FirstOrDefault(mark => mark.Surah == verse.Surah && mark.Ayah == verse.Ayah)?.Note ?? "";
                _reading.SaveBookmark(verse, note);
            }
            else if (item.DhikrId > 0) Preferences.Set($"azkar.favorite.{item.DhikrId}", true);
            await PopupSheet.AlertAsync(this, "المفضلة", "حُفظت في المفضلة", "حسنًا");
        };
        BackgroundColor = Ui.Paper;
        Ui.ThemeProperty(this, BackgroundColorProperty, "HomePaper");
        var wordmark = Ui.Label("الميثاق", 23, true);
        wordmark.VerticalTextAlignment = TextAlignment.Center;
        var logo = new Image { WidthRequest = 32, HeightRequest = 40 }; Ui.ThemeProperty(logo, Image.SourceProperty, "HomeBrand");
        var brand = Ui.Row(logo, wordmark);
        brand.FlowDirection = FlowDirection.RightToLeft;
        brand.HorizontalOptions = LayoutOptions.Center; brand.VerticalOptions = LayoutOptions.Center;
        var heading = new Grid
        {
            HeightRequest = 59, Padding = new Thickness(14, 2), FlowDirection = FlowDirection.LeftToRight,
            ColumnDefinitions = { new(48), new(GridLength.Star), new(48) }
        };
        var menu = Ui.Icon("\uf0c9", "فتح القائمة الرئيسية", async () =>
        {
#if IOS
            if (Window?.Page is AppShell shell) await shell.ShowMenuAsync();
#else
            if (Window?.Page is FlyoutPage shell) shell.IsPresented = true;
            await Task.CompletedTask;
#endif
        });
        var themeToggle = Ui.ThemeToggle();
        Ui.ThemeProperty(themeToggle, Button.TextColorProperty, "AppInk");
        heading.Add(themeToggle); heading.Add(brand, 1); heading.Add(menu, 2);
        NavigationPage.SetHasNavigationBar(this, false);
        _date.FontSize = _gregorianDate.FontSize = 11;
        _date.MaxLines = _gregorianDate.MaxLines = 1;
        _date.HorizontalTextAlignment = TextAlignment.End;
        _gregorianDate.HorizontalTextAlignment = TextAlignment.Start;
        var dates = new Grid { FlowDirection = FlowDirection.LeftToRight, ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        dates.Add(_gregorianDate); dates.Add(_date, 1);
        var dateCard = HomeCard(dates); dateCard.Padding = new Thickness(12, 7);
        _prayerName.TextColor = _prayer.TextColor = _remaining.TextColor = _locationName.TextColor = Color.FromArgb("#F8EFDA");
        _prayer.FontSize = 22; _prayerName.FontSize = 20; _remaining.FontSize = 12; _locationName.FontSize = 10;
        _remaining.FlowDirection = _prayer.FlowDirection = FlowDirection.LeftToRight;
        _remaining.HorizontalTextAlignment = _prayer.HorizontalTextAlignment = TextAlignment.End;
        _locationName.MaxLines = 1; _locationName.LineBreakMode = LineBreakMode.TailTruncation; _locationName.VerticalOptions = LayoutOptions.Center;
        var prayerLabel = Ui.Label("الصلاة القادمة", 12, true); prayerLabel.TextColor = Color.FromArgb("#DCC38A");
        var locate = Ui.AsyncButton("تحديد الموقع", SelectLocationAsync, true);
        locate.ImageSource = new FontImageSource { FontFamily = "FontIconSolid", Glyph = "\uf3c5", Size = 14, Color = Color.FromArgb("#DCC38A") };
        locate.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Right, 5);
        locate.FontSize = 10; locate.MinimumHeightRequest = 28; locate.Padding = 0;
        locate.TextColor = Color.FromArgb("#DCC38A"); locate.BackgroundColor = Colors.Transparent; locate.BorderWidth = 0;
        var place = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 6 };
        place.Add(_locationName); place.Add(locate, 1);
        var prayerText = Ui.Stack(prayerLabel, _prayerName, _prayer, _remaining, place);
        prayerText.Spacing = 1; prayerText.Padding = new Thickness(8, 10, 14, 6);
        prayerText.FlowDirection = FlowDirection.RightToLeft;
        var hero = new Grid
        {
            FlowDirection = FlowDirection.LeftToRight, HeightRequest = 147.2,
            ColumnDefinitions = { new(new GridLength(.46, GridUnitType.Star)), new(new GridLength(.54, GridUnitType.Star)) }
        };
        var prayerBackdrop = new HomePhotoArt();
        Grid.SetColumnSpan(prayerBackdrop, 2); hero.Add(prayerBackdrop);
        hero.Add(prayerText, 1);
        var heroCard = HomeCard(hero); heroCard.Padding = 0; hero.IsClippedToBounds = true;
        heroCard.BackgroundColor = Color.FromArgb("#07462F");
        var timesCard = _timesCard = HomeCard(_todayTimes); timesCard.Padding = new Thickness(3, 3); timesCard.HeightRequest = 48; timesCard.IsVisible = _locations.Saved() is not null;
        var openTimes = new TapGestureRecognizer(); openTimes.Tapped += async (_, _) => await _navigator.GoToAsync("prayer");
        timesCard.GestureRecognizers.Add(openTimes);
        var openPrayer = new TapGestureRecognizer(); openPrayer.Tapped += async (_, _) => await _navigator.GoToAsync("prayer");
        heroCard.GestureRecognizers.Add(openPrayer);
        _last.FontSize = 11; _last.MaxLines = 1; _last.LineBreakMode = LineBreakMode.TailTruncation;
        _wirdSummary.FontSize = 11; _wirdSummary.MaxLines = 1;
        var readingCard = ReadingCard("متابعة القراءة", _last, _readingProgress, "book", async () =>
        {
            Preferences.Set("quran.resume.request", true); await _navigator.GoToAsync("quran");
        });
        var wirdCard = ReadingCard("وردي اليوم", _wirdSummary, _wirdProgress, "layers", OpenWirdAsync);
        var wirdEdit = new TapGestureRecognizer(); wirdEdit.Tapped += async (_, _) => await OpenWirdEditorAsync();
        _wirdSummary.GestureRecognizers.Add(wirdEdit);
        var readingCards = new Grid { FlowDirection = FlowDirection.RightToLeft, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
        readingCards.Add(readingCard); readingCards.Add(wirdCard, 1);
        var features = new Grid { ColumnSpacing = 6, RowSpacing = 7 };
        for (var column = 0; column < 4; column++) features.ColumnDefinitions.Add(new(GridLength.Star));
        for (var row = 0; row < 2; row++) features.RowDefinitions.Add(new(GridLength.Auto));
        (string Title, string Route, RectF Crop)[] shortcuts =
        [
            ("القرآن الكريم", "quran", new(3, 0, 1, 1)),
            ("الأذكار والأدعية", "azkar", new(2, 0, 1, 1)),
            ("الختمة", "khatmah", new(1, 0, 1, 1)),
            ("اتجاه القبلة", "qibla", new(0, 0, 1, 1)),
            ("السبحة", "tasbeeh", new(3, 1, 1, 1)),
            ("قيام الليل", "qiyam", new(2, 1, 1, 1)),
            ("الحفظ والمراجعة", "hifz", new(1, 1, 1, 1)),
            ("الزكاة", "zakat", new(0, 1, 1, 1))
        ];
        for (var index = 0; index < shortcuts.Length; index++)
        {
            var item = shortcuts[index];
            var art = new HomeDesignArt(item.Crop) { HeightRequest = 59, WidthRequest = 70, HorizontalOptions = LayoutOptions.Center };
            var title = Ui.Label(item.Title, 11, true); title.HorizontalTextAlignment = TextAlignment.Center;
            title.MaxLines = 2; title.HeightRequest = 36; title.VerticalTextAlignment = TextAlignment.Center;
            if (item.Route == "hifz") title.LineHeight = .85;
            var surface = HomeCard(title); surface.Padding = new Thickness(3, 30, 3, 4); surface.HeightRequest = 71;
            var body = new Grid { HeightRequest = 100, RowDefinitions = { new(29), new(GridLength.Star) }, IsClippedToBounds = false };
            body.Add(surface, 0, 1);
            art.HeightRequest = 59; art.VerticalOptions = LayoutOptions.Start; Grid.SetRowSpan(art, 2); body.Add(art);
            var card = body;
            var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await _navigator.GoToAsync(item.Route);
            card.GestureRecognizers.Add(tap); SemanticProperties.SetDescription(card, item.Title);
            features.Add(card, index % 4, index / 4);
        }
        var contextCard = MosqueBanner(_contextTitle, "qiyam");
        var azkarCard = MosqueBanner(_azkarContextTitle, "azkar");
        var reminders = new ReminderToggle { IsToggled = AlMithaq.Features.Salawat.Services.SalawatSettings.Enabled, VerticalOptions = LayoutOptions.Center };
        reminders.Toggled += async (_, e) =>
        {
            if (e.Value) await AlMithaq.Features.Salawat.Services.SalawatCoordinator.EnableAsync();
            else { AlMithaq.Features.Salawat.Services.SalawatSettings.Enabled = false; AlMithaq.Features.Salawat.Services.SalawatCoordinator.Refresh(); }
            reminders.IsToggled = AlMithaq.Features.Salawat.Services.SalawatSettings.Enabled;
        };
        var remindText = Ui.Label("تذكير الصلاة على النبي", 12, true); remindText.VerticalOptions = LayoutOptions.Center;
        var blessing = Ui.Label("ﷺ", 22); blessing.FontFamily = "AmiriQuran"; blessing.VerticalTextAlignment = TextAlignment.Center;
        var reminderCaption = Ui.Row(remindText, blessing); reminderCaption.Spacing = 5; reminderCaption.VerticalOptions = LayoutOptions.Center;
        var bell = Ui.Label("\uf0f3", 18); bell.FontFamily = "FontIconSolid"; bell.TextColor = Ui.Gold; bell.VerticalTextAlignment = TextAlignment.Center;
        var reminderRow = new Grid { FlowDirection = FlowDirection.RightToLeft, ColumnSpacing = 7, ColumnDefinitions = { new(23), new(GridLength.Star), new(GridLength.Auto) } };
        reminderRow.Add(bell); reminderRow.Add(reminderCaption, 1); reminderRow.Add(reminders, 2);
        var reminderCard = HomeCard(reminderRow); reminderCard.Padding = new Thickness(12, 3);
        var dayBody = Ui.Stack(SummaryHeading("ملخص اليوم", "bars"), _daySummary); dayBody.Spacing = 3;
        var dayGrid = new Grid { ColumnDefinitions = { new(GridLength.Star), new(58) }, ColumnSpacing = 10 };
        dayGrid.Add(dayBody); dayGrid.Add(_dayProgress, 1);
        var dayCard = HomeCard(dayGrid);
        var khatmahArrow = Ui.Icon("\uf053", "متابعة الختمة", () => _navigator.GoToAsync("khatmah"));
        khatmahArrow.WidthRequest = khatmahArrow.HeightRequest = khatmahArrow.MinimumWidthRequest = khatmahArrow.MinimumHeightRequest = 26;
        khatmahArrow.FontSize = 12; khatmahArrow.CornerRadius = 7;
        Ui.ThemeProperty(khatmahArrow, Button.BackgroundColorProperty, "HomeSelected");
        var khatmahHead = SummaryHeading("ختمتي", "layers", khatmahArrow);
        var khatmahBody = Ui.Stack(khatmahHead, _khatmahSummary, _khatmahProgress); khatmahBody.Spacing = 6;
        var stack = Ui.Stack(dateCard, heroCard, timesCard, readingCards, features, azkarCard, _verseCards, contextCard, reminderCard, dayCard, KhatmahCard(khatmahBody));
        stack.Padding = new Thickness(12, 4, 12, 12); stack.Spacing = 8;
        var pageLayout = new Grid { ClassId = "choice-host", RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        pageLayout.Add(heading); pageLayout.Add(new ScrollView { Content = stack }, 0, 1); Content = pageLayout;
        StartupTrace.Write("Home constructor complete");
    }

    private static Border HomeCard(View body, bool ornamented = false)
    {
        var ornament = new HomeCardOrnament();
        var decorated = new Grid(); if (ornamented) decorated.Add(ornament); decorated.Add(body);
        var card = Ui.Card(decorated); card.Margin = 0; card.Padding = 12;
        card.Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#06160F")), Opacity = .15f, Offset = new Point(0, 3), Radius = 7 };
        Ui.ThemeProperty(card, Border.BackgroundColorProperty, "HomeCard");
        Ui.ThemeProperty(card, Border.StrokeProperty, "HomeStrokeBrush");
        Ui.ThemeProperty(card, Border.StrokeThicknessProperty, "HomeStrokeWidth");
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 };
        return card;
    }

    private static Grid SummaryHeading(string title, string icon, View? action = null)
    {
        var grid = new Grid { FlowDirection = FlowDirection.RightToLeft, ColumnSpacing = 6, ColumnDefinitions = { new(22), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(new HomeNavigationIcon(icon) { WidthRequest = 21, HeightRequest = 21, Tint = Ui.Gold, VerticalOptions = LayoutOptions.Center });
        var label = Ui.Label(title, 15, true); label.VerticalOptions = LayoutOptions.Center; grid.Add(label, 1);
        if (action is not null) grid.Add(action, 2); return grid;
    }
    private Border MosqueBanner(Label title, string route)
    {
        var follow = Ui.Icon("\uf053", "متابعة " + (route == "azkar" ? "الأذكار" : "قيام الليل"), async () =>
        {
            if (route == "azkar") Preferences.Set("azkar.home.chapter", DateTime.Now.Hour < 12 ? 27 : 28);
            await _navigator.GoToAsync(route);
        });
        follow.BackgroundColor = Colors.Transparent;
        Ui.ThemeProperty(follow, Button.TextColorProperty, "HomeArrow");
        if (follow.ImageSource is FontImageSource arrow) arrow.Color = follow.TextColor;
        follow.FontSize = 12; follow.Padding = 0; follow.CornerRadius = 7;
        follow.WidthRequest = follow.HeightRequest = follow.MinimumWidthRequest = follow.MinimumHeightRequest = 26;
        Ui.ThemeProperty(follow, Button.BackgroundColorProperty, "HomeSelected"); follow.VerticalOptions = LayoutOptions.Center;
        Ui.ThemeProperty(title, Label.TextColorProperty, "AppInk"); title.VerticalOptions = LayoutOptions.Center;
        var row = new Grid { Padding = new Thickness(10, 0), FlowDirection = FlowDirection.RightToLeft, ColumnSpacing = 6,
            ColumnDefinitions = { new(32), new(GridLength.Star), new(GridLength.Auto) } };
        row.Add(new HomeDesignArt(new RectF(2, 0, 1, 1)) { WidthRequest = 30, HeightRequest = 40, VerticalOptions = LayoutOptions.Center });
        var action = new Grid { FlowDirection = FlowDirection.LeftToRight, WidthRequest = 125 }; follow.HorizontalOptions = LayoutOptions.Start; action.Add(follow);
        row.Add(title, 1); row.Add(action, 2);
        var panel = new Grid { HeightRequest = 56, IsClippedToBounds = true };
        panel.Add(new HomeBannerArt(route == "qiyam") { HeightRequest = 56 }); panel.Add(row);
        var card = HomeCard(panel); Ui.ThemeProperty(card, Border.BackgroundColorProperty, "HomeBanner"); card.Padding = 0; card.HeightRequest = 56;
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => { if (route == "azkar") Preferences.Set("azkar.home.chapter", DateTime.Now.Hour < 12 ? 27 : 28); await _navigator.GoToAsync(route); }; card.GestureRecognizers.Add(tap); return card;
    }

    private static void SetForwardArrow(Button button)
    {
        var image = new FontImageSource { FontFamily = "FontIconSolid", Glyph = "\uf053", Size = 12 };
        Ui.ThemeProperty(image, FontImageSource.ColorProperty, "AppInk"); button.ImageSource = image;
        button.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 6);
        button.FlowDirection = FlowDirection.LeftToRight;
    }
    private static Border ReadingCard(string title, Label detail, ProgressBar progress, string icon, Func<Task> action)
    {
        var heading = Ui.Label(title, 12, true); heading.VerticalTextAlignment = TextAlignment.Center; heading.MaxLines = 1;
        var symbol = new HomeNavigationIcon(icon) { WidthRequest = 23, HeightRequest = 23, Tint = Ui.Gold };
        var head = new Grid { FlowDirection = FlowDirection.RightToLeft, ColumnSpacing = 5, ColumnDefinitions = { new(23), new(GridLength.Star) } };
        head.Add(symbol); head.Add(heading, 1);
        var arrow = Ui.Icon("\uf053", title, action);
        arrow.WidthRequest = arrow.HeightRequest = arrow.MinimumWidthRequest = arrow.MinimumHeightRequest = 26;
        arrow.FontSize = 12; arrow.CornerRadius = 7;
        Ui.ThemeProperty(arrow, Button.BackgroundColorProperty, "HomeSelected");
        var lower = new Grid { FlowDirection = FlowDirection.LeftToRight, ColumnDefinitions = { new(26), new(GridLength.Star) }, ColumnSpacing = 10 };
        progress.HeightRequest = 3; progress.VerticalOptions = LayoutOptions.End;
        lower.Add(arrow); lower.Add(progress, 1);
        var body = Ui.Stack(head, detail, lower); body.Spacing = 2;
        var card = HomeCard(body); card.Padding = new Thickness(9, 7); card.HeightRequest = 87;
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await action(); card.GestureRecognizers.Add(tap);
        return card;
    }

    private Border KhatmahCard(View body)
    {
        var card = HomeCard(body); var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await _navigator.GoToAsync("khatmah"); card.GestureRecognizers.Add(tap); return card;
    }
    public async Task OpenWirdEditorAsync()
    {
        if (_wirdEditorOpen) return; _wirdEditorOpen = true;
        try {
        Preferences.Remove("home.wird.open");
        await PopupSheet.PresentAsync<string>(this, "وردي اليوم", done =>
            new AlMithaq.Features.Quran.Views.DailyWirdView(this, async () =>
            {
                done("open"); await _navigator.GoToAsync("quran");
            }, async () => { done("plan"); await _navigator.GoToAsync("khatmah"); }));
        RefreshProgress();
        } finally { _wirdEditorOpen = false; }
    }
    private async Task OpenWirdAsync()
    {
        if (AlMithaq.Features.Quran.Services.DailyWirdStore.Load() is not { } plan)
        { await OpenWirdEditorAsync(); return; }
        Preferences.Set("quran.wird-page.request", plan.StartPage);
        await _navigator.GoToAsync("quran");
    }
    private void RefreshProgress()
    {
        var plan = AlMithaq.Features.Quran.Services.DailyWirdStore.Load();
        var complete = plan?.IsComplete(DateOnly.FromDateTime(DateTime.Today)) == true;
        _wirdSummary.Text = plan is null ? "حدد وردك اليومي" : complete ? "أتممت ورد اليوم ✓" : $"الصفحات {plan.StartPage}–{plan.EndPage}";
        _wirdProgress.Progress = complete ? 1 : 0; _dayProgress.Progress = _wirdProgress.Progress;
        var khatmah = AlMithaq.Features.Quran.Services.KhatmahStore.Load();
        _khatmahSummary.Text = khatmah is null ? "ابدأ خطة ختمة تناسب وقتك" : khatmah.IsFinished ? "أتممت الختمة ✓" : $"قرأت {khatmah.ReadPages} صفحة · المتبقي {khatmah.RemainingPages}";
        _khatmahProgress.Progress = khatmah?.Progress ?? 0;
        _daySummary.Text = $"{(complete ? "أتممت وردك اليوم" : "خطوة أخرى نحو إتمام وردك")}\nالأذكار {_azkarStat.Text} · تسبيحات اليوم {_tasbeehStat.Text}";
        _contextTitle.Text = "قيام الليل";
        _azkarContextTitle.Text = DateTime.Now.Hour < 12 ? "أذكار الصباح" : "أذكار المساء";
    }
    private async Task SelectLocationAsync()
    {
        try
        {
            var result = await _locations.AcquireAsync(CancellationToken.None);
            _locationName.Text = result.Description; _timesDate = default; _next = null;
            await UpdateAsync();
        }
        catch (Exception ex)
        {
            await PopupSheet.AlertAsync(this, "تحديد الموقع", ex.Message, "حسنًا");
        }
    }

    protected override async void OnAppearing()
    {
        StartupTrace.Write("Home appearing");
        base.OnAppearing(); _active = true; _next = null; _timesDate = default;
        RefreshProgress();
        _tasbeehStat.Text = Preferences.Get($"tasbeeh.day.{DateTime.Today:yyyyMMdd}", 0).ToString(); _marksStat.Text = _reading.GetBookmarks().Count.ToString();
        _adhanStatus.Text = AdhanSettings.Enabled ? "الأذان · " + AdhanCoordinator.Status : "الأذان غير مفعّل · فعّله من الإعدادات";
        Ui.ThemeProperty(_adhanStatus, Label.TextColorProperty, "AppMuted");
        await Task.Yield();
        if (!_active) return;
        StartupTrace.Write("Home dispatcher ready");
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += Tick; _timer.Start();
        StartupTrace.Write("Home timer started");
        await _locations.RefreshNameAsync(CancellationToken.None);
        await UpdateAsync();
        StartupTrace.Write("Home clock updated");
        await RefreshDailyContentAsync();
        if (Preferences.Get("home.wird.open", false)) await OpenWirdEditorAsync();
        else if (!AlMithaq.Features.Quran.Services.KhatmahStore.Linked && AlMithaq.Features.Quran.Services.DailyWirdStore.Load() is { } pending && pending.IsOverdue(DateOnly.FromDateTime(DateTime.Today)))
        {
            var key = $"wird.overdue.shown.{DateTime.Today:yyyyMMdd}";
            if (!Preferences.Get(key,false))
            {
                Preferences.Set(key,true);
                if (await PopupSheet.AlertAsync(this,"وردك ينتظرك",$"لم يكتمل وردك السابق من صفحة {pending.StartPage} إلى {pending.EndPage}. أتممه أولًا، وسنجهّز ورد اليوم مباشرة.","فتح وردي","لاحقًا")) await OpenWirdEditorAsync();
            }
        }
    }
    private async Task RefreshDailyContentAsync()
    {
        _cardsDate = DateTime.Today; RefreshProgress();
        try
        {
            var document = await _quran.LoadAsync(); if (!_active) return;
            StartupTrace.Write("Quran corpus loaded");
            var last = _reading.GetPosition();
            if (last is not null && last.Surah is >= 1 and <= 114)
            {
                var versePosition = document.Chapters[last.Surah - 1].Verses.FirstOrDefault(item => item.Ayah == last.Ayah);
                _readingProgress.Progress = (versePosition?.Page ?? 0) / 604d;
            }

            _last.Text = last is not null && last.Surah is >= 1 and <= 114 ? $"{document.Chapters[last.Surah - 1].Name} — آية {last.Ayah}" : "ابدأ القراءة، وسنحفظ موضعك تلقائيًا";
            var all = document.Chapters.SelectMany(s => s.Verses).Where(v => v.Text.Length is > 60 and < 250).ToArray();
            var index = (DateTime.Today - new DateTime(2020, 1, 1)).Days % all.Length; var verse = all[Math.Abs(index)];
            _dailyVerses = Enumerable.Range(0, 3).Select(i => all[(Math.Abs(index) * 3 + i) % all.Length]).Select(v => new DailyCard(v.Text, $"{document.Chapters[v.Surah - 1].Name} · آية {v.Ayah}", true, v.Surah, v.Ayah)).ToArray();
            _verseCards.SetItems(_dailyVerses.Concat(_dailyAdhkar));
            _verse.Text = verse.Text; _reference.Text = $"{document.Chapters[verse.Surah - 1].Name} — آية {verse.Ayah} · Tanzil.net";
        }
        catch (Exception ex) { _last.Text = "تعذر قراءة بيانات القرآن؛ افتح القارئ لإعادة المحاولة"; System.Diagnostics.Debug.WriteLine(ex); }
        try
        {
            var data = await _azkar.LoadAsync(); if (!_active) return;
            var category = DateTime.Now.Hour < 12 ? 0 : 1; var type = category == 0 ? 1 : 2;
            var daily = data.Where(d => d.Chapter == (category == 0 ? 27 : 28)).ToArray();
            _azkarStat.Text = $"{daily.Count(d => Preferences.Get($"azkar.progress.{DateTime.Today:yyyyMMdd}.hisn.{d.Id}", 0) >= d.Repeat)} / {daily.Length}";
            var choices = data.Where(d => d.Text.Length is >= 24 and <= 240 && (d.Source.Contains("البخاري") || d.Source.Contains("مسلم"))).ToArray();
            if (choices.Length == 0) choices = data.Where(d => !string.IsNullOrWhiteSpace(d.Text)).ToArray();
            if (choices.Length == 0) throw new InvalidDataException("لا توجد أذكار");
            var day = Math.Abs((DateTime.Today - new DateTime(2020, 1, 1)).Days);
            _dailyAdhkar = Enumerable.Range(0, 3).Select(i => choices[(day * 3 + i) % choices.Length]).Select(d => new DailyCard(d.Text, d.Source, DhikrId: d.Id)).ToArray();
            _verseCards.SetItems(_dailyVerses.Concat(_dailyAdhkar));
            var dhikr = choices[day % choices.Length]; _dua.Text = dhikr.Text;
            RefreshProgress();
            var group = dhikr.Type switch { 1 => "أذكار الصباح", 2 => "أذكار المساء", 3 => "ذكر النوم", 4 => "بعد الصلاة", _ => "ذكر ودعاء" };
            _duaReference.Text = $"{dhikr.Source}\n{group}" + (dhikr.Repeat > 1 ? $" · التكرار: {dhikr.Repeat}" : "");
        }
        catch (Exception ex) { _dua.Text = "افتح قسم الأذكار والأدعية لقراءة وردك."; System.Diagnostics.Debug.WriteLine(ex); }
    }
    protected override void OnDisappearing()
    {
        _active = false; if (_timer is not null) { _timer.Stop(); _timer.Tick -= Tick; _timer = null; } base.OnDisappearing();
    }
    private async void Tick(object? sender, EventArgs e)
    {
        await UpdateAsync();
        if (_active && _cardsDate != DateTime.Today) await RefreshDailyContentAsync();
    }
    private async Task UpdateAsync()
    {
        if (_updating || !_active) return; _updating = true;
        try
        {
            var state = await AlMithaq.Core.Logic.BackgroundWork.Run(() =>
            {
                var current = DateTime.Now;
                var saved = _locations.Saved();
                return new ClockSnapshot(saved, current);
            });
            if (!_active) return;
            _date.Text = AlMithaq.Core.Logic.HijriDateText.Format(state.Now);
            _gregorianDate.Text = state.Now.ToString("dddd، d MMM yyyy", CultureInfo.GetCultureInfo("ar-EG"));
            var now = state.Now;
            _azkarContextTitle.Text = now.Hour < 12 ? "أذكار الصباح" : "أذكار المساء";
            var location = state.Location;
            if (_timesCard is not null) _timesCard.IsVisible = location is not null;
            _locationName.Text = location?.Description ?? "حدد موقعك";
            if (location is null) { _prayerName.Text = "موقعك أولًا"; _prayer.Text = "—"; _remaining.Text = "حدد موقعك لحساب المواقيت"; return; }
            var locationKey = FormattableString.Invariant($"{location.Location.Latitude:F5},{location.Location.Longitude:F5}");
            if (_timesDate != now.Date || _timesLocation != locationKey)
            {
                _next = await _prayers.GetNextPrayer(location.Location); _todayTimes.Clear(); _prayerCells.Clear(); _highlightedPrayer = null; var column = 0;
                foreach (var time in _prayers.GetDay(location.Location, now.Date).Where(p => p.IsPrayer))
                {
                    var name = Ui.Label(time.Name, 10); name.HorizontalTextAlignment = TextAlignment.Center;
                    var value = Ui.Label(time.Time.ToString("h:mm", CultureInfo.InvariantCulture) + (time.Time.Hour < 12 ? " ص" : " م"), 11, true); value.HorizontalTextAlignment = TextAlignment.Center; value.FlowDirection = FlowDirection.LeftToRight;
                    var period = Ui.Label(time.Time.Hour < 12 ? "صباحًا" : "مساءً", 8); period.HorizontalTextAlignment = TextAlignment.Center; Ui.ThemeProperty(period, Label.TextColorProperty, "AppMuted");
                    var body = Ui.Stack(name, value); body.Spacing = 1; body.Padding = new Thickness(1, 1);
                    var cell = new Border { Content = body, Padding = 0, StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 11 } };
                    _prayerCells.Add((cell, name, value, time.Name));
                    if (column < 4)
                    {
                        var divider = new BoxView { WidthRequest = .5, HeightRequest = 25, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
                        Ui.ThemeProperty(divider, BoxView.ColorProperty, "HomeDivider");
                        var content = cell.Content; cell.Content = null; var split = new Grid(); split.Add(content); split.Add(divider); cell.Content = split;
                    }
                    _todayTimes.Add(cell, column++);
                }
                _timesDate = now.Date; _timesLocation = locationKey;
            }
            if (_next is null || _next.Time <= now) _next = await _prayers.GetNextPrayer(location.Location);
            if (_highlightedPrayer != _next.Name)
            {
                foreach (var item in _prayerCells)
                {
                    var selected = item.Prayer == _next.Name; item.Cell.BackgroundColor = selected ? Ui.Green : Colors.Transparent;
                    if (selected) item.Name.TextColor = item.Time.TextColor = Colors.White;
                    else { Ui.ThemeProperty(item.Name, Label.TextColorProperty, "AppInk"); Ui.ThemeProperty(item.Time, Label.TextColorProperty, "AppInk"); }
                }
                _highlightedPrayer = _next.Name;
            }
            var diff = _next.Time - now; _prayerName.Text = _next.Name; _prayer.Text = _next.Time.ToString("h:mm tt", new CultureInfo("ar-EG"));
            _remaining.Text = $"متبقي {Math.Max(0, (int)diff.TotalHours):D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
        }
        catch (Exception ex) { _prayer.Text = "راجع إعدادات الموقع وطريقة الحساب"; System.Diagnostics.Debug.WriteLine(ex); }
        finally { _updating = false; }
    }
}

