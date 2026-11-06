using AlMithaq.Core.Logic;
using AlMithaq.Shared.Controls;
using System.Text.Json;

namespace AlMithaq.Features.Quran.Views;

public sealed class MushafPageView : ContentView
{
    private readonly WebView _web = new() { Opacity = 0 };
    private readonly Grid _surface = new();
    private readonly Label _message = Ui.Label("جارٍ فتح صفحة المصحف…", 14);
    private readonly ActivityIndicator _busy = new() { Color = Ui.Gold };
    private readonly Dictionary<int, string> _pages = [];
    private readonly VerticalStackLayout _overlay;
    private int _request, _page, _requestedPage, _surah, _ayah, _theme;
    private bool _ready;
    private double _zoom = 1;
    public event EventHandler<int>? PageSwiped;
    public event EventHandler<double>? ZoomChanged;
    public event EventHandler? ChromeTapped;
    public event EventHandler<(int Surah, int Ayah, double Y)>? VerseTapped;
    public int PageNumber => _page;
    public MushafPageView()
    {
        var retry = Ui.AsyncButton("إعادة فتح الصفحة", () => ShowAsync(_requestedPage, _surah, _ayah), true);
        _overlay = Ui.Stack(_busy, _message, retry); _overlay.VerticalOptions = LayoutOptions.Center; _overlay.HorizontalOptions = LayoutOptions.Center;
        _surface.Add(_web); _surface.Add(_overlay); Content = _surface;
        _web.HandlerChanged += (_, _) =>
        {
            Paint();
#if ANDROID && DEBUG
            global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
        };
        _web.Navigating += (_, e) =>
        {
            if (Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) && uri.Scheme == "almithaq")
            {
                e.Cancel = true; var parts = uri.AbsolutePath.Trim('/').Split('/');
                if (!_ready) return;
                if (uri.Host == "page") PageSwiped?.Invoke(this, uri.AbsolutePath == "/next" ? 1 : -1);
                if (uri.Host == "chrome") ChromeTapped?.Invoke(this, EventArgs.Empty);
                if (uri.Host == "zoom" && double.TryParse(uri.AbsolutePath.Trim('/'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var zoom)) { _zoom = Math.Clamp(zoom, 1, 3.5); ZoomChanged?.Invoke(this, _zoom); }
                if (uri.Host == "verse" && parts.Length == 2 && int.TryParse(parts[0], out var s) && int.TryParse(parts[1], out var a) && s is >= 1 and <= 114 && a is >= 1 and <= 286)
                    VerseTapped?.Invoke(this, (s, a, double.TryParse(uri.Fragment.TrimStart('#'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y) ? Math.Clamp(y, 0, 1) : .5));
            }
            else if (!(e.Url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) || e.Url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || e.Url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))) e.Cancel = true;
        };
        _web.Navigated += async (_, e) =>
        {
            if (e.Result != WebNavigationResult.Success) return;
            try
            {
                var number = await _web.EvaluateJavaScriptAsync("window.mushafPageNumber||0");
                if (!int.TryParse(number.Trim('"'), out var page) || page != _requestedPage) return;
                _ready = true; _page = page;
                await SetZoomAsync(_zoom); await HighlightAsync(_surah, _ayah);
                _overlay.IsVisible = false; _busy.IsRunning = false; _web.Opacity = 1;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        };
        Paint();
    }
    private void Paint()
    {
        var color = Color.FromArgb(_theme == 2 ? "#101f1c" : _theme == 0 ? "#f0e5d0" : "#fffcf2");
        BackgroundColor = _surface.BackgroundColor = _web.BackgroundColor = color;
        _message.TextColor = _theme == 2 ? Color.FromArgb("#ECE8DA") : Ui.Ink;
#if ANDROID
        if (_web.Handler?.PlatformView is global::Android.Webkit.WebView native)
            native.SetBackgroundColor(global::Android.Graphics.Color.ParseColor(_theme == 2 ? "#101f1c" : _theme == 0 ? "#f0e5d0" : "#fffcf2"));
#endif
    }
    private async Task<string?> PageAsync(int page)
    {
        if (page is < 1 or > 604) return null;
        if (_pages.TryGetValue(page, out var cached)) return cached;
        await using var asset = await FileSystem.OpenAppPackageFileAsync($"mushaf/groups/{MushafPageArchive.GroupStart(page):000}.bin.br");
        var prepared = await Task.Run(() => MushafMarkup.PrepareSvg(MushafPageArchive.Read(asset, page), page));
        if (PageHeading?.Invoke(page) is { } heading)
        {
            var surah = System.Net.WebUtility.HtmlEncode(heading.Surah);
            var partition = System.Net.WebUtility.HtmlEncode(heading.Partition);
            prepared = $"<header class='folio-heading'><span>{surah}</span><span>{partition}</span></header>{prepared}<footer class='folio-number'>{page}</footer>";
        }
        _pages[page] = prepared;
        foreach (var old in _pages.Keys.Where(p => Math.Abs(p - _requestedPage) > 2).ToArray()) _pages.Remove(old);
        return prepared;
    }
    public Func<int, (string Surah, string Partition)>? PageHeading { get; set; }
    private int _frameStyle;
    public async Task SetFrameStyleAsync(int style)
    {
        _frameStyle = Math.Clamp(style, 0, 3);
        if (_ready) await _web.EvaluateJavaScriptAsync($"document.body.dataset.frame='{_frameStyle}'");
    }
    public async Task ShowAsync(int page, int surah = 0, int ayah = 0)
    {
        if (page is < 1 or > 604) return;
        _requestedPage = page; _surah = surah; _ayah = ayah;
        if (_page == page && _ready) { await HighlightAsync(surah, ayah); return; }
        var request = ++_request;
        if (!_ready) { _overlay.IsVisible = true; _busy.IsRunning = true; _web.Opacity = 0; _message.Text = $"جارٍ فتح صفحة {page}…"; }
        try
        {
            var leaves = await Task.WhenAll(PageAsync(page), PageAsync(page - 1), PageAsync(page + 1));
            if (request != _request) return;
            if (_ready)
            {
                var arguments = string.Join(",", page.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    JsonSerializer.Serialize(leaves[0]), JsonSerializer.Serialize(leaves[1]), JsonSerializer.Serialize(leaves[2]),
                    _zoom.ToString(System.Globalization.CultureInfo.InvariantCulture), _theme.ToString());
                await _web.EvaluateJavaScriptAsync("window.installPages(" + arguments + ")");
                if (request != _request) return;
                _page = page; await HighlightAsync(surah, ayah);
            }
            else _web.Source = new HtmlWebViewSource { Html = MushafMarkup.BuildDocument(page, leaves[0]!, leaves[1], leaves[2], _theme, _frameStyle) };
        }
        catch (Exception ex)
        {
            if (request != _request) return;
            if (_ready) try { await _web.EvaluateJavaScriptAsync("window.cancelTurn()"); } catch { }
            _message.Text = "تعذر فتح الصفحة. اضغط إعادة المحاولة."; _overlay.IsVisible = true; _busy.IsRunning = false;
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
    public async Task HighlightAsync(int surah, int ayah)
    {
        _surah = surah; _ayah = ayah;
        if (_ready) try { await _web.EvaluateJavaScriptAsync($"window.markAyah({surah},{ayah})"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    public void CancelPending()
    {
        if (_ready) _ = CancelGestureAsync();
        ++_request; _page = _requestedPage = 0; _ready = false; _web.Opacity = 0;
    }
    private async Task CancelGestureAsync()
    {
        try { await _web.EvaluateJavaScriptAsync("window.cancelTurn()"); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    public async Task SetZoomAsync(double zoom)
    {
        _zoom = Math.Clamp(zoom, 1, 3.5);
        if (_ready) try { await _web.EvaluateJavaScriptAsync($"window.setZoom({_zoom.ToString(System.Globalization.CultureInfo.InvariantCulture)})"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
    public async Task SetThemeAsync(int theme)
    {
        _theme = Math.Clamp(theme, 0, 2); Paint();
        if (_ready) try { await _web.EvaluateJavaScriptAsync($"window.setPalette({_theme})"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
}
