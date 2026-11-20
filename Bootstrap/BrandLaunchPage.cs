using AlMithaq.Shared.Controls;

namespace AlMithaq;

// Android's system splash remains native; this lightweight page supplies animated text.
public sealed class BrandLaunchPage : ContentPage
{
    private readonly Func<Page> _createRoot;
    private Page? _preparedRoot;
    private readonly Image _logo = new() { Source = "mithaqlogo.png", WidthRequest = 150, HeightRequest = 150, Opacity = 1, Scale = 1, TranslationY = 0, Rotation = 0 };
    private readonly GraphicsView _halo = new() { WidthRequest = 210, HeightRequest = 210, Drawable = new LaunchHalo(), InputTransparent = true, Opacity = 0, Scale = .85 };
    private readonly Label _name = Ui.Label("الميثاق", 38, true);
    private readonly Label _tagline = Ui.Label("لحظات تقربك... وتملأ يومك طمأنينة", 17);
    private readonly Button _retry;
    private bool _starting, _completed, _visible, _suspended, _loaded;
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
        artwork.Loaded += (_, _) => { _loaded = true; Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(32), async () => await StartAsync()); };
    }
    private sealed class LaunchHalo : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            var size = Math.Min(bounds.Width, bounds.Height) - 4;
            if (size <= 0) return;
            canvas.StrokeColor = Ui.Gold; canvas.StrokeSize = 1.5f;
            canvas.DrawEllipse(bounds.X + (bounds.Width - size) / 2, bounds.Y + (bounds.Height - size) / 2, size, size);
        }
    }
    protected override void OnAppearing() { base.OnAppearing(); _visible = true; if (_loaded) Dispatcher.Dispatch(async () => await StartAsync()); }
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
    private async Task AnimateIntroAsync(CancellationToken token)
    {
        await Task.Delay(250, token);
        await Task.WhenAll(_halo.FadeToAsync(.65, 500), _halo.ScaleToAsync(1, 500, Easing.CubicOut));
        token.ThrowIfCancellationRequested();
        await Task.WhenAll(_name.FadeToAsync(1, 350), _name.TranslateToAsync(0, 0, 350, Easing.CubicOut), _name.ScaleToAsync(1, 350, Easing.CubicOut));
        await Task.WhenAll(_tagline.FadeToAsync(1, 350), _tagline.TranslateToAsync(0, 0, 350, Easing.CubicOut));
        await _logo.ScaleToAsync(1.04, 300, Easing.SinInOut);
        await _logo.ScaleToAsync(1, 300, Easing.SinInOut);
        token.ThrowIfCancellationRequested();
    }

    private async Task StartAsync()
    {
        if (_starting || _completed || _suspended || !_visible || !_loaded) return;
        _starting = true; _retry.IsVisible = false;
        var attempt = new CancellationTokenSource(); _attempt = attempt;
        try
        {
            StartupTrace.Write("Brand animation begin");
            var artworkReady = Task.Run(async () => await Task.WhenAll(HomeDesignArt.PreloadAsync(), HomeBannerArt.PreloadAsync(), HomePhotoArt.PreloadAsync()));
#if ANDROID
            // The native live cover is already animating, independently of this UI thread.
            var animation = Task.CompletedTask;
#else
            var animation = AnimateIntroAsync(attempt.Token);
            await Task.Delay(100, attempt.Token);
#endif
            // Build the managed home/menu tree during the intro, not after its final frame.
            var root = _preparedRoot ??= _createRoot();
            await Task.WhenAll(animation, artworkReady).WaitAsync(attempt.Token);
            if (Window is not { } window || window.Page != this) return;
            attempt.Token.ThrowIfCancellationRequested();
            _completed = true; LaunchTransition.Show(window, root);
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
