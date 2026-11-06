using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Logic;
using AlMithaq.Features.Azkar.Services;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Features.Qiyam.Services;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;
using System.Globalization;
namespace AlMithaq.Pages;

public sealed class QiyamPage : ContentPage
{
    private readonly LocationService _locations;
    private readonly IPrayerService _prayers;
    private readonly IAppNavigator _navigator;
    private readonly AzkarRepository _azkar;
    private readonly DatePicker _date = new() { MinimumDate = DateTime.Today.AddDays(-30), MaximumDate = DateTime.Today, Date = DateTime.Today, Format = "dd / MM / yyyy", FontFamily = "Almarai-Regular", FontSize = 14 };
    private readonly Label _countdown = Ui.Label("حدد موقعك لعرض وقت الليلة", 16, true), _status = Ui.Label("", 13), _count = Ui.Label("0", 28, true), _goalText = Ui.Label("", 13), _reminderStatus = Ui.Label("", 12);
    private readonly Label[] _times = [Ui.Label("—", 15, true), Ui.Label("—", 15, true), Ui.Label("—", 15, true)];
    private readonly Entry _goal = Ui.Input("عدد زوجي من 2 إلى 100", true);
    private readonly ProgressBar _progress = new() { ProgressColor = Ui.Gold, HeightRequest = 5 };
    private readonly Switch _reminder = new(), _witr = new();
    private readonly ChoicePicker _lead = new() { Title = "موعد التذكير", ItemsSource = new[] { "بداية الثلث الأخير", "قبله بـ15 دقيقة", "قبله بـ30 دقيقة", "قبله بساعة" } };
    private readonly VerticalStackLayout _duas = new() { Spacing = 7 };
    private readonly Button _add, _undo, _done;
    private QiyamWindow? _window;
    private CancellationTokenSource? _cancel;
    private IDispatcherTimer? _timer;
    private bool _sync, _active, _recordingAllowed;
    private readonly GraphicsView _art = new() { Drawable = new NightArt(), WidthRequest = 72, HeightRequest = 80 };
    private DateTime Evening => (_date.Date ?? DateTime.Today).Date;
    private bool CanRecord => _window is { } night ? DateTimeOffset.Now >= night.Isha : Evening < DateTime.Today;
    private static string Clock(DateTimeOffset time) => time.ToLocalTime().ToString("hh:mm tt", CultureInfo.GetCultureInfo("ar-EG"));
    public QiyamPage(LocationService locations, IPrayerService prayers, IAppNavigator navigator, AzkarRepository azkar)
    {
        _locations = locations; _prayers = prayers; _navigator = navigator; _azkar = azkar; Ui.Page(this, "قيام الليل");
        Ui.ThemeProperty(_date, DatePicker.TextColorProperty, "AppInk"); Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted"); Ui.ThemeProperty(_reminderStatus, Label.TextColorProperty, "AppMuted");
        var hero = new Grid { ColumnDefinitions = { new(72), new(GridLength.Star) }, FlowDirection = FlowDirection.LeftToRight, ColumnSpacing = 12 };
        hero.Add(_art);
        var words = Ui.Stack(Ui.Label("لحظات هادئة تقرّبك", 18, true), Ui.Label("موعد لصلاتك وقرآنك ودعائك", 13)); words.FlowDirection = FlowDirection.RightToLeft; words.VerticalOptions = LayoutOptions.Center; words.Spacing = 3; hero.Add(words, 1);
        var timeRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 7 };
        for (var i = 0; i < 3; i++)
        {
            var label = Ui.Label(new[] { "العشاء", "الثلث الأخير", "الفجر" }[i], 12); label.HorizontalTextAlignment = TextAlignment.Center; _times[i].HorizontalTextAlignment = TextAlignment.Center;
            var cell = Ui.Card(Ui.Stack(label, _times[i])); cell.Padding = new Thickness(3, 8); timeRow.Add(cell, i);
        }
        _countdown.HorizontalTextAlignment = TextAlignment.Center;
        var location = Ui.AsyncButton("تحديث الموقع والمواقيت", async () =>
        {
            _status.Text = "جارٍ تحديد الموقع…";
            try { await _locations.AcquireAsync(_cancel?.Token ?? default); RefreshWindow(); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _status.Text = ex.Message; }
        }, true); location.FontSize = 13;
        _count.HorizontalTextAlignment = TextAlignment.Center; _goalText.HorizontalTextAlignment = TextAlignment.Center;
        _add = Ui.Button("سجلت ركعتين", () => Change(2)); _undo = Ui.Button("تراجع", () => Change(-2), true);
        var counts = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 }; counts.Add(_add); counts.Add(_undo, 1);
        _done = Ui.AsyncButton("أتممت ورد الليلة", async () =>
        {
            var log = QiyamSettings.Load(Evening); if (!CanRecord || log.Completed || log.Rakaat < log.Goal) return;
            if (await PopupSheet.ChooseAsync(this, "تأكيد إتمام ورد هذه الليلة؟", "نعم، أتممت وردي", "لاحقًا") == "نعم، أتممت وردي") { QiyamSettings.Save(Evening, log with { Completed = true }); RenderLog(); }
        }, true);
        var witrRow = Row("أديت الوتر", _witr);
        _witr.Toggled += (_, e) => { if (!_sync && CanRecord) { var log = QiyamSettings.Load(Evening); QiyamSettings.Save(Evening, log with { Witr = e.Value }); } };
        var saveGoal = Ui.Button("حفظ الهدف", () =>
        {
            _goal.Unfocus();
            if (!int.TryParse(NumberInput.Normalize(_goal.Text ?? ""), out var n) || n is < 2 or > 100 || n % 2 != 0) { _goalText.Text = "اختر عددًا زوجيًا من 2 إلى 100 دون الوتر"; return; }
            QiyamSettings.Goal = n; var log = QiyamSettings.Load(Evening); QiyamSettings.Save(Evening, log with { Goal = n, Completed = log.Completed && log.Rakaat >= n }); RenderLog();
        }, true); saveGoal.FontSize = 13;
        var goalRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 8 }; goalRow.Add(_goal); goalRow.Add(saveGoal, 1);
        _reminder.Toggled += async (_, e) =>
        {
            if (_sync) return;
            if (e.Value) { var enabled = await QiyamCoordinator.EnableAsync(); _sync = true; _reminder.IsToggled = enabled; _sync = false; }
            else { QiyamSettings.Enabled = false; QiyamCoordinator.Refresh(); }
            RenderReminder();
        };
        _lead.SelectedIndexChanged += (_, _) => { if (_sync) return; QiyamSettings.LeadIndex = _lead.SelectedIndex; QiyamCoordinator.Refresh(); RenderReminder(); };
        var test = Ui.AsyncButton("اختبار تذكير بعد 10 ثوانٍ", () => { QiyamCoordinator.Test(); _reminderStatus.Text = "أُرسل اختبار التنبيه. يمكنك إغلاق الشاشة."; return Task.CompletedTask; }, true); test.FontSize = 13;
        var read = Ui.AsyncButton("فتح وردي من القرآن", async () =>
        {
            if (DailyWirdStore.Load() is { } wird) Preferences.Set("quran.wird-page.request", wird.StartPage); else Preferences.Set("quran.resume.request", true);
            await _navigator.GoToAsync("quran");
        }); read.FontSize = 13;
        var body = Ui.Stack(hero, Ui.Label("ليلة تبدأ مغرب هذا اليوم", 13), _date,
            Ui.Card(Ui.Stack(_countdown, timeRow, _status, location)),
            Ui.Card(Ui.Stack(Ui.Label("وردي الشخصي لليلة", 15, true), _count, _goalText, _progress, counts, witrRow, _done, Ui.Label("هدف شخصي للركعات دون الوتر، وليس عددًا لازمًا. الإتمام يُسجل بتأكيدك. يمكنك اختيار ليلة سابقة لتسجيلها لاحقًا.", 12), goalRow, read)),
            Ui.Card(Ui.Stack(Row("تذكير قيام الليل", _reminder), _lead, _reminderStatus, test)),
            Ui.Label("أدعية نبوية", 15, true), _duas,
            Ui.Card(Ui.Stack(Ui.Label("وقت القيام ومصدر الحساب", 15, true), Ui.Label("صلاة الليل من بعد العشاء إلى الفجر. ويُحسب الثلث الأخير من المدة بين المغرب والفجر. المواقيت حسب الموقع وطريقة الحساب المختارة في إعدادات الصلاة.", 13),
                Ui.AsyncButton("وقت صلاة الليل · الأزهر", () => Browser.Default.OpenAsync("https://azhar.eg/fatwacenter/fatwa/ebadat/ArtMID/7985/ArticleID/47277/صلاةُ-الليل"), true),
                Ui.AsyncButton("حساب الثلث الأخير · دار الإفتاء", () => Browser.Default.OpenAsync("https://www.dar-alifta.org/en/fatwa/details/7920/which-hour-coincides-with-the-last-third-of-the-night-so-as-to-perform-the-night"), true))));
        body.Padding = new Thickness(16, 12, 16, 24); body.Spacing = 7;
        var host = new Grid { ClassId = "choice-host" }; host.Add(new ScrollView { Content = body }); Content = host;
        _date.DateSelected += (_, _) => { if (!_sync) RefreshWindow(); };
    }
    private static Grid Row(string title, View control)
    {
        var row = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; var label = Ui.Label(title, 13); label.VerticalOptions = LayoutOptions.Center; row.Add(label); row.Add(control, 1); return row;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _active = true; _cancel = new(); AppAppearance.Changed += RefreshArt;
        _sync = true; _date.MaximumDate = DateTime.Today; _date.MinimumDate = DateTime.Today.AddDays(-30); _date.Date = QiyamService.CurrentEvening(_locations.Saved()?.Location); _reminder.IsToggled = QiyamSettings.Enabled; _lead.SelectedIndex = QiyamSettings.LeadIndex; _sync = false;
        RefreshWindow(); _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += Tick; _timer.Start();
        try
        {
            var data = await _azkar.LoadAsync(); if (!_active) return; _duas.Clear();
            foreach (var dua in data.Where(d => d.Type == 6).Take(3)) { var card = Ui.Card(Ui.Stack(Ui.Label(dua.Text, 17), Ui.Label(dua.Source, 12))); card.Padding = 14; _duas.Add(card); }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    protected override void OnDisappearing()
    {
        _active = false; AppAppearance.Changed -= RefreshArt; _cancel?.Cancel(); _cancel?.Dispose(); _cancel = null;
        if (_timer is not null) { _timer.Stop(); _timer.Tick -= Tick; _timer = null; } base.OnDisappearing();
    }
    protected override bool OnBackButtonPressed() => PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    private void RefreshWindow()
    {
        _window = null;
        try
        {
            var location = _locations.Saved();
            if (location is null) { _status.Text = "حدد الموقع من الزر أو إعدادات التطبيق. حساب المواعيد محلي."; }
            else { _window = QiyamService.Calculate(location.Location, Evening, _prayers); _status.Text = "مواعيد محسوبة حسب إعدادات الصلاة · " + location.Description; }
        }
        catch (Exception ex) { _status.Text = ex.Message; }
        for (var i = 0; i < 3; i++) _times[i].Text = _window is null ? "—" : Clock(new[] { _window.Isha, _window.LastThird, _window.Fajr }[i]);
        RenderLog(); RenderReminder(); Tick(null, EventArgs.Empty);
    }
    private void RefreshArt() => _art.Invalidate();
    private void Tick(object? sender, EventArgs e)
    {
        if (_recordingAllowed != CanRecord) RenderLog();
        if (_window is not { } window) { _countdown.Text = "حدد موقعك لعرض وقت الليلة"; return; }
        var now = DateTimeOffset.Now;
        if (now >= window.Fajr) { _countdown.Text = "انتهى وقت هذه الليلة عند الفجر"; return; }
        var target = now < window.LastThird ? window.LastThird : window.Fajr; var span = target - now;
        _countdown.Text = (now < window.LastThird ? "باقٍ للثلث الأخير " : "الثلث الأخير الآن · باقٍ للفجر ") + $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
    }
    private void Change(int delta)
    {
        if (!CanRecord) return; var log = QiyamSettings.Load(Evening); var count = Math.Clamp(log.Rakaat + delta, 0, 1000);
        QiyamSettings.Save(Evening, log with { Rakaat = count, Completed = log.Completed && count >= log.Goal }); RenderLog();
    }
    private void RenderLog()
    {
        var log = QiyamSettings.Load(Evening); _recordingAllowed = CanRecord; _count.Text = log.Rakaat.ToString(); _goal.Text = log.Goal.ToString();
        _goalText.Text = log.Completed ? "✓ ورد هذه الليلة مكتمل بتأكيدك" : $"هدف الليلة {log.Goal} ركعات دون الوتر" + (!CanRecord ? " · يبدأ التسجيل بعد العشاء" : "");
        _progress.Progress = Math.Min(1, log.Rakaat / (double)log.Goal); _add.IsEnabled = CanRecord; _undo.IsEnabled = CanRecord && log.Rakaat > 0;
        _done.IsEnabled = CanRecord && !log.Completed && log.Rakaat >= log.Goal; _witr.IsEnabled = CanRecord;
        _sync = true; _witr.IsToggled = log.Witr; _sync = false;
    }
    private void RenderReminder()
    {
        _reminderStatus.Text = !QiyamSettings.Enabled ? "التذكير اختياري وغير مفعّل" : _locations.Saved() is null ? "التذكير مفعّل؛ حدد الموقع لجدولته" : "يعمل دون إنترنت · يعاد ضبطه مع الموقع وإعدادات المواقيت";
#if ANDROID
        if (QiyamSettings.Enabled && OperatingSystem.IsAndroidVersionAtLeast(31) && !AlMithaq.Platforms.Android.AdhanScheduler.CanSchedule) _reminderStatus.Text += " · قد يؤخر النظام التنبيه دون إذن المنبهات الدقيقة";
#endif
    }
}
internal sealed class NightArt : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        var x = rect.Center.X; var y = rect.Center.Y;
        var background = Ui.Paper; canvas.FillColor = Ui.Gold; canvas.FillCircle(x - 4, y, 25);
        canvas.FillColor = background; canvas.FillCircle(x + 7, y - 7, 23);
        canvas.FillColor = Ui.Gold; canvas.FillCircle(x + 23, y - 22, 2.5f); canvas.FillCircle(x + 24, y + 18, 1.8f); canvas.FillCircle(x - 26, y - 27, 1.5f);
    }
}
