using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Logic;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Shared.Controls;

namespace AlMithaq.Pages;

public partial class QiblaPage : ContentPage
{
    private readonly IAppNavigator _navigator;
    private readonly LocationService _locations;
    private readonly Label _status = Ui.Label("حدد موقعك لعرض اتجاه القبلة", 11);
    private readonly Label _angle = Ui.Label("حدد موقعك لحساب زاوية القبلة", 17, true);
    private readonly Label _alignment = Ui.Label("السهم غير نشط حتى تحديد الموقع وقراءة الحساس", 14);
    private readonly Label _locationState = Ui.Label("الموقع · غير محدد", 12);
    private readonly Label _sensorState = Ui.Label("البوصلة · انتظار", 12);
    private readonly CompassDial _dial = new();
    private readonly GraphicsView _compass;
    private CancellationTokenSource? _cancel;
    private double _bearing, _heading, _declination;
    private bool _hasLocation, _hasHeading, _ownsSensor;
    public QiblaPage(LocationService locations, IAppNavigator navigator)
    {
        InitializeComponent(); Ui.Page(this, "اتجاه القبلة"); _navigator = navigator; _locations = locations;
        _compass = new GraphicsView { Drawable = _dial, HeightRequest = 268, HorizontalOptions = LayoutOptions.Fill, FlowDirection = FlowDirection.LeftToRight };
        _angle.HorizontalTextAlignment = _alignment.HorizontalTextAlignment = TextAlignment.Center;
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        var title = Ui.Label("وجهتك إلى البيت الحرام", 17, true);
        var art = new Image { Source = "scene_qibla.png", WidthRequest = 48, HeightRequest = 48 };
        var heading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48) }, ColumnSpacing = 10 }; title.VerticalOptions = LayoutOptions.Center; heading.Add(title); heading.Add(art, 1);
        var statusRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
        foreach (var label in new[] { _locationState, _sensorState }) { label.HorizontalTextAlignment = TextAlignment.Center; Ui.ThemeProperty(label, Label.TextColorProperty, "AppMuted"); }
        statusRow.Add(_locationState); statusRow.Add(_sensorState, 1);
        var guide = Ui.Label("أعلى الهاتف هو اتجاه نظرك", 12); guide.HorizontalTextAlignment = TextAlignment.Center; Ui.ThemeProperty(guide, Label.TextColorProperty, "AppMuted");
        var dialBody = Ui.Stack(statusRow, _compass, _angle, _alignment, guide); dialBody.Spacing = 6;
        var compassCard = Ui.Card(dialBody); compassCard.Padding = new Thickness(12, 14, 12, 18);
        var actions = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48) }, ColumnSpacing = 8 };
        var start = Ui.AsyncButton("تحديث الموقع والبوصلة", StartAsync); start.FontSize = 12;
        actions.Add(start); actions.Add(Ui.Icon("\uf013", "إعداد الموقع يدويًا", () => _navigator.GoToAsync("//settings")), 1);
        var tips = Ui.Label("أمسك الهاتف أفقيًا، بعيدًا عن المعادن. للمعايرة حرّكه بشكل 8. تعتمد دقة الاتجاه على حساس جهازك.", 11); Ui.ThemeProperty(tips, Label.TextColorProperty, "AppMuted");
        var stack = Ui.Stack(heading, compassCard, _status, actions, Ui.Card(Ui.Stack(Ui.Label("للحصول على قراءة أدق", 12, true), tips)));
        stack.Spacing = 6; stack.Padding = new Thickness(16, 10, 16, 24); Content = new ScrollView { Content = stack };
    }
    protected override void OnAppearing()
    {
        base.OnAppearing(); AppAppearance.Changed += RefreshPalette; _cancel = new(); var saved = _locations.Saved();
        if (saved is not null) { SetLocation(saved); StartSensor(); }
    }
    protected override void OnDisappearing()
    {
        _cancel?.Cancel(); _cancel?.Dispose(); _cancel = null;
        if (_ownsSensor) { Compass.Default.ReadingChanged -= OnReading; Compass.Default.Stop(); _ownsSensor = false; }
        AppAppearance.Changed -= RefreshPalette;
        _hasHeading = false; _dial.HasHeading = false; _alignment.Text = "بانتظار قراءة البوصلة"; _sensorState.Text = "البوصلة · انتظار"; base.OnDisappearing();
    }
    private void RefreshPalette() { _compass.Invalidate(); _alignment.TextColor = QiblaMath.IsAligned(_bearing, _heading, _hasLocation, _hasHeading) ? (AppAppearance.IsDark ? Color.FromArgb("#8FD8B3") : Ui.Green) : Ui.Ink; }
    private async Task StartAsync()
    {
        var ct = _cancel?.Token ?? default; _status.Text = "جارٍ تحديد الموقع…";
        try { var location = await _locations.AcquireAsync(ct); if (ct.IsCancellationRequested) return; SetLocation(location); StartSensor(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = ex.Message; }
    }
    private void SetLocation(DeviceLocation location)
    {
        _bearing = QiblaMath.Bearing(location.Location.Latitude, location.Location.Longitude); _hasLocation = true;
        _declination = 0;
#if ANDROID
        var field = new Android.Hardware.GeomagneticField((float)location.Location.Latitude, (float)location.Location.Longitude,
            (float)(location.Location.Altitude ?? 0), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _declination = field.Declination;
        _status.Text = "تم تصحيح الانحراف المغناطيسي وفق الموقع والتاريخ";
#else
        _status.Text = "السهم تقريبي: قراءة الحساس مغناطيسية. استخدم الزاوية الرقمية مع الشمال الحقيقي للتحقق.";
#endif
        _angle.Text = $"القبلة {_bearing:F1}° من الشمال الحقيقي";
        _dial.Bearing = _bearing; _dial.HasLocation = true; _locationState.Text = "الموقع · محدد"; _compass.Invalidate();
    }
    private void StartSensor()
    {
        if (!_hasLocation || _ownsSensor) return;
        if (!Compass.Default.IsSupported) { _sensorState.Text = "البوصلة · غير متاحة"; _alignment.Text = "السهم غير نشط · لا يتوفر حساس بوصلة"; _status.Text = "زاوية القبلة محسوبة من الشمال الحقيقي. استخدم بوصلة خارجية للتحقق من الاتجاه."; return; }
        try { Compass.Default.ReadingChanged += OnReading; Compass.Default.Start(SensorSpeed.UI); _ownsSensor = true; _sensorState.Text = "البوصلة · جارٍ الاتصال"; }
        catch (Exception ex) { Compass.Default.ReadingChanged -= OnReading; _status.Text = ex.Message; }
    }
    private void OnReading(object? sender, CompassChangedEventArgs e)
    {
        if (!double.IsFinite(e.Reading.HeadingMagneticNorth)) return;
        var target = QiblaMath.Normalize(e.Reading.HeadingMagneticNorth + _declination);
        _heading = _hasHeading ? QiblaMath.Normalize(_heading + QiblaMath.Delta(target, _heading) * .22) : target;
        _hasHeading = true;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!_ownsSensor) return; var delta = QiblaMath.Delta(_bearing, _heading);
            _sensorState.Text = "البوصلة · نشطة"; _dial.Heading = _heading; _dial.HasHeading = true; _compass.Invalidate();
            _alignment.Text = QiblaMath.IsAligned(_bearing, _heading, _hasLocation, _hasHeading) ? "✓ أنت باتجاه القبلة تقريبًا" : $"استدر {(delta > 0 ? "يمينًا" : "يسارًا")} {Math.Abs(delta):F0}°";
            RefreshPalette();
        });
    }
}

public sealed class CompassDial : IDrawable
{
    public double Bearing { get; set; }
    public double Heading { get; set; }
    public bool HasHeading { get; set; }
    public bool HasLocation { get; set; }
    public void Draw(ICanvas canvas, RectF rect)
    {
        var cx = rect.Center.X; var cy = rect.Center.Y;
        var radius = Math.Min(rect.Width, rect.Height) / 2 - 18;
        var ready = HasLocation && HasHeading;
        var aligned = QiblaMath.IsAligned(Bearing, Heading, HasLocation, HasHeading);
        var accent = !ready ? Color.FromArgb("#96A79E") : aligned ? Color.FromArgb("#36B67B") : Ui.Gold;
        var heading = HasHeading ? Heading : 0;
        canvas.FillColor = AppAppearance.IsDark ? Color.FromArgb("#203A31") : Color.FromArgb("#F5F3EA");
        canvas.FillCircle(cx, cy, radius);
        canvas.StrokeColor = accent.WithAlpha(aligned ? .28f : .13f); canvas.StrokeSize = aligned ? 9 : 6; canvas.DrawCircle(cx, cy, radius + 3);
        canvas.StrokeColor = accent; canvas.StrokeSize = 1.3f; canvas.DrawCircle(cx, cy, radius);
        canvas.StrokeColor = Ui.Gold.WithAlpha(.25f); canvas.StrokeSize = .7f; canvas.DrawCircle(cx, cy, radius - 39);
        canvas.FontSize = 12;
        for (var i = 0; i < 360; i += 5)
        {
            var a = (i - heading - 90) * Math.PI / 180;
            var major = i % 30 == 0;
            canvas.StrokeColor = i == 0 ? accent : Ui.Gold.WithAlpha(major ? .9f : .5f); canvas.StrokeSize = major ? 1.7f : .7f;
            canvas.DrawLine(cx + (float)Math.Cos(a) * (radius - 6), cy + (float)Math.Sin(a) * (radius - 6), cx + (float)Math.Cos(a) * (radius - (major ? 16 : 11)), cy + (float)Math.Sin(a) * (radius - (major ? 16 : 11)));
            if (!major) continue;
            var x = cx + (float)Math.Cos(a) * (radius - 28); var y = cy + (float)Math.Sin(a) * (radius - 28);
            canvas.FontSize = i % 90 == 0 ? 14 : 12;
            canvas.FontColor = i == 0 ? accent : Ui.Ink.WithAlpha(.8f);
            canvas.DrawString(i switch { 0 => "N", 90 => "E", 180 => "S", 270 => "W", _ => i.ToString() }, x - 18, y - 10, 36, 22, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
        // A fixed marker represents the top edge of the phone.
        var marker = new PathF(); marker.MoveTo(cx, cy - radius + 2); marker.LineTo(cx - 5, cy - radius - 9); marker.LineTo(cx + 5, cy - radius - 9); marker.Close();
        canvas.FillColor = accent; canvas.FillPath(marker);
        // Until both inputs are ready this grey needle is an explicit inactive placeholder.
        var angle = (ready ? Bearing - Heading - 90 : -90) * Math.PI / 180;
        var ux = (float)Math.Cos(angle); var uy = (float)Math.Sin(angle);
        var px = -uy * 13; var py = ux * 13;
        var tipX = cx + ux * (radius - 41); var tipY = cy + uy * (radius - 41);
        var tailX = cx - ux * (radius - 64); var tailY = cy - uy * (radius - 64);
        void Triangle(float ax, float ay, float bx, float by, float dx, float dy, Color fill)
        {
            var path = new PathF(); path.MoveTo(ax, ay); path.LineTo(bx, by); path.LineTo(dx, dy); path.Close(); canvas.FillColor = fill; canvas.FillPath(path);
        }
        Triangle(tipX, tipY, cx + px, cy + py, cx, cy, accent);
        Triangle(tipX, tipY, cx, cy, cx - px, cy - py, accent.WithAlpha(.65f));
        var tail = AppAppearance.IsDark ? Color.FromArgb("#567366") : Color.FromArgb("#CBD6CC");
        Triangle(tailX, tailY, cx + px, cy + py, cx, cy, tail);
        Triangle(tailX, tailY, cx, cy, cx - px, cy - py, tail.WithAlpha(.65f));
        canvas.FillColor = AppAppearance.IsDark ? Color.FromArgb("#172E26") : Colors.White; canvas.FillCircle(cx, cy, 12);
        canvas.StrokeColor = accent; canvas.StrokeSize = 1.5f; canvas.DrawCircle(cx, cy, 12);
        canvas.FillColor = Color.FromArgb("#20372F"); canvas.FillRectangle(cx - 6, cy - 6, 12, 12);
        canvas.FillColor = Ui.Gold; canvas.FillRectangle(cx - 6, cy - 3, 12, 2.5f);
    }
}
