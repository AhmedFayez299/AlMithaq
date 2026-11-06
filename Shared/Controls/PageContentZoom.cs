using Microsoft.Maui.Layouts;
using AlMithaq.Core.Logic;
namespace AlMithaq.Shared.Controls;

// A measured zoom surface reserves the enlarged content's real scrollable bounds.
// Scaling only the visual would leave lower cards and buttons outside those bounds.
public static class PageContentZoom
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ScrollView, object> Attached = new();
    private static IEnumerable<ScrollView> MainScrollViews(Element root)
    {
        if (root is ScrollView scroll) { yield return scroll; yield break; }
        if (root is IVisualTreeElement tree)
            foreach (var child in tree.GetVisualChildren().OfType<Element>())
                foreach (var item in MainScrollViews(child)) yield return item;
    }
    public static void Attach(ContentPage page)
    {
        if (page is AlMithaq.Pages.QuranPage or AlMithaq.Pages.AzkarPage || page.Content is null) return;
        var scrolls = MainScrollViews(page.Content).ToArray();
        foreach (var scroll in scrolls)
        {
            if (scroll.Content is not View body || Attached.TryGetValue(scroll, out _)) continue;
            Attached.Add(scroll, new());
            body.FlowDirection = page.FlowDirection;
            scroll.FlowDirection = FlowDirection.LeftToRight;
            scroll.Content = null;
            var surface = new ZoomSurface(body, Math.Max(1, scroll.Width)) { FlowDirection = FlowDirection.LeftToRight };
            scroll.Orientation = ScrollOrientation.Vertical; scroll.Content = surface;
            scroll.SizeChanged += (_, _) => surface.SetViewportWidth(scroll.Width);

            var reset = Ui.Button("100٪", () => { }, true);
            reset.FontSize = 13; reset.WidthRequest = 72; reset.HeightRequest = 44; reset.MinimumHeightRequest = 44;
            reset.Padding = 4; reset.IsVisible = false; reset.ZIndex = 30;
            reset.HorizontalOptions = LayoutOptions.Start; reset.VerticalOptions = LayoutOptions.End; reset.Margin = new Thickness(12);
            SemanticProperties.SetDescription(reset, "العودة إلى حجم الشاشة الطبيعي");
            Grid host;
            if (page.Content is Grid existing && existing.ClassId == "choice-host") host = existing;
            else
            {
                var original = page.Content; page.Content = null; host = new Grid { ClassId = "choice-host" };
                if (original is not null) host.Add(original); page.Content = host;
            }
            host.Add(reset); Grid.SetRowSpan(reset, Math.Max(1, host.RowDefinitions.Count)); Grid.SetColumnSpan(reset, Math.Max(1, host.ColumnDefinitions.Count));
            long update = 0;
            bool pending = false;
            Point requested = Point.Zero;
            double contentX = 0, contentY = 0, focusX = 0, focusY = 0, panX = 0, panY = 0;
            async Task Move(double x, double y)
            {
                if (scroll.Handler is null) return;
                x = ZoomViewport.ClampOffset(x, surface.Width, scroll.Width);
                y = ZoomViewport.ClampOffset(y, surface.Height, scroll.Height);
                try { await scroll.ScrollToAsync(x, y, false); }
                catch (ObjectDisposedException) { }
            }
            void MoveAfterLayout(double x, double y) { requested = new Point(x, y); pending = true; update++; }
            surface.Arranged += () =>
            {
                if (!pending) return;
                var token = update;
                page.Dispatcher.Dispatch(async () =>
                {
                    if (!pending || token != update) return;
                    pending = false; await Move(requested.X, requested.Y);
                });
            };
            scroll.Unloaded += (_, _) => { update++; pending = false; };
            reset.Clicked += (_, _) =>
            {
                var y = scroll.ScrollY / surface.Zoom; _ = Move(0, scroll.ScrollY); MoveAfterLayout(0, y); scroll.Orientation = ScrollOrientation.Vertical; surface.SetZoom(1); reset.IsVisible = false;
            };
            surface.PanChanged = (status, delta) =>
            {
                if (status == GestureStatus.Started) { update++; pending = false; panX = scroll.ScrollX; panY = scroll.ScrollY; }
                else if (status == GestureStatus.Running) _ = Move(panX + delta.X, panY + delta.Y);
            };
            void Pinch(GestureStatus status, double scale, Point origin)
            {
                if (status == GestureStatus.Started)
                {
                    focusX = Math.Clamp(origin.X * scroll.Width, 0, scroll.Width);
                    focusY = Math.Clamp(origin.Y * scroll.Height, 0, scroll.Height);
                    contentX = ZoomViewport.ContentFocus(scroll.ScrollX, focusX, surface.Zoom); contentY = ZoomViewport.ContentFocus(scroll.ScrollY, focusY, surface.Zoom);
                }
                if (status != GestureStatus.Running || !double.IsFinite(scale) || scale <= 0) return;
                var next = Math.Clamp(surface.Zoom * scale, 1, 2.5);
                if (Math.Abs(next - surface.Zoom) < .0001) return;
                MoveAfterLayout(ZoomViewport.OffsetAtFocus(contentX, focusX, next), ZoomViewport.OffsetAtFocus(contentY, focusY, next));
                if (next <= 1.0001) _ = Move(0, scroll.ScrollY);
                scroll.Orientation = next > 1.0001 ? ScrollOrientation.Both : ScrollOrientation.Vertical;
                surface.SetZoom(next); reset.IsVisible = surface.Zoom > 1.01;
            }
#if ANDROID
            surface.PinchChanged = Pinch;
            void ConnectNative()
            {
                if (scroll.Handler?.PlatformView is AlMithaq.Platforms.Android.ZoomMauiScrollView native) native.Surface = surface;
            }
            scroll.HandlerChanged += (_, _) => ConnectNative(); ConnectNative();
#else
            var pinch = new PinchGestureRecognizer();
            pinch.PinchUpdated += (_, e) => Pinch(e.Status, e.Scale,
                new Point((e.ScaleOrigin.X * surface.Width - scroll.ScrollX) / Math.Max(1, scroll.Width), (e.ScaleOrigin.Y * surface.Height - scroll.ScrollY) / Math.Max(1, scroll.Height)));
            surface.GestureRecognizers.Add(pinch);
#endif
        }
    }
}

internal sealed class ZoomSurface : Microsoft.Maui.Controls.Layout
{
    internal Action<GestureStatus, double, Point>? PinchChanged { get; set; }
    internal Action<GestureStatus, Point>? PanChanged { get; set; }
    internal event Action? Arranged;
    internal View Body { get; }
    internal double ViewportWidth { get; private set; }
    internal double Zoom { get; private set; } = 1;
    public ZoomSurface(View body, double viewportWidth)
    {
        Body = body; ViewportWidth = viewportWidth; body.AnchorX = body.AnchorY = 0;
        Children.Add(body); IsClippedToBounds = false;
        HandlerChanged += (_, _) =>
        {
#if ANDROID
            if (Handler?.PlatformView is global::Android.Views.ViewGroup group) { group.SetClipChildren(false); group.SetClipToPadding(false); }
#endif
        };
    }
    public void SetViewportWidth(double width)
    {
        if (!double.IsFinite(width) || width <= 0 || Math.Abs(width - ViewportWidth) < .5) return;
        ViewportWidth = width; InvalidateMeasure();
    }
    public void SetZoom(double value)
    {
        if (!double.IsFinite(value)) return;
        Zoom = Math.Clamp(value, 1, 2.5); Body.Scale = Zoom; InvalidateMeasure();
    }
    protected override ILayoutManager CreateLayoutManager() => new ZoomLayoutManager(this);
    private sealed class ZoomLayoutManager(ZoomSurface surface) : LayoutManager(surface)
    {
        private double _width, _height;
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            _width = Math.Max(1, surface.ViewportWidth);
            _height = ((IView)surface.Body).Measure(_width, double.PositiveInfinity).Height;
            return new Size(_width * surface.Zoom, _height * surface.Zoom);
        }
        public override Size ArrangeChildren(Rect bounds)
        {
            ((IView)surface.Body).Arrange(new Rect(0, 0, _width, _height)); surface.Arranged?.Invoke(); return bounds.Size;
        }
    }
}
