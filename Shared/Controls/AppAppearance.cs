namespace AlMithaq.Shared.Controls;

public static class AppAppearance
{
    public static readonly string[] Names = ["حسب الجهاز", "عاجي هادئ", "ليلي", "ورقي دافئ"];
    public static event Action? Changed;
    private static ResourceDictionary? _palette;
    public static int Selection => Math.Clamp(Preferences.Get("appearance.theme", 1), 0, 3);
    public static bool IsDark => Selection == 2 || Selection == 0 && Application.Current?.PlatformAppTheme == AppTheme.Dark;
    public static void Apply(int selection, bool save = true, Application? application = null)
    {
        var app = application ?? Application.Current; if (app is null) return;
        selection = Math.Clamp(selection, 0, 3);
        if (save) Preferences.Set("appearance.theme", selection);
        var dark = selection == 2 || selection == 0 && app.PlatformAppTheme == AppTheme.Dark;
        var warm = selection == 3;
        var colors = new Dictionary<string, string>
        {
            ["AppPaper"] = dark ? "#101F1C" : warm ? "#F0E5D0" : "#F7F6F0",
            ["AppCard"] = dark ? "#1A302A" : warm ? "#FFF4DC" : "#FFFFFF",
            ["AppInk"] = dark ? "#ECF3E9" : "#193E34",
            ["AppMuted"] = dark ? "#ACC5B9" : "#61786C",
            ["AppSoft"] = dark ? "#29483D" : "#E8EEE5",
            ["AppStroke"] = dark ? "#355044" : "#E0E6DA",
            ["AppAccent"] = dark ? "#8ACCB0" : "#176B57"
        };
        var palette = new ResourceDictionary();
        foreach (var pair in colors) palette[pair.Key] = Color.FromArgb(pair.Value);
        palette["AppStrokeBrush"] = new SolidColorBrush((Color)palette["AppStroke"]);
        if (_palette is not null) app.Resources.MergedDictionaries.Remove(_palette);
        app.Resources.MergedDictionaries.Add(palette); _palette = palette;
        app.UserAppTheme = selection == 0 ? AppTheme.Unspecified : selection == 2 ? AppTheme.Dark : AppTheme.Light;
        Ui.RefreshThemeProperties();
        StartupTrace.Write($"Appearance applied: {selection}; dark={dark}; dictionaries={app.Resources.MergedDictionaries.Count}");
        Changed?.Invoke();
    }
}
