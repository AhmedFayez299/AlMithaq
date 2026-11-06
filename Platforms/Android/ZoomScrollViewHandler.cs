using global::Android.Views;
using AlMithaq.Shared.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
namespace AlMithaq.Platforms.Android;

internal sealed class ZoomScrollViewHandler : ScrollViewHandler
{
    protected override MauiScrollView CreatePlatformView() => new ZoomMauiScrollView(
        new ContextThemeWrapper(Context!, Microsoft.Maui.Resource.Style.scrollViewTheme), null!, Microsoft.Maui.Resource.Attribute.scrollViewStyle)
    { ClipToOutline = true, FillViewport = true };
}

// The outer viewport receives every touch, including those over nested controls
// and empty space. Unzoomed scrolling and taps retain their normal native path.
internal sealed class ZoomMauiScrollView : MauiScrollView
{
    internal ZoomSurface? Surface { get; set; }
    private readonly ScaleGestureDetector _detector;
    private readonly ScaleListener _listener;
    private readonly float _slop, _density;
    private bool _pinching, _panning, _panEligible;
    private float _downX, _downY;
    public ZoomMauiScrollView(global::Android.Content.Context context, global::Android.Util.IAttributeSet attrs, int style) : base(context, attrs, style)
    {
        _listener = new(this); _detector = new(context, _listener) { QuickScaleEnabled = false };
        _slop = ViewConfiguration.Get(context)!.ScaledTouchSlop; _density = context.Resources!.DisplayMetrics!.Density;
    }
    private void CancelChild(MotionEvent e)
    {
        using var cancel = MotionEvent.Obtain(e)!; cancel.Action = MotionEventActions.Cancel; base.DispatchTouchEvent(cancel);
    }
    public override bool OnInterceptTouchEvent(MotionEvent? e) => _panEligible || _pinching ? false : base.OnInterceptTouchEvent(e);
    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        if (e is null) return false;
        if (Surface is not { } surface) return base.DispatchTouchEvent(e);
        if (e.ActionMasked == MotionEventActions.Down)
        {
            _pinching = _panning = false; _panEligible = surface.Zoom > 1.01;
            _downX = e.RawX; _downY = e.RawY;
            if (_panEligible) { surface.PanChanged?.Invoke(GestureStatus.Started, Point.Zero); Parent?.RequestDisallowInterceptTouchEvent(true); }
        }
        if (!_pinching && e.PointerCount > 1)
        {
            CancelChild(e); _pinching = true; _panning = false; Parent?.RequestDisallowInterceptTouchEvent(true);
        }
        _detector.OnTouchEvent(e);
        if (_pinching)
        {
            if (e.ActionMasked is MotionEventActions.Up or MotionEventActions.Cancel) FinishTouch();
            return true;
        }
        if (_panEligible && e.ActionMasked == MotionEventActions.Move)
        {
            var dx = e.RawX - _downX; var dy = e.RawY - _downY;
            if (!_panning && dx * dx + dy * dy > _slop * _slop) { CancelChild(e); _panning = true; }
            if (_panning)
            {
                surface.PanChanged?.Invoke(GestureStatus.Running, new Point(-dx / _density, -dy / _density)); return true;
            }
        }
        if (e.ActionMasked is MotionEventActions.Up or MotionEventActions.Cancel)
        {
            var handled = _panning; FinishTouch();
            if (handled) return true;
        }
        return base.DispatchTouchEvent(e);
    }
    private void FinishTouch() { _pinching = _panning = _panEligible = false; Parent?.RequestDisallowInterceptTouchEvent(false); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _detector.Dispose(); _listener.Dispose(); }
        base.Dispose(disposing);
    }
    private sealed class ScaleListener(ZoomMauiScrollView native) : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        private Point Origin(ScaleGestureDetector detector) => new(detector.FocusX / Math.Max(1, native.Width), detector.FocusY / Math.Max(1, native.Height));
        public override bool OnScaleBegin(ScaleGestureDetector detector)
        {
            native.Surface?.PinchChanged?.Invoke(GestureStatus.Started, 1, Origin(detector)); return true;
        }
        public override bool OnScale(ScaleGestureDetector detector)
        {
            var scale = detector.ScaleFactor;
            if (float.IsFinite(scale) && scale > 0) native.Surface?.PinchChanged?.Invoke(GestureStatus.Running, scale, Origin(detector));
            return true;
        }
        public override void OnScaleEnd(ScaleGestureDetector detector)
        {
            native.Surface?.PinchChanged?.Invoke(GestureStatus.Completed, 1, Origin(detector));
            StartupTrace.Write($"Page zoom viewport: {native.Surface?.Zoom:F2}");
        }
    }
}
