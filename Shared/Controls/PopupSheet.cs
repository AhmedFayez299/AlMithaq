using Microsoft.Maui.Controls.Shapes;
namespace AlMithaq.Shared.Controls;
public static class PopupSheet
{
    private static readonly Dictionary<ContentPage, Action> Open = [];
    internal static bool Register(ContentPage page, Action close) { if (Open.ContainsKey(page)) return false; Open[page] = close; return true; }
    internal static void Unregister(ContentPage page, Action close) { if (Open.TryGetValue(page, out var current) && current == close) Open.Remove(page); }
    public static bool Dismiss(ContentPage page) { if (!Open.TryGetValue(page, out var close)) return false; close(); return true; }
    public static async Task<string?> ChooseAsync(ContentPage page, string title, params string[] options)
    { var i = await ShowAsync(page, title, options); return i is { } index ? options[index] : null; }
    public static async Task<int?> ShowAsync(ContentPage page, string title, IReadOnlyList<string> options, int selectedIndex = -1, bool selection = false)
    {
        if (page.Content is null || options.Count == 0 || Open.ContainsKey(page)) return null;
        var original = page.Content;
        Grid host;
        if (original is Grid existing && existing.ClassId == "choice-host") host = existing;
        else { page.Content = null; host = new Grid { ClassId = "choice-host" }; host.Add(original); page.Content = host; }
        var result = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Close() => result.TrySetResult(null);
        Open[page] = Close;
        var shade = new Grid { BackgroundColor = Color.FromArgb("#AA061C15"), Padding = new Thickness(18, 24), ZIndex = 100, FlowDirection = FlowDirection.RightToLeft };
        var backdrop = new Button { BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
        backdrop.Clicked += (_, _) => Close(); SemanticProperties.SetDescription(backdrop, "إغلاق النافذة"); shade.Add(backdrop);
        var header = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 6 };
        var heading = Ui.Label(title, 17, true); heading.VerticalOptions = LayoutOptions.Center;
        header.Add(heading); header.Add(Ui.Icon("\uf00d", "إغلاق النافذة", () => { Close(); return Task.CompletedTask; }), 1);
        var search = new SearchBar { Placeholder = "ابحث في القائمة…", IsVisible = selection && options.Count > 8 };
        Ui.ThemeProperty(search, SearchBar.TextColorProperty, "AppInk"); Ui.ThemeProperty(search, SearchBar.PlaceholderColorProperty, "AppMuted");
        var available = Math.Max(110, Math.Min(420, (page.Height > 0 ? page.Height : 700) - 220));
        var list = new CollectionView { SelectionMode = SelectionMode.None, HeightRequest = Math.Min(available, options.Count * 60) };
        list.ItemTemplate = new DataTemplate(() =>
        {
            var text = Ui.Label("", 14); text.SetBinding(Label.TextProperty, nameof(Option.Text)); text.VerticalOptions = LayoutOptions.Center;
            var row = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Padding = new Thickness(8, 8), ColumnSpacing = 14, MinimumHeightRequest = 58 };
            row.Add(text);
            if (selection)
            {
                var dot = new Ellipse { Fill = Ui.Gold, WidthRequest = 8, HeightRequest = 8, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                dot.SetBinding(VisualElement.IsVisibleProperty, nameof(Option.Selected));
                row.Add(new Border { WidthRequest = 20, HeightRequest = 20, Padding = 0, Stroke = Ui.Gold, StrokeThickness = 1.5, BackgroundColor = Colors.Transparent, StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = dot, VerticalOptions = LayoutOptions.Center, InputTransparent = true }, 1);
            }
            else
            {
                var icon = Ui.Label("", 18); icon.FontFamily = "FontIconSolid"; icon.WidthRequest = 28; icon.VerticalOptions = LayoutOptions.Center; icon.TextColor = Ui.Gold;
                icon.SetBinding(Label.TextProperty, nameof(Option.Glyph)); row.Add(icon, 1);
            }
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => { if (row.BindingContext is Option option) result.TrySetResult(option.Index); }; row.GestureRecognizers.Add(tap);
            return row;
        });
        void Filter(string query) => list.ItemsSource = options.Select((text, i) => new Option(i, text, i == selectedIndex, IconFor(text)))
            .Where(o => Core.Logic.ArabicSearch.Normalize(o.Text).Contains(Core.Logic.ArabicSearch.Normalize(query), StringComparison.OrdinalIgnoreCase)).ToArray();
        search.TextChanged += (_, e) => Filter(e.NewTextValue ?? ""); Filter("");
        var content = Ui.Stack(header, search, list); content.Spacing = 8;
        var card = Ui.Card(content); card.VerticalOptions = LayoutOptions.Center; card.MaximumWidthRequest = 480; card.HorizontalOptions = LayoutOptions.Fill; card.Padding = 16;
        shade.Add(card); Grid.SetRowSpan(shade, Math.Max(1, host.RowDefinitions.Count)); Grid.SetColumnSpan(shade, Math.Max(1, host.ColumnDefinitions.Count));
        void Disappearing(object? sender, EventArgs e) => Close();
        page.Disappearing += Disappearing; host.Add(shade);
        try { return await result.Task; }
        finally { page.Disappearing -= Disappearing; host.Remove(shade); Open.Remove(page); }
    }
    public static async Task<T?> PresentAsync<T>(ContentPage page, string title, Func<Action<T>, View> create) where T : class
    {
        if (page.Content is null || Open.ContainsKey(page)) return null;
        var original = page.Content;
        Grid host;
        if (original is Grid grid && grid.ClassId == "choice-host") host = grid;
        else { page.Content = null; host = new Grid { ClassId = "choice-host" }; host.Add(original); page.Content = host; }
        var result = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Close() => result.TrySetResult(null);
        Open[page] = Close;
        var overlay = new Grid { ZIndex = 110, BackgroundColor = Color.FromArgb("#AA061C15"), Padding = new Thickness(12, 24), FlowDirection = FlowDirection.RightToLeft };
        var backdrop = new Button { BackgroundColor = Colors.Transparent }; backdrop.Clicked += (_, _) => Close(); overlay.Add(backdrop);
        var heading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48) } };
        var label = Ui.Label(title, 18, true); label.VerticalOptions = LayoutOptions.Center; heading.Add(label);
        heading.Add(Ui.Icon("\uf00d", "إغلاق", () => { Close(); return Task.CompletedTask; }), 1);
        var body = create(value => result.TrySetResult(value));
        var content = Ui.Stack(heading, body); content.Spacing = 6;
        var card = Ui.Card(content); card.Padding = 14; card.MaximumWidthRequest = 520; card.VerticalOptions = LayoutOptions.Center;
        overlay.Add(card); Grid.SetRowSpan(overlay, Math.Max(1, host.RowDefinitions.Count)); Grid.SetColumnSpan(overlay, Math.Max(1, host.ColumnDefinitions.Count));
        void Disappearing(object? sender, EventArgs e) => Close();
        page.Disappearing += Disappearing; host.Add(overlay);
        try { return await result.Task; }
        finally { page.Disappearing -= Disappearing; host.Remove(overlay); Open.Remove(page); }
    }
    private static string IconFor(string text) => text switch
    {
        var t when t.Contains("استماع") => "\uf04b", var t when t.Contains("تكرار") => "\uf01e",
        var t when t.Contains("تفسير") || t.Contains("المصدر") => "\uf02d", var t when t.Contains("حذف") || t.Contains("تصفير") => "\uf2ed",
        var t when t.Contains("نسخ") => "\uf0c5", var t when t.Contains("مشاركة") => "\uf1e0", var t when t.Contains("إخفاء") => "\uf06e",
        var t when t.Contains("علامة") || t.Contains("المفضلة") => "\uf02e", _ => "\uf105"
    };
    private sealed record Option(int Index, string Text, bool Selected, string Glyph);
}
