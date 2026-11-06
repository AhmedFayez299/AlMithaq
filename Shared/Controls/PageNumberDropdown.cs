namespace AlMithaq.Shared.Controls;

// An anchored overlay; opening the list never changes the surrounding layout.
public sealed class PageNumberDropdown : ContentView
{
    private readonly Button _button;
    private Action? _close;
    private int _page = 1;
    public event EventHandler? SelectedPageChanged;
    public int SelectedPage
    {
        get => _page;
        set { value = Math.Clamp(value, 1, 604); var changed = _page != value; _page = value; _button.Text = $"الصفحة {_page}  ﹀"; if (changed) SelectedPageChanged?.Invoke(this, EventArgs.Empty); }
    }
    public PageNumberDropdown()
    {
        _button = Ui.AsyncButton("الصفحة 1  ﹀", OpenAsync, true);
        _button.FontSize = 13; _button.HeightRequest = _button.MinimumHeightRequest = 44; _button.Padding = new Thickness(10, 2);
        Content = _button; MinimumHeightRequest = HeightRequest = 44;
        SemanticProperties.SetDescription(_button, "اختيار رقم صفحة المصحف من القائمة");
        Unloaded += (_, _) => _close?.Invoke();
    }
    private Task OpenAsync()
    {
        if (_close is not null) { _close(); return Task.CompletedTask; }
        Element? element = this; while (element is not null && element is not ContentPage) element = element.Parent;
        if (element is not ContentPage page || page.Content is null) return Task.CompletedTask;
        Grid host;
        if (page.Content is Grid grid && grid.ClassId == "choice-host") host = grid;
        else { var original = page.Content; page.Content = null; host = new Grid { ClassId = "choice-host" }; host.Add(original); page.Content = host; }
        var overlay = new AbsoluteLayout { ZIndex = 200, FlowDirection = FlowDirection.LeftToRight, WidthRequest = host.Width, HeightRequest = host.Height };
        var outside = new Button { BackgroundColor = Colors.Transparent, Padding = 0 };
        AbsoluteLayout.SetLayoutBounds(outside, new Rect(0, 0, 1, 1)); AbsoluteLayout.SetLayoutFlags(outside, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All); overlay.Add(outside);
        var numbers = new CollectionView { SelectionMode = SelectionMode.None, FlowDirection = FlowDirection.RightToLeft, ItemsSource = Enumerable.Range(1, 604).ToArray() };
        bool closed = false;
        void Close()
        {
            if (closed) return; closed = true;
            host.Remove(overlay); page.Disappearing -= Disappear; host.SizeChanged -= Resize;
            PopupSheet.Unregister(page, Close); _close = null;
        }
        void Disappear(object? sender, EventArgs e) => Close();
        void Resize(object? sender, EventArgs e) => Close();
        if (!PopupSheet.Register(page, Close)) return Task.CompletedTask;
        _close = Close; outside.Clicked += (_, _) => Close();
        numbers.ItemTemplate = new DataTemplate(() =>
        {
            var button = Ui.Button("", () => { }, true); button.FontSize = 13; button.Padding = 1; button.HeightRequest = button.MinimumHeightRequest = 42;
            button.SetBinding(Button.TextProperty, ".");
            button.BindingContextChanged += (_, _) => { if (button.BindingContext is int number) { button.BackgroundColor = number == _page ? Ui.Green : Ui.Paper; button.TextColor = number == _page ? Colors.White : Ui.Ink; } };
            button.Clicked += (_, _) => { if (button.BindingContext is int number) { Close(); SelectedPage = number; } };
            return button;
        });
        var bounds = AnchorBounds(host);
        var width = Math.Min(Math.Max(216, bounds.Width), Math.Min(320, Math.Max(120, host.Width - 16)));
        var height = Math.Min(244, Math.Max(100, host.Height - 16));
        var x = Math.Clamp(bounds.Right - width, 8, Math.Max(8, host.Width - width - 8));
        var y = bounds.Bottom + 4;
        if (y + height > host.Height - 8) y = Math.Max(8, bounds.Top - height - 4);
        numbers.HeightRequest = height - 12; numbers.ItemsLayout = new GridItemsLayout(Math.Clamp((int)(width / 52), 3, 6), ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing = 3, VerticalItemSpacing = 3 };
        var menu = Ui.Card(numbers); menu.Padding = 6; menu.Margin = 0; menu.Shadow = new Shadow { Brush = Colors.Black, Opacity = .2f, Radius = 12, Offset = new Point(0, 4) };
        menu.FlowDirection = FlowDirection.RightToLeft;
        AbsoluteLayout.SetLayoutBounds(menu, new Rect(x, y, width, height)); overlay.Add(menu);
        Grid.SetRowSpan(overlay, Math.Max(1, host.RowDefinitions.Count)); Grid.SetColumnSpan(overlay, Math.Max(1, host.ColumnDefinitions.Count));
        page.Disappearing += Disappear; host.SizeChanged += Resize; host.Add(overlay);
        Dispatcher.Dispatch(() => { if (!closed) numbers.ScrollTo(_page - 1, position: ScrollToPosition.Start, animate: false); });
        return Task.CompletedTask;
    }
    private Rect AnchorBounds(Grid host)
    {
#if ANDROID
        if (_button.Handler?.PlatformView is global::Android.Views.View native && host.Handler?.PlatformView is global::Android.Views.View root)
        {
            using var visible = new global::Android.Graphics.Rect(); native.GetGlobalVisibleRect(visible);
            var origin = new int[2]; root.GetLocationOnScreen(origin);
            var position = new int[2]; native.GetLocationOnScreen(position);
            var density = DeviceDisplay.Current.MainDisplayInfo.Density;
            return new Rect((position[0] - origin[0]) / density, (position[1] - origin[1]) / density, visible.Width() / density, visible.Height() / density);
        }
#endif
        double x = 0, y = 0; Element? current = this;
        while (current is VisualElement view && current != host) { x += view.X + view.TranslationX; y += view.Y + view.TranslationY; current = view.Parent; }
        return new Rect(x, y, Width, Height);
    }
}
