using AlMithaq.Core.Abstractions;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Shared.Controls;
using System.Globalization;

namespace AlMithaq.Pages;

public partial class PrayerTimesPage : ContentPage
{
    private readonly IAppNavigator _navigator;
    private readonly LocationService _locations;
    private readonly IPrayerService _prayers;
    private readonly Label _status = Ui.Label("حدد موقعك لحساب المواقيت", 11);
    private readonly Label _next = Ui.Label("حدد موقعك لعرض الصلاة القادمة", 19, true);
    private readonly Label _countdown = Ui.Label("", 15);
    private readonly DatePicker _date = new() { Date = DateTime.Today, Format = "dd MMMM yyyy", FontFamily = "Almarai-Regular", FontSize = 13, HorizontalOptions = LayoutOptions.Fill };
    private readonly VerticalStackLayout _times = new() { Spacing = 0 };
    private readonly Dictionary<string, Label> _badges = [];
    private bool _updating, _active;
    private CancellationTokenSource? _cancel;
    private IDispatcherTimer? _timer;
    private DeviceLocation? _location;
    public PrayerTimesPage(LocationService locations, IPrayerService prayers, IAppNavigator navigator)
    {
        InitializeComponent(); Ui.Page(this, "مواقيت الصلاة"); _navigator = navigator; _locations = locations; _prayers = prayers;
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        Ui.ThemeProperty(_date, DatePicker.TextColorProperty, "AppInk");
        var nextCaption = Ui.Label("الصلاة القادمة", 12); nextCaption.TextColor = Color.FromArgb("#E8D8AC");
        _next.TextColor = Colors.White; _countdown.TextColor = Color.FromArgb("#E8D8AC");
        var heroText = Ui.Stack(nextCaption, _next, _countdown); heroText.Spacing = 3; heroText.VerticalOptions = LayoutOptions.Center;
        var heroGrid = new Grid { FlowDirection = FlowDirection.LeftToRight, ColumnDefinitions = { new(76), new(GridLength.Star) }, ColumnSpacing = 12 };
        heroText.FlowDirection = FlowDirection.RightToLeft;
        heroGrid.Add(new Image { Source = "scene_prayer.png", WidthRequest = 76, HeightRequest = 90, Aspect = Aspect.AspectFit }); heroGrid.Add(heroText, 1);
        var hero = Ui.Card(heroGrid); hero.BackgroundColor = Ui.Green; hero.Padding = 16;
        var refresh = Ui.AsyncButton("تحديد موقعي", RefreshAsync, true); refresh.FontSize = 12; refresh.Padding = new Thickness(8, 2); refresh.MinimumHeightRequest = refresh.HeightRequest = 44; refresh.VerticalOptions = LayoutOptions.Center;
        SemanticProperties.SetDescription(refresh, "تحديد موقعي الحالي لحساب مواقيت الصلاة");
        var locationRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(110) }, ColumnSpacing = 8 };
        _status.VerticalOptions = LayoutOptions.Center; locationRow.Add(_status); locationRow.Add(refresh, 1);
        var dateRow = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 14 };
        var dateTitle = Ui.Label("مواقيت اليوم", 14, true); dateTitle.VerticalOptions = LayoutOptions.Center;
        dateRow.Add(dateTitle); dateRow.Add(_date, 1);
        var list = Ui.Card(_times); list.Padding = new Thickness(14, 4);
        var note = Ui.Label("تُحسب المواقيت على الجهاز دون إنترنت وفق موقعك وتوقيت الهاتف. راجع طريقة الحساب والتصحيح مع مسجدك المحلي.", 11);
        Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
        var settings = Ui.AsyncButton("إعدادات الأذان وطريقة الحساب", () => _navigator.GoToAsync("//settings"), true); settings.FontSize = 12;
        var content = Ui.Stack(hero, locationRow, dateRow, list, settings, note);
        content.Spacing = 8; content.Padding = new Thickness(16, 12, 16, 24); Content = new ScrollView { Content = content };
        _date.DateSelected += (_, _) => Render();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing(); _active = true; _cancel = new(); _location = _locations.Saved(); Render();
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromSeconds(1); _timer.Tick += Tick; _timer.Start();
    }
    protected override void OnDisappearing() { _active = false; _cancel?.Cancel(); _cancel?.Dispose(); _cancel = null; if (_timer is not null) { _timer.Stop(); _timer.Tick -= Tick; _timer = null; } base.OnDisappearing(); }
    private void Tick(object? sender, EventArgs e) => UpdateNext();
    private async Task RefreshAsync()
    {
        var token = _cancel?.Token ?? default; _status.Text = "جارٍ تحديد الموقع…";
        try { var location = await _locations.AcquireAsync(token); if (token.IsCancellationRequested) return; _location = location; Render(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = ex.Message + " — أدخل الموقع يدويًا من الإعدادات إن لزم."; }
    }
    private void Render()
    {
        _times.Clear(); _badges.Clear(); if (_location is null) { _status.Text = "اضغط «تحديد موقعي» أو أدخل الموقع يدويًا من الإعدادات"; _times.Add(Ui.Label("تظهر المواقيت بعد تحديد الموقع", 13)); return; }
        try
        {
            _status.Text = $"{_location.Description}\n{PrayerSettings.Methods[PrayerSettings.MethodIndex].Name}";
            foreach (var prayer in _prayers.GetDay(_location.Location, _date.Date ?? DateTime.Today))
            {
                if (_times.Count > 0) { var line = new BoxView { HeightRequest = 1 }; Ui.ThemeProperty(line, BoxView.ColorProperty, "AppStroke"); _times.Add(line); }
                var row = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) }, ColumnSpacing = 10, Padding = new Thickness(2, 9), MinimumHeightRequest = 52 };
                var name = Ui.Label(prayer.Name, 15, prayer.IsPrayer); name.VerticalOptions = LayoutOptions.Center;
                var badge = Ui.Label("القادمة", 10, true); badge.IsVisible = false; badge.TextColor = Ui.Gold; badge.VerticalOptions = LayoutOptions.Center; _badges[prayer.Name] = badge;
                var time = Ui.Label(prayer.Time.ToString("hh:mm tt", new CultureInfo("ar-EG")), 16, true); time.VerticalOptions = LayoutOptions.Center;
                if (!prayer.IsPrayer) { Ui.ThemeProperty(name, Label.TextColorProperty, "AppMuted"); Ui.ThemeProperty(time, Label.TextColorProperty, "AppMuted"); }
                row.Add(name); row.Add(badge, 1); row.Add(time, 2); _times.Add(row);
            }
            UpdateNext();
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }
    private async void UpdateNext()
    {
        if (_location is null || _updating || !_active) return;
        _updating = true;
        try { var location = _location; var next = await _prayers.GetNextPrayer(location.Location); if (!_active || !ReferenceEquals(location, _location)) return;
            var seconds = Math.Max(0, (long)(next.Time - DateTime.Now).TotalSeconds);
            _next.Text = $"{next.Name} · {next.Time.ToString("hh:mm tt", new CultureInfo("ar-EG"))}";
            _countdown.Text = $"متبقي {seconds / 3600:D2}:{seconds / 60 % 60:D2}:{seconds % 60:D2}";
            foreach (var pair in _badges) pair.Value.IsVisible = pair.Key == next.Name && (_date.Date ?? DateTime.Today).Date == next.Time.Date; }
        catch (Exception ex) { _next.Text = "تعذر حساب الصلاة القادمة"; System.Diagnostics.Debug.WriteLine(ex); }
        finally { _updating = false; }
    }
}
