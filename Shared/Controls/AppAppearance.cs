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
        var paper = dark ? "#071F17" : warm ? "#F0E5D0" : "#FAF5EA";
        var card = dark ? "#0C3023" : warm ? "#FFF1D6" : "#FCF5E8";
        var soft = dark ? "#225C40" : warm ? "#E7D8B8" : "#E0E7CD";
        var stroke = dark ? "#176B57" : warm ? "#DCC59B" : "#E8DDC8";
        var colors = new Dictionary<string, string>
        {
            ["HomePaper"] = paper,
            ["HomeCard"] = card,
            ["HomeStroke"] = stroke,
            ["HomeBanner"] = dark ? "#10392A" : warm ? "#FFF4DE" : "#FFF8EB",
            ["HomeSelected"] = soft,
            ["AppPaper"] = paper,
            ["AppCard"] = card,
            ["AppInk"] = dark ? "#ECF3E9" : "#193E34",
            ["AppMuted"] = dark ? "#ACC5B9" : "#61786C",
            ["AppSoft"] = soft,
            ["AppStroke"] = stroke,
            ["AppAccent"] = dark ? "#8ACCB0" : "#176B57"
        };
        var palette = new ResourceDictionary();
        foreach (var pair in colors) palette[pair.Key] = Color.FromArgb(pair.Value);
        palette["AppStrokeBrush"] = new SolidColorBrush((Color)palette["AppStroke"]);
        palette["HomeStrokeBrush"] = new SolidColorBrush((Color)palette["HomeStroke"]);
        palette["HomeStrokeWidth"] = dark ? .6d : 0d;
        palette["HomeArrow"] = dark ? Colors.White : Color.FromArgb("#173C34");
        palette["HomeDivider"] = dark ? Color.FromArgb("#44624D") : Color.FromArgb("#D9C9A7");
        palette["HomeBrand"] = ImageSource.FromFile("mithaqlogo.png");
        palette["HomeIconBrush"] = new SolidColorBrush((Color)palette["AppInk"]);
        if (_palette is not null) app.Resources.MergedDictionaries.Remove(_palette);
        app.Resources.MergedDictionaries.Add(palette); _palette = palette;
        app.UserAppTheme = selection == 0 ? AppTheme.Unspecified : selection == 2 ? AppTheme.Dark : AppTheme.Light;
        Ui.RefreshThemeProperties();
        StartupTrace.Write($"Appearance applied: {selection}; dark={dark}; dictionaries={app.Resources.MergedDictionaries.Count}");
        Changed?.Invoke();
    }
}
