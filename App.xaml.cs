namespace AlMithaq;
public partial class App : Application
{
    private readonly IServiceProvider _services;
    public App(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        AlMithaq.Shared.Controls.AppAppearance.Apply(AlMithaq.Shared.Controls.AppAppearance.Selection, false, this);
        RequestedThemeChanged += (_, _) => { if (AlMithaq.Shared.Controls.AppAppearance.Selection == 0) AlMithaq.Shared.Controls.AppAppearance.Apply(0, false); };
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        StartupTrace.Write("CreateWindow begin");
        // Android can recreate the activity/window while retaining the Application process.
        // Navigation pages belong to one window and must never be reused in another.
        var scope = _services.CreateScope();
        var launch = new BrandLaunchPage(() => new AppShell(scope.ServiceProvider.GetRequiredService<AppNavigator>()));
        var window = new Window(launch);
        window.Stopped += (_, _) => launch.Suspend();
        window.Resumed += (_, _) => launch.Resume();
#if IOS
        window.Activated += (_, _) =>
        {
            _ = Platforms.iOS.LocalReminders.RefreshAsync();
            _ = Platforms.iOS.AppStoreUpdates.CheckAsync(window);
        };
#endif
        window.Destroying += (_, _) => { launch.Suspend(); scope.Dispose(); };
        return window;
    }
}
