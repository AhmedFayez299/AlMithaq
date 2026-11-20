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
    private readonly List<(string Name, Border Card, Label Time)> _prayerRows = [];
    private string? _paintedNext;
    private bool _updating, _active;
    private Border? _hero;
    private Grid? _dayRow;
    private CancellationTokenSource? _cancel;
    private IDispatcherTimer? _timer;
    private DeviceLocation? _location;
    public PrayerTimesPage(LocationService locations, IPrayerService prayers, IAppNavigator navigator)
    {
        InitializeComponent(); Ui.Page(this, "مواقيت الصلاة"); _navigator = navigator; _locations = locations; _prayers = prayers;
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        Ui.ThemeProperty(_date, DatePicker.TextColorProperty, "AppInk");
        var nextCaption = Ui.Label("الصلاة القادمة",12,true); nextCaption.TextColor = Color.FromArgb("#E8D8AC");
        _next.FontSize = 24; _next.TextColor = Colors.White; _next.MaxLines = 2;
        _countdown.TextColor = Color.FromArgb("#E8D8AC"); _countdown.FontSize = 15;
        var heroText = Ui.Stack(nextCaption,_next,_countdown); heroText.Spacing = 6; heroText.Padding = new Thickness(12,18); heroText.VerticalOptions = LayoutOptions.Center;
        var heroGrid = new Grid { FlowDirection = FlowDirection.LeftToRight,HeightRequest = 160,IsClippedToBounds = true,ColumnDefinitions = { new(new GridLength(.38,GridUnitType.Star)),new(new GridLength(.62,GridUnitType.Star)) } };
        var photo = new HomePhotoArt { HeightRequest = 160 }; Grid.SetColumnSpan(photo,2); heroGrid.Add(photo); heroText.FlowDirection = FlowDirection.RightToLeft; heroGrid.Add(heroText,1);
        var hero = _hero = ScreenDesign.Surface(heroGrid,new Thickness(0)); hero.BackgroundColor = Ui.Green;
        var refresh = Ui.Icon("\uf3c5","تحديد موقعي الحالي",RefreshAsync); refresh.BorderWidth = 0; refresh.BackgroundColor = Colors.Transparent;
        _status.VerticalOptions = LayoutOptions.Center; _status.FontSize = 12;
        var locationRow = new Grid { ColumnDefinitions = { new(GridLength.Star),new(44) },ColumnSpacing = 8 }; locationRow.Add(_status); locationRow.Add(refresh,1);
        var dateRow = _dayRow = new Grid { ColumnDefinitions = { new(GridLength.Star),new(GridLength.Auto) },ColumnSpacing = 10 };
        var dateTitle = Ui.Label("مواقيت اليوم",14,true); dateTitle.VerticalOptions = LayoutOptions.Center; dateRow.Add(dateTitle); dateRow.Add(_date,1);
        _times.Spacing = 6;
        var settings = Ui.AsyncButton("إعدادات الأذان وطريقة الحساب",()=>_navigator.GoToAsync("settings"),true);
        var note = Ui.Label("المواقيت حسب موقعك وتوقيت الهاتف. يمكنك ضبط طريقة الحساب والتصحيح من الإعدادات.",11); Ui.ThemeProperty(note,Label.TextColorProperty,"AppMuted");
        var content = Ui.Stack(ScreenDesign.Intro("على موعد مع الصلاة","مواقيت يومك، حسب موقعك", "\uf017"),hero,ScreenDesign.Surface(locationRow,new Thickness(12,8)),dateRow,_times,settings,note);
        content.Spacing = 9; content.Padding = new Thickness(16,12,16,24); Content = new ScrollView { Content = content };
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
        if (_hero is not null) _hero.IsVisible = _location is not null;
        if (_dayRow is not null) _dayRow.IsVisible = _location is not null;
        _times.Clear(); _badges.Clear(); _prayerRows.Clear(); _paintedNext = null; if (_location is null) { _status.Text = "اضغط «تحديد موقعي» أو أدخل الموقع يدويًا من الإعدادات";  return; }
        try
        {
            _status.Text = $"{_location.Description}\n{PrayerSettings.Methods[PrayerSettings.MethodIndex].Name}";
            foreach (var prayer in _prayers.GetDay(_location.Location, _date.Date ?? DateTime.Today))
            {
                var symbol = Ui.Label(prayer.Name switch { "الفجر" => "\uf186", "الشروق" => "\uf185", "الظهر" => "\uf185", "العصر" => "\uf0c2", "المغرب" => "\uf042", _ => "\uf186" },17);
                symbol.FontFamily = "FontIconSolid"; symbol.TextColor = Ui.Gold; symbol.VerticalTextAlignment = TextAlignment.Center; symbol.HorizontalTextAlignment = TextAlignment.Center;
                var row = new Grid { ColumnDefinitions = { new(26),new(GridLength.Star),new(GridLength.Auto),new(GridLength.Auto) },ColumnSpacing = 10,MinimumHeightRequest = 44 };
                var name = Ui.Label(prayer.Name,14,prayer.IsPrayer); name.VerticalOptions = LayoutOptions.Center;
                var badge = Ui.Label("القادمة",10,true); badge.IsVisible = false; badge.TextColor = Ui.Gold; badge.VerticalOptions = LayoutOptions.Center; _badges[prayer.Name] = badge;
                var time = Ui.Label(prayer.Time.ToString("hh:mm tt",new CultureInfo("ar-EG")),17,true); time.VerticalOptions = LayoutOptions.Center;
                if (!prayer.IsPrayer) { Ui.ThemeProperty(name,Label.TextColorProperty,"AppMuted"); Ui.ThemeProperty(time,Label.TextColorProperty,"AppMuted"); }
                row.Add(symbol); row.Add(name,1); row.Add(badge,2); row.Add(time,3);
                var card = ScreenDesign.Surface(row,new Thickness(12,7)); card.Margin = 0; card.StrokeThickness = .5;
                _prayerRows.Add((prayer.Name,card,time)); _times.Add(card);
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
            foreach (var pair in _badges) pair.Value.IsVisible = pair.Key == next.Name && (_date.Date ?? DateTime.Today).Date == next.Time.Date;
            var active = (_date.Date ?? DateTime.Today).Date == next.Time.Date ? next.Name : "";
            if (_paintedNext != active)
            {
                foreach (var row in _prayerRows) Ui.ThemeProperty(row.Card,Border.BackgroundColorProperty,row.Name == active ? "AppSoft" : "AppCard");
                _paintedNext = active;
            } }
        catch (Exception ex) { _next.Text = "تعذر حساب الصلاة القادمة"; System.Diagnostics.Debug.WriteLine(ex); }
        finally { _updating = false; }
    }
}
