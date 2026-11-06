using AlMithaq.Shared.Controls;

namespace AlMithaq;

// Android's system splash remains native; this lightweight page supplies animated text.
public sealed class BrandLaunchPage : ContentPage
{
    private readonly Func<Page> _createRoot;
    private readonly Image _logo = new() { Source = "mithaqbrand.png", WidthRequest = 194, HeightRequest = 194, Opacity = 0, Scale = .55, TranslationY = 30, Rotation = -10 };
    private readonly Border _halo = new() { WidthRequest = 210, HeightRequest = 210, Stroke = Ui.Gold, StrokeThickness = 1.5, BackgroundColor = Colors.Transparent, Opacity = 0, Scale = .5, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 105 } };
    private readonly Label _name = Ui.Label("الميثاق", 38, true);
    private readonly Label _tagline = Ui.Label("لحظات تقربك... وتملأ يومك طمأنينة", 17);
    private readonly Button _retry;
    private bool _starting, _completed, _visible, _suspended;
    private CancellationTokenSource? _attempt;
    public BrandLaunchPage(Func<Page> createRoot)
    {
        _createRoot = createRoot; BackgroundColor = Color.FromArgb("#123F34"); FlowDirection = FlowDirection.RightToLeft;
        _name.TextColor = Colors.White; _tagline.TextColor = Color.FromArgb("#E5D0A2");
        foreach (var label in new[] { _name, _tagline }) { label.HorizontalTextAlignment = TextAlignment.Center; label.Opacity = 0; label.TranslationY = 28; }
        _name.Scale = .85;
        _retry = Ui.AsyncButton("إعادة المحاولة", StartAsync); _retry.IsVisible = false;
        var logoStage = new Grid { WidthRequest = 230, HeightRequest = 230 }; logoStage.Add(_halo); logoStage.Add(_logo); _halo.HorizontalOptions = _logo.HorizontalOptions = LayoutOptions.Center; _halo.VerticalOptions = _logo.VerticalOptions = LayoutOptions.Center;
        var artwork = Ui.Stack(logoStage, _name, _tagline, _retry); artwork.Spacing = 12; artwork.MaximumWidthRequest = 360; artwork.HorizontalOptions = LayoutOptions.Center; artwork.VerticalOptions = LayoutOptions.Center; artwork.Padding = 24;
        Content = artwork;
    }
    protected override async void OnAppearing() { base.OnAppearing(); _visible = true; await StartAsync(); }
    protected override void OnDisappearing() { _visible = false; CancelAttempt(); base.OnDisappearing(); }
    public void Suspend() { _suspended = true; CancelAttempt(); }
    public void Resume()
    {
        _suspended = false;
        if (Window?.Page == this) Dispatcher.Dispatch(async () => await StartAsync());
    }
    private void CancelAttempt()
    {
        _attempt?.Cancel();
        foreach (var view in new VisualElement[] { _halo, _logo, _name, _tagline }) view.CancelAnimations();
    }
    private async Task StartAsync()
    {
        if (_starting || _completed || _suspended || !_visible) return;
        _starting = true; _retry.IsVisible = false;
        var attempt = new CancellationTokenSource(); _attempt = attempt;
        try
        {
            StartupTrace.Write("Brand animation begin");
            await Task.WhenAll(_halo.FadeToAsync(.55, 600), _halo.ScaleToAsync(1, 1000, Easing.CubicOut), _logo.FadeToAsync(1, 700), _logo.ScaleToAsync(1.08, 900, Easing.SpringOut), _logo.RotateToAsync(0, 850, Easing.CubicOut), _logo.TranslateToAsync(0, 0, 850, Easing.CubicOut));
            attempt.Token.ThrowIfCancellationRequested();
            await _logo.ScaleToAsync(1, 180, Easing.CubicOut);
            await Task.WhenAll(_name.FadeToAsync(1, 500), _name.TranslateToAsync(0, 0, 550, Easing.CubicOut), _name.ScaleToAsync(1, 550, Easing.CubicOut));
            attempt.Token.ThrowIfCancellationRequested();
            await Task.WhenAll(_tagline.FadeToAsync(1, 520), _tagline.TranslateToAsync(0, 0, 520, Easing.CubicOut));
            attempt.Token.ThrowIfCancellationRequested();
            await _logo.ScaleToAsync(1.1, 360, Easing.SinInOut); await _logo.ScaleToAsync(1, 360, Easing.SinInOut);
            attempt.Token.ThrowIfCancellationRequested();
            if (Window is not { } window || window.Page != this) return;
            var root = _createRoot();
            attempt.Token.ThrowIfCancellationRequested();
            _completed = true; window.Page = root;
            StartupTrace.Write("Brand animation complete");
        }
        catch (OperationCanceledException) when (attempt.IsCancellationRequested) { }
        catch (Exception ex) { StartupTrace.Failure(ex); _retry.IsVisible = _visible && !_suspended; }
        finally
        {
            _starting = false;
            if (ReferenceEquals(_attempt, attempt)) _attempt = null;
            var cancelled = attempt.IsCancellationRequested; attempt.Dispose();
            if (cancelled && _visible && !_suspended && !_completed) Dispatcher.Dispatch(async () => await StartAsync());
        }
    }
}
