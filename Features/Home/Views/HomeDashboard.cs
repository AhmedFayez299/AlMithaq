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
    private readonly DailyCardsView _verseCards = new("آياتٌ تنير يومك", true);
    private readonly DailyCardsView _dhikrCards = new("ذكر ودعاء اليوم", false);
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
    private readonly AlMithaq.Features.Quran.Views.DailyWirdView _wird;
    private DateTime _timesDate, _cardsDate;
    private string _timesLocation = "";
    private IDispatcherTimer? _timer;
    private bool _active, _updating;
    private PrayerInfo? _next;
    private sealed record ClockSnapshot(DeviceLocation? Location, DateTime Now);
    protected override bool OnBackButtonPressed() => AlMithaq.Features.Quran.Views.WirdCelebration.Dismiss(this) || PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    public HomePage(IQuranRepository quran, IReadingStore reading, LocationService locations, IPrayerService prayers, IAppNavigator navigator, AzkarRepository azkar)
    {
        StartupTrace.Write("Home constructor begin");
        InitializeComponent(); Ui.Page(this, "الميثاق", nativeHeader: false); _navigator = navigator; _quran = quran; _reading = reading; _locations = locations; _prayers = prayers;
        _azkar = azkar;
        var wordmark = Ui.Label("الميثاق", 20, true); wordmark.TextColor = Colors.White; wordmark.VerticalTextAlignment = TextAlignment.Center; wordmark.HorizontalTextAlignment = TextAlignment.Center; wordmark.FontAutoScalingEnabled = false; wordmark.HeightRequest = 54;
        var heading = new Grid { HeightRequest = 64, BackgroundColor = Ui.Green, Padding = new Thickness(10, 0), FlowDirection = FlowDirection.LeftToRight, ColumnDefinitions = { new(48), new(GridLength.Star), new(48) } };
        var brand = Ui.Row(new Image { Source = "mithaqbrand.png", WidthRequest = 34, HeightRequest = 48 }, wordmark); brand.FlowDirection = FlowDirection.RightToLeft; brand.HorizontalOptions = LayoutOptions.Center; brand.VerticalOptions = LayoutOptions.Center;
        var headerBackdrop = new HeaderBackdrop(); Grid.SetColumnSpan(headerBackdrop, 3); heading.Add(headerBackdrop);
        heading.Add(brand, 1);
        heading.Add(Ui.ThemeToggle(), 0);
        var menuButton = Ui.Icon("\uf0c9", "فتح القائمة الرئيسية", () => {
#if IOS
            return Window?.Page is AppShell shell ? shell.ShowMenuAsync() : Task.CompletedTask;
#else
            if (Window?.Page is FlyoutPage flyout) flyout.IsPresented = true; return Task.CompletedTask;
#endif
        }); menuButton.TextColor = Colors.White; menuButton.VerticalOptions = LayoutOptions.Center; heading.Add(menuButton, 2);
        NavigationPage.SetHasNavigationBar(this, false);
        var greeting = Ui.Label("السّلام عليكم", 21, true); greeting.TextColor = Colors.White;
        var subtitle = Ui.Label("لحظاتٌ تقرّبك… وتملأ يومك طمأنينة", 11); subtitle.TextColor = Color.FromArgb("#E8D8AC");
        var heroLayout = new Grid { HeightRequest = 132, ColumnDefinitions = { new(new GridLength(.33, GridUnitType.Star)), new(new GridLength(.67, GridUnitType.Star)) }, FlowDirection = FlowDirection.LeftToRight };
        var heroArt = new Image { Source = "home_welcome.png", Aspect = Aspect.AspectFill }; heroLayout.Add(heroArt); Grid.SetColumnSpan(heroArt, 2);
        var bismillah = Ui.Label("بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ", 10); bismillah.TextColor = Color.FromArgb("#E8D8AC");
        var ornament = new BoxView { HeightRequest = 1, WidthRequest = 40, Color = Color.FromArgb("#BBA56D"), HorizontalOptions = LayoutOptions.Start, Margin = new Thickness(0, 3) };
        var welcome = Ui.Stack(bismillah, ornament, greeting, subtitle); welcome.Spacing = 3; welcome.Padding = new Thickness(16, 12); welcome.FlowDirection = FlowDirection.RightToLeft; welcome.VerticalOptions = LayoutOptions.Center; heroLayout.Add(welcome, 1);
        var hero = new Border { Content = heroLayout, Padding = 0, StrokeThickness = 1, Stroke = Color.FromArgb("#668F79"), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 }, BackgroundColor = Ui.Green, Margin = new Thickness(0, 2, 0, 5) };
        Ui.ThemeProperty(_verse, Label.TextColorProperty, "AppInk");
        _date.FontSize = 12; Ui.ThemeProperty(_date, Label.TextColorProperty, "AppMuted");
        Ui.ThemeProperty(_gregorianDate, Label.TextColorProperty, "AppMuted");
        var dates = new Grid { FlowDirection = FlowDirection.LeftToRight, ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
        _date.FlowDirection = _gregorianDate.FlowDirection = FlowDirection.RightToLeft;
        _date.HorizontalOptions = LayoutOptions.End;
        _gregorianDate.HorizontalOptions = LayoutOptions.Start;
        _date.MaxLines = _gregorianDate.MaxLines = 1;
        _gregorianDate.FontSize = 11;
        dates.Add(_gregorianDate); dates.Add(_date, 1);
        _remaining.TextColor = Ui.Gold; _remaining.FontSize = 13; _prayer.FontSize = 17; _last.FontSize = 13; _verse.FontSize = 22; _dua.FontSize = 16; _reference.FontSize = 12;
        _dua.LineHeight = 1.25; Ui.ThemeProperty(_duaReference, Label.TextColorProperty, "AppMuted");
        var shortcuts = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) }, ColumnSpacing = 10, RowSpacing = 2 };
        View Feature(string name, string caption, string image, string route)
        {
            var art = new Image { Source = image, WidthRequest = 42, HeightRequest = 46, VerticalOptions = LayoutOptions.Center };
            var label = Ui.Label(name, 13, true);
            var detail = Ui.Label(caption, 10); Ui.ThemeProperty(detail, Label.TextColorProperty, "AppMuted");
            var words = Ui.Stack(label, detail); words.Spacing = 1; words.VerticalOptions = LayoutOptions.Center;
            var content = new Grid { ColumnDefinitions = { new(42), new(GridLength.Star) }, ColumnSpacing = 7 }; content.Add(art); content.Add(words, 1);
            var card = Ui.Card(content); card.Padding = 10;
            var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await _navigator.GoToAsync(route); card.GestureRecognizers.Add(tap);
            SemanticProperties.SetDescription(card, name); return card;
        }
        shortcuts.Add(Feature("القرآن الكريم", "قراءةٌ وتدبّر", "scene_quran.png", "quran"), 0, 0);
        shortcuts.Add(Feature("الأذكار والأدعية", "ذكرٌ يطمئنّ به قلبك", "scene_azkar.png", "azkar"), 1, 0);
        shortcuts.Add(Feature("مواقيت الصلاة", "على موعدٍ مع الصلاة", "scene_prayer.png", "prayer"), 0, 1);
        shortcuts.Add(Feature("اتجاه القبلة", "وجهتك أينما كنت", "scene_qibla.png", "qibla"), 1, 1);
        shortcuts.Add(Feature("السبحة", "حسناتٌ بكل تسبيحة", "scene_tasbeeh.png", "tasbeeh"), 0, 2);
        shortcuts.Add(Feature("حاسبة الزكاة", "مالٌ يزكو بالخير", "scene_zakat.png", "zakat"), 1, 2);
        shortcuts.Add(Feature("الإعدادات", "الميثاق كما تحب", "scene_settings.png", "settings"), 1, 4);
        shortcuts.Add(Feature("قيام الليل", "لحظات هادئة تقرّبك", "scene_prayer.png", "qiyam"), 0, 4);
        shortcuts.Add(Feature("خطة الختمة", "خطوات يومية مرنة", "scene_quran.png", "khatmah"), 0, 3);
        shortcuts.Add(Feature("الحفظ والمراجعة", "ثبّت ما تحفظ", "scene_quran.png", "hifz"), 1, 3);
        var resume = Ui.AsyncButton("متابعة القراءة", async () => { Preferences.Set("quran.resume.request", true); await _navigator.GoToAsync("quran"); }); resume.FontSize = 12;
        resume.ImageSource = new FontImageSource { FontFamily = "FontIconSolid", Glyph = "\uf053", Size = 14, Color = Colors.White };
        resume.FlowDirection = FlowDirection.LeftToRight;
        resume.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8);
        var readingCard = Ui.Card(Ui.Stack(Ui.Label("رحلتك مع القرآن", 14, true), _last, resume));
        _wird = new(this, () => _navigator.GoToAsync("quran"), () => _navigator.GoToAsync("khatmah")); var wirdCard = Ui.Card(_wird);
        var prayerContent = Ui.Stack(Ui.Label("الصلاة القادمة", 12, true), _prayer, _remaining, _adhanStatus); prayerContent.Spacing = 3;
        var prayerLayout = new Grid { FlowDirection = FlowDirection.LeftToRight, ColumnDefinitions = { new(78), new(GridLength.Star) }, ColumnSpacing = 10 };
        prayerContent.FlowDirection = FlowDirection.RightToLeft;
        prayerLayout.Add(new Image { Source = "scene_prayer.png", WidthRequest = 76, HeightRequest = 94, VerticalOptions = LayoutOptions.Center, Aspect = Aspect.AspectFit }); prayerLayout.Add(prayerContent, 1);
        var prayerCard = Ui.Card(Ui.Stack(prayerLayout, _todayTimes, Ui.AsyncButton("المواقيت وإعدادات الأذان", () => _navigator.GoToAsync("prayer"), true)));
        _azkarStat.FlowDirection = FlowDirection.LeftToRight;
        var overview = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 7 };
        View Stat(string caption, Label value, string route)
        {
            value.HorizontalTextAlignment = TextAlignment.Center; var label = Ui.Label(caption, 10); label.HorizontalTextAlignment = TextAlignment.Center;
            Ui.ThemeProperty(label, Label.TextColorProperty, "AppMuted"); var body = Ui.Stack(value, label); body.Spacing = 1;
            var card = Ui.Card(body); card.Padding = 9; var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await _navigator.GoToAsync(route); card.GestureRecognizers.Add(tap); return card;
        }
        overview.Add(Stat("أذكار مكتملة", _azkarStat, "azkar")); overview.Add(Stat("تسبيحات اليوم", _tasbeehStat, "tasbeeh"), 1); overview.Add(Stat("علامات محفوظة", _marksStat, "quran"), 2);
        var stack = Ui.Stack(hero, dates, prayerCard, wirdCard, readingCard, Ui.Label("نشاطك في الميثاق", 14, true), overview, Ui.Label("أبواب الخير", 15, true), shortcuts, _verseCards, _dhikrCards,
            Ui.Label("الميثاق · معك في كل خطوة", 12));
        stack.Padding = new Thickness(16, 12, 16, 24); stack.Spacing = 8;
        var pageLayout = new Grid { ClassId = "choice-host", RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } }; pageLayout.Add(heading); pageLayout.Add(new ScrollView { Content = stack }, 0, 1); Content = pageLayout;
        StartupTrace.Write("Home constructor complete");
    }
    protected override async void OnAppearing()
    {
        StartupTrace.Write("Home appearing");
        base.OnAppearing(); _active = true; _next = null; _timesDate = default;
        _wird.Refresh();
        _tasbeehStat.Text = Preferences.Get($"tasbeeh.day.{DateTime.Today:yyyyMMdd}", 0).ToString(); _marksStat.Text = _reading.GetBookmarks().Count.ToString();
        _adhanStatus.Text = AdhanSettings.Enabled ? "الأذان · " + AdhanCoordinator.Status : "الأذان غير مفعّل · فعّله من الإعدادات";
        Ui.ThemeProperty(_adhanStatus, Label.TextColorProperty, "AppMuted");
        await Task.Yield();
        if (!_active) return;
        StartupTrace.Write("Home dispatcher ready");
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += Tick; _timer.Start();
        StartupTrace.Write("Home timer started");
        await UpdateAsync();
        StartupTrace.Write("Home clock updated");
        await RefreshDailyContentAsync();
    }
    private async Task RefreshDailyContentAsync()
    {
        _cardsDate = DateTime.Today;
        try
        {
            var document = await _quran.LoadAsync(); if (!_active) return;
            StartupTrace.Write("Quran corpus loaded");
            var last = _reading.GetPosition();
            _last.Text = last is not null && last.Surah is >= 1 and <= 114 ? $"{document.Chapters[last.Surah - 1].Name} — آية {last.Ayah}" : "ابدأ القراءة، وسنحفظ موضعك تلقائيًا";
            var all = document.Chapters.SelectMany(s => s.Verses).Where(v => v.Text.Length is > 60 and < 250).ToArray();
            var index = (DateTime.Today - new DateTime(2020, 1, 1)).Days % all.Length; var verse = all[Math.Abs(index)];
            _verseCards.SetItems(Enumerable.Range(0, 3).Select(i => all[(Math.Abs(index) * 3 + i) % all.Length]).Select(v => new DailyCard(v.Text, $"{document.Chapters[v.Surah - 1].Name} · آية {v.Ayah}")));
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
            _dhikrCards.SetItems(Enumerable.Range(0, 3).Select(i => choices[(day * 3 + i) % choices.Length]).Select(d => new DailyCard(d.Text, d.Source)));
            var dhikr = choices[day % choices.Length]; _dua.Text = dhikr.Text;
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
            var location = state.Location; if (location is null) { _prayer.Text = "حدد موقعك لعرض الصلاة القادمة"; return; }
            if (_timesDate != now.Date || _timesLocation != location.Description)
            {
                _todayTimes.Clear(); var column = 0;
                foreach (var time in _prayers.GetDay(location.Location, now.Date).Where(p => p.IsPrayer))
                {
                    var name = Ui.Label(time.Name, 10); name.HorizontalTextAlignment = TextAlignment.Center;
                    var value = Ui.Label(time.Time.ToString("hh:mm", CultureInfo.InvariantCulture), 11, true); value.HorizontalTextAlignment = TextAlignment.Center; value.FlowDirection = FlowDirection.LeftToRight;
                    var period = Ui.Label(time.Time.Hour < 12 ? "صباحًا" : "مساءً", 8); period.HorizontalTextAlignment = TextAlignment.Center; Ui.ThemeProperty(period, Label.TextColorProperty, "AppMuted");
                    var body = Ui.Stack(name, value, period); body.Spacing = 1; var cell = Ui.Card(body); cell.Padding = new Thickness(2, 7); _todayTimes.Add(cell, column++);
                }
                _timesDate = now.Date; _timesLocation = location.Description;
            }
            if (_next is null || _next.Time <= now) _next = await _prayers.GetNextPrayer(location.Location);
            var diff = _next.Time - now; _prayer.Text = $"{_next.Name} — {_next.Time.ToString("hh:mm tt", new CultureInfo("ar-EG"))}";
            _remaining.Text = $"متبقي {Math.Max(0, (int)diff.TotalHours):D2}:{diff.Minutes:D2}:{diff.Seconds:D2}";
        }
        catch (Exception ex) { _prayer.Text = "راجع إعدادات الموقع وطريقة الحساب"; System.Diagnostics.Debug.WriteLine(ex); }
        finally { _updating = false; }
    }
}

