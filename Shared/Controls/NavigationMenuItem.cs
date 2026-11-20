namespace AlMithaq.Shared.Controls;

public sealed class NavigationMenuItem : Border
{
    private readonly Label _title, _caption, _icon, _chevron;
    private readonly Border _iconBox;
    private bool _selected;
    public NavigationMenuItem(string title, string caption, string glyph, Func<Task> navigate)
    {
        Padding = 0; StrokeThickness = .5; StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 };
        _title = Ui.Label(title, 14, true); _caption = Ui.Label(string.IsNullOrWhiteSpace(caption) ? Caption(title) : caption, 10);
        _caption.MaxLines = 1; _caption.LineBreakMode = LineBreakMode.TailTruncation; _caption.IsVisible = !string.IsNullOrWhiteSpace(_caption.Text);
        _icon = Ui.Label(glyph, 18); _icon.FontFamily = "FontIconSolid"; _icon.HorizontalTextAlignment = TextAlignment.Center; _icon.VerticalTextAlignment = TextAlignment.Center;
        _iconBox = new Border { WidthRequest = 36, HeightRequest = 36, Padding = 0, StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }, Content = _icon, VerticalOptions = LayoutOptions.Center };
        _chevron = Ui.Label("\uf104", 13); _chevron.FontFamily = "FontIconSolid"; _chevron.VerticalOptions = LayoutOptions.Center;
        var words = Ui.Stack(_title, _caption); words.Spacing = 0; words.VerticalOptions = LayoutOptions.Center;
        var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10, Padding = new Thickness(10, 6), MinimumHeightRequest = 56 };
        row.Add(_iconBox); row.Add(words, 1); row.Add(_chevron, 2);
        var hit = Ui.AsyncButton("", navigate, true); hit.BackgroundColor = Colors.Transparent; hit.Padding = 0; hit.CornerRadius = 14;
        SemanticProperties.SetDescription(hit, title + "، " + caption);
        var layers = new Grid(); layers.Add(row); layers.Add(hit); Content = layers;
#if IOS
        Loaded += (_, _) => { AppAppearance.Changed -= Render; AppAppearance.Changed += Render; Render(); };
        Unloaded += (_, _) => AppAppearance.Changed -= Render;
        Render();
#else
        AppAppearance.Changed += Render; Render();
#endif
    }
    private static string Caption(string title) => title switch
    {
        "الرئيسية" => "وردك وصلاتك في مكان واحد", "القرآن الكريم" => "قراءة وتلاوة وتدبّر", "خطة الختمة" => "ورد يومي حتى موعدك",
        "الحفظ والمراجعة" => "تدرّب وراجع تقدمك", "الأذكار والأدعية" => "طمأنينة في كل وقت", "مواقيت الصلاة" => "على موعد مع الصلاة",
        "قيام الليل" => "ورد الليلة وتذكير السحر", "اتجاه القبلة" => "اعرف وجهتك", "السبحة" => "ذكر حاضر في يومك",
        "حاسبة الزكاة" => "حساب المال والذهب والأنعام", "الإعدادات" => "المظهر والأذان والتفضيلات", "دليل استخدام الميثاق" => "شرح الميزات خطوة بخطوة", _ => "المراجع وحقوق المحتوى"
    };
    public void SetSelected(bool selected) { _selected = selected; Render(); }
    private static Color Palette(string key, Color fallback) => Application.Current?.Resources.TryGetValue(key, out var value) == true ? (Color)value : fallback;
    private void Render()
    {
        BackgroundColor = _selected ? Ui.Green : Palette("AppCard",Ui.Paper);
        Stroke = _selected ? Ui.Gold.WithAlpha(.6f) : Palette("AppStroke",Ui.Gold).WithAlpha(.5f);
        _iconBox.BackgroundColor = _selected ? Color.FromArgb("#2D8169") : Palette("AppSoft", Color.FromArgb("#E8EEE5"));
        _title.TextColor = _selected ? Colors.White : Ui.Ink;
        _caption.TextColor = _selected ? Color.FromArgb("#DEEEE4") : Palette("AppMuted", Ui.Ink);
        _icon.TextColor = _selected ? Colors.White : Palette("AppAccent", Ui.Green);
        _chevron.TextColor = _selected ? Color.FromArgb("#DEEEE4") : Palette("AppMuted", Ui.Ink);
    }
}
