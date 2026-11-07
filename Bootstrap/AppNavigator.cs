#if !IOS
using AlMithaq.Core.Abstractions;
using AlMithaq.Shared.Controls;

namespace AlMithaq;

public sealed class AppNavigator(IServiceProvider services) : IAppNavigator, IDisposable
{
    private readonly Dictionary<string, NavigationPage> _pages = [];
    private readonly AlMithaq.Core.Logic.NavigationHistory _history = new();
    private FlyoutPage? _host;
    private bool _disposed;
    public event Action<string>? Navigated;
    public void Attach(FlyoutPage host) { _host = host; Navigate("home"); }
    public Task GoToAsync(string route)
    {
        if (!MainThread.IsMainThread) return MainThread.InvokeOnMainThreadAsync(() => Navigate(route));
        Navigate(route); return Task.CompletedTask;
    }
    public Task GoBackAsync()
    {
        void Back() => Navigate(_history.Back(), remember: false);
        if (!MainThread.IsMainThread) return MainThread.InvokeOnMainThreadAsync(Back);
        Back(); return Task.CompletedTask;
    }
    private void Navigate(string route, bool remember = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        StartupTrace.Write("Navigate begin");
        if (_host is null) throw new InvalidOperationException("Navigation host is not ready.");
        route = route.Trim('/');
        if (!_pages.TryGetValue(route, out var navigation))
        {
            ContentPage page = route switch
            {
                "guide" => new GuidePage(),
                "qiyam" => services.GetRequiredService<QiyamPage>(), "khatmah" => services.GetRequiredService<KhatmahPage>(), "hifz" => services.GetRequiredService<MemorizationPage>(),
                "home" => services.GetRequiredService<HomePage>(), "quran" => services.GetRequiredService<QuranPage>(),
                "azkar" => services.GetRequiredService<AzkarPage>(), "prayer" => services.GetRequiredService<PrayerTimesPage>(),
                "qibla" => services.GetRequiredService<QiblaPage>(), "tasbeeh" => services.GetRequiredService<TasbeehPage>(),
                "settings" => services.GetRequiredService<SettingsPage>(), "sources" => services.GetRequiredService<SourcesPage>(), "zakat" => services.GetRequiredService<ZakatPage>(), _ => throw new ArgumentOutOfRangeException(nameof(route))
            };
            StartupTrace.Write("Content page created");
            navigation = new NavigationPage(page) { BarBackgroundColor = Ui.Green, BarTextColor = Colors.White };
            StartupTrace.Write("Navigation page created");
            _pages[route] = navigation;
        }
        var changedSection = _host.Detail != navigation;
        if (route == "quran" && navigation.RootPage is AlMithaq.Pages.QuranPage reader)
        {
            if (changedSection) reader.PrepareSectionEntry();
            else if (!Preferences.Get("quran.resume.request", false) && Preferences.Get("quran.wird-page.request", 0) == 0 && string.IsNullOrEmpty(Preferences.Get("quran.practice.request", ""))) reader.ShowLibrary();
        }
        if (changedSection) _host.Detail = navigation;
        if (remember) _history.Visit(route);
        StartupTrace.Write("Detail attached");
        _host.IsPresented = false; Navigated?.Invoke(route);
    }
    public void Dispose()
    {
        _disposed = true; _host = null; _pages.Clear(); Navigated = null;
    }

}

#endif
