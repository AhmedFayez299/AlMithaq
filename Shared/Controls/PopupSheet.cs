using Microsoft.Maui.Controls.Shapes;
using AlMithaq.Core.Logic;
namespace AlMithaq.Shared.Controls;

public static class PopupSheet
{
    private static readonly Dictionary<ContentPage, OverlayStack> Open = [];
    internal static bool Register(ContentPage page, Action close, bool allowNested = false)
    {
        if (!Open.TryGetValue(page, out var stack)) Open[page] = stack = new();
        if (stack.Count > 0 && !allowNested) return false;
        stack.Push(close); return true;
    }
    internal static int Layer(ContentPage page) => 200 + (Open.TryGetValue(page, out var stack) ? stack.Count * 20 : 0);
    internal static void Unregister(ContentPage page, Action close)
    {
        if (!Open.TryGetValue(page, out var stack)) return;
        stack.Remove(close); if (stack.Count == 0) Open.Remove(page);
    }
    public static bool Dismiss(ContentPage page) => Open.TryGetValue(page, out var stack) && stack.DismissTop();
    internal static Grid Host(ContentPage page)
    {
        if (page.Content is Grid grid) return grid;
        var original = page.Content; page.Content = null;
        var host = new Grid { ClassId = "choice-host" };
        if (original is not null) host.Add(original);
        page.Content = host; return host;
    }
    private static Grid Overlay(ContentPage page, Action close)
    {
        var overlay = new Grid { ZIndex = Layer(page), BackgroundColor = Color.FromArgb("#80071F17"), Padding = new Thickness(10, 18, 10, 10), FlowDirection = FlowDirection.RightToLeft };
        var backdrop = new Button { BackgroundColor = Colors.Transparent, Padding = 0, BorderWidth = 0 };
        backdrop.Clicked += (_, _) => close(); SemanticProperties.SetDescription(backdrop, "إغلاق النافذة"); overlay.Add(backdrop);
        return overlay;
    }
    private static Border Chrome(string title, View body, Action close)
    {
        var handle = new BoxView { WidthRequest = 34, HeightRequest = 4, HorizontalOptions = LayoutOptions.Center, CornerRadius = 2, Margin = new Thickness(0,0,0,4) };
        Ui.ThemeProperty(handle, BoxView.ColorProperty, "AppStroke");
        var heading = Ui.Label(title, 18, true); heading.VerticalOptions = LayoutOptions.Center; heading.MaxLines = 2;
        var exit = Ui.Icon("\uf00d", "إغلاق النافذة", () => { close(); return Task.CompletedTask; });
        exit.WidthRequest = exit.HeightRequest = exit.MinimumWidthRequest = exit.MinimumHeightRequest = 36; exit.FontSize = 15; exit.CornerRadius = 12;
        Ui.ThemeProperty(exit, Button.BackgroundColorProperty, "AppSoft");
        var header = new Grid { ColumnDefinitions = { new(GridLength.Star), new(36) }, ColumnSpacing = 12 };
        header.Add(heading); header.Add(exit,1);
        var content = Ui.Stack(handle, header, body); content.Spacing = 12;
        var card = Ui.Card(content); card.Padding = new Thickness(18,12,18,18); card.Margin = 0;
        card.StrokeShape = new RoundRectangle { CornerRadius = 26 }; card.StrokeThickness = .6;
        card.VerticalOptions = LayoutOptions.End; card.HorizontalOptions = LayoutOptions.Fill; card.MaximumWidthRequest = 560;
        card.Shadow = new Shadow { Brush = Colors.Black, Opacity = .18f, Radius = 20, Offset = new Point(0,6) };
        return card;
    }
    private static void Add(Grid host, Grid overlay)
    {
        Grid.SetRowSpan(overlay,Math.Max(1,host.RowDefinitions.Count)); Grid.SetColumnSpan(overlay,Math.Max(1,host.ColumnDefinitions.Count)); host.Add(overlay);
    }
    public static async Task<string?> ChooseAsync(ContentPage page, string title, params string[] options)
    { var index = await ShowAsync(page,title,options); return index is { } i ? options[i] : null; }
    public static async Task<int?> ShowAsync(ContentPage page, string title, IReadOnlyList<string> options, int selectedIndex = -1, bool selection = false)
    {
        if (page.Content is null || options.Count == 0) return null;
        var host = Host(page);
        var result = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Close() => result.TrySetResult(null);
        Register(page,Close,allowNested:true);
        Grid? overlay = null;
        try
        {
            overlay = Overlay(page,Close);
            var search = new SearchBar { Placeholder = "ابحث في القائمة…", IsVisible = selection && options.Count > 8, FontFamily = "Almarai-Regular", FontSize = 13, HeightRequest = 44 };
            Ui.ThemeProperty(search, SearchBar.TextColorProperty,"AppInk"); Ui.ThemeProperty(search, SearchBar.PlaceholderColorProperty,"AppMuted");
            var available = Math.Clamp((page.Height > 0 ? page.Height : 700) - 240,150,420);
            var list = new CollectionView { SelectionMode = SelectionMode.None, HeightRequest = Math.Min(available,options.Count * 60), EmptyView = Ui.Label("لا توجد نتائج مطابقة",13) };
            list.ItemTemplate = new DataTemplate(() =>
            {
                var text = Ui.Label("",14); text.SetBinding(Label.TextProperty,nameof(Option.Text)); text.VerticalOptions = LayoutOptions.Center;
                var icon = Ui.Label("",17); icon.FontFamily = "FontIconSolid"; icon.HorizontalTextAlignment = TextAlignment.Center; icon.VerticalTextAlignment = TextAlignment.Center; icon.TextColor = Ui.Gold;
                icon.SetBinding(Label.TextProperty,nameof(Option.Glyph));
                var row = new Grid { ColumnDefinitions = { new(GridLength.Star),new(26) },ColumnSpacing = 10,MinimumHeightRequest = 48 };
                row.Add(text); row.Add(icon,1);
                var tile = new Border { Content = row,Padding = new Thickness(12,4),Margin = new Thickness(0,3),StrokeThickness = 0,StrokeShape = new RoundRectangle { CornerRadius = 14 } };
                tile.BindingContextChanged += (_,_) =>
                {
                    if (tile.BindingContext is not Option option) return;
                    Ui.ThemeProperty(tile,Border.BackgroundColorProperty,option.Selected ? "AppSoft" : "AppCard");
                    icon.Text = option.Selected ? "\uf058" : selection ? "\uf104" : option.Glyph;
                    icon.TextColor = option.Selected ? Ui.Green : Ui.Gold;
                };
                var tap = new TapGestureRecognizer(); tap.Tapped += (_,_) => { if (tile.BindingContext is Option option) result.TrySetResult(option.Index); }; tile.GestureRecognizers.Add(tap);
                return tile;
            });
            void Filter(string query) => list.ItemsSource = options.Select((text,i) => new Option(i,text,i==selectedIndex,IconFor(text)))
                .Where(option => ArabicSearch.Normalize(option.Text).Contains(ArabicSearch.Normalize(query),StringComparison.OrdinalIgnoreCase)).ToArray();
            search.TextChanged += (_,e) => Filter(e.NewTextValue ?? ""); Filter("");
            var body = Ui.Stack(search,list); body.Spacing = 6;
            var card = Chrome(title,body,Close); overlay.Add(card);
            void Disappear(object? sender,EventArgs e) => Close();
            page.Disappearing += Disappear;
            try
            {
                Add(host,overlay);
                if (selectedIndex >= 0) page.Dispatcher.Dispatch(() => list.ScrollTo(selectedIndex,position:ScrollToPosition.Center,animate:false));
                return await result.Task;
            }
            finally { page.Disappearing -= Disappear; }
        }
        finally { if (overlay is not null) host.Remove(overlay); Unregister(page,Close); }
    }
    public static async Task<T?> PresentAsync<T>(ContentPage page,string title,Func<Action<T>,View> create) where T:class
    {
        if (page.Content is null) return null;
        var host = Host(page); var result = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Close() => result.TrySetResult(null);
        Register(page,Close,allowNested:true); Grid? overlay = null;
        try
        {
            overlay = Overlay(page,Close);
            var body = create(value => result.TrySetResult(value));
            var viewport = new ScrollView { Content = body, MaximumHeightRequest = Math.Clamp((page.Height > 0 ? page.Height : 700)-200,180,600) };
            var card = Chrome(title,viewport,Close); overlay.Add(card);
            void Disappear(object? sender,EventArgs e) => Close();
            page.Disappearing += Disappear;
            try { Add(host,overlay); return await result.Task; }
            finally { page.Disappearing -= Disappear; }
        }
        finally { if (overlay is not null) host.Remove(overlay); Unregister(page,Close); }
    }
    public static async Task AlertAsync(ContentPage page,string title,string message,string cancel)
    {
        await PresentAsync<string>(page,title,done => Ui.Stack(Ui.Label(message,14),Ui.AsyncButton(cancel,() => { done("ok"); return Task.CompletedTask; })));
    }
    public static async Task<bool> AlertAsync(ContentPage page,string title,string message,string accept,string cancel)
    {
        var answer = await PresentAsync<string>(page,title,done =>
        {
            var buttons = new Grid { ColumnDefinitions = { new(GridLength.Star),new(GridLength.Star) },ColumnSpacing = 8 };
            buttons.Add(Ui.Button(accept,()=>done("yes"))); buttons.Add(Ui.Button(cancel,()=>done("no"),true),1);
            return Ui.Stack(Ui.Label(message,14),buttons);
        });
        return answer == "yes";
    }
    public static Task<string?> PromptAsync(ContentPage page,string title,string message,string accept="حفظ",string cancel="إلغاء",string? placeholder=null,int maxLength=-1,Keyboard? keyboard=null,string initialValue="")
        => PresentAsync<string>(page,title,done =>
        {
            var entry = Ui.Input(placeholder ?? "اكتب هنا…"); entry.Text = initialValue; entry.Keyboard = keyboard ?? Keyboard.Default;
            if (maxLength > 0) entry.MaxLength = maxLength;
            var buttons = new Grid { ColumnDefinitions = { new(GridLength.Star),new(GridLength.Star) },ColumnSpacing = 8 };
            buttons.Add(Ui.Button(accept,()=>{ entry.Unfocus(); done(entry.Text ?? ""); })); buttons.Add(Ui.Button(cancel,()=>Dismiss(page),true),1);
            return Ui.Stack(Ui.Label(message,13),entry,buttons);
        });
    private static string IconFor(string text) => text switch
    {
        var t when t.Contains("استماع") => "\uf04b", var t when t.Contains("تكرار") => "\uf01e",
        var t when t.Contains("تفسير") || t.Contains("المصدر") => "\uf02d", var t when t.Contains("حذف") || t.Contains("تصفير") => "\uf2ed",
        var t when t.Contains("نسخ") => "\uf0c5", var t when t.Contains("مشاركة") => "\uf1e0", var t when t.Contains("إخفاء") => "\uf06e",
        var t when t.Contains("علامة") || t.Contains("المفضلة") => "\uf02e", _ => "\uf104"
    };
    private sealed record Option(int Index,string Text,bool Selected,string Glyph);
}

