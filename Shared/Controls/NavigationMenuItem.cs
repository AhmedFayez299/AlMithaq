namespace AlMithaq.Shared.Controls;

public sealed class NavigationMenuItem : Border
{
    private readonly Label _title, _caption, _icon, _chevron;
    private readonly Border _iconBox;
    private bool _selected;
    public NavigationMenuItem(string title, string caption, string glyph, Func<Task> navigate)
    {
        Padding = 0; StrokeThickness = 0; StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 };
        _title = Ui.Label(title, 15, true); _caption = Ui.Label(caption, 10);
        _icon = Ui.Label(glyph, 18); _icon.FontFamily = "FontIconSolid"; _icon.HorizontalTextAlignment = TextAlignment.Center; _icon.VerticalTextAlignment = TextAlignment.Center;
        _iconBox = new Border { WidthRequest = 40, HeightRequest = 40, Padding = 0, StrokeThickness = 0, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }, Content = _icon, VerticalOptions = LayoutOptions.Center };
        _chevron = Ui.Label("\uf104", 13); _chevron.FontFamily = "FontIconSolid"; _chevron.VerticalOptions = LayoutOptions.Center;
        var words = Ui.Stack(_title, _caption); words.Spacing = 0; words.VerticalOptions = LayoutOptions.Center;
        var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10, Padding = new Thickness(10, 6), MinimumHeightRequest = 58 };
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
    public void SetSelected(bool selected) { _selected = selected; Render(); }
    private static Color Palette(string key, Color fallback) => Application.Current?.Resources.TryGetValue(key, out var value) == true ? (Color)value : fallback;
    private void Render()
    {
        BackgroundColor = _selected ? Ui.Green : Colors.Transparent;
        _iconBox.BackgroundColor = _selected ? Color.FromArgb("#2D8169") : Palette("AppSoft", Color.FromArgb("#E8EEE5"));
        _title.TextColor = _selected ? Colors.White : Ui.Ink;
        _caption.TextColor = _selected ? Color.FromArgb("#DEEEE4") : Palette("AppMuted", Ui.Ink);
        _icon.TextColor = _selected ? Colors.White : Palette("AppAccent", Ui.Green);
        _chevron.TextColor = _selected ? Color.FromArgb("#DEEEE4") : Palette("AppMuted", Ui.Ink);
    }
}
