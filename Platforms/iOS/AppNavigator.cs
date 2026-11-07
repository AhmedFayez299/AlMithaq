#if IOS
using AlMithaq.Core.Abstractions;

namespace AlMithaq;

// UIKit owns one navigation controller; do not swap cached NavigationPage controllers.
public sealed class AppNavigator(IServiceProvider services) : IAppNavigator, IDisposable
{
    private readonly Dictionary<string, ContentPage> _pages = [];
    private readonly Core.Logic.NavigationHistory _history = new();
    private readonly SemaphoreSlim _navigation = new(1, 1);
    private AppShell? _host;
    private bool _disposed;
    public event Action<string>? Navigated;

    internal ContentPage CreateHome() => Resolve("home");

    public void Attach(AppShell host)
    {
        _host = host;
        _history.Visit("home");
    }

    public Task GoToAsync(string route) => MainThread.InvokeOnMainThreadAsync(() => NavigateAsync(route));
    public Task GoBackAsync() => MainThread.InvokeOnMainThreadAsync(() => NavigateAsync(null));

    private ContentPage Resolve(string route)
    {
        if (_pages.TryGetValue(route, out var page)) return page;
        page = route switch
        {
            "home" => services.GetRequiredService<HomePage>(), "quran" => services.GetRequiredService<QuranPage>(),
            "guide" => new GuidePage(), "qiyam" => services.GetRequiredService<QiyamPage>(),
            "khatmah" => services.GetRequiredService<KhatmahPage>(), "hifz" => services.GetRequiredService<MemorizationPage>(),
            "azkar" => services.GetRequiredService<AzkarPage>(), "prayer" => services.GetRequiredService<PrayerTimesPage>(),
            "qibla" => services.GetRequiredService<QiblaPage>(), "tasbeeh" => services.GetRequiredService<TasbeehPage>(),
            "settings" => services.GetRequiredService<SettingsPage>(), "sources" => services.GetRequiredService<SourcesPage>(),
            "zakat" => services.GetRequiredService<ZakatPage>(), _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
        _pages.Add(route, page);
        return page;
    }

    private async Task NavigateAsync(string? route)
    {
        await _navigation.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_host is null) return;
            var remember = route is not null;
            route = route?.Trim('/') ?? _history.Back();
            await _host.CloseMenuAsync();
            var page = Resolve(route);
            if (page is QuranPage reader)
            {
                if (_host.CurrentPage != page) reader.PrepareSectionEntry();
                else if (!Preferences.Get("quran.resume.request", false) && Preferences.Get("quran.wird-page.request", 0) == 0 && string.IsNullOrEmpty(Preferences.Get("quran.practice.request", ""))) reader.ShowLibrary();
            }
            if (_host.CurrentPage != page)
            {
                if (_host.Navigation.NavigationStack.Contains(page))
                    await _host.PopToRootAsync(false);
                else
                {
                    await _host.PushAsync(page, false);
                    foreach (var old in _host.Navigation.NavigationStack.Skip(1).Where(p => p != page).ToArray())
                        _host.Navigation.RemovePage(old);
                }
            }
            if (remember) _history.Visit(route);
            Navigated?.Invoke(route);
        }
        finally { _navigation.Release(); }
    }

    public void Dispose() { _disposed = true; _host = null; _pages.Clear(); Navigated = null; }
}

#endif
