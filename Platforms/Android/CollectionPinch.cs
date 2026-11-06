using global::Android.Views;
using global::AndroidX.RecyclerView.Widget;

namespace AlMithaq.Platforms.Android;

// Observe the native list without replacing its one-finger touch/scroll handler.
public static class CollectionPinch
{
    public static void Attach(CollectionView view, Action<double> zoom, Action<int>? swipe = null)
    {
        RecyclerView? native = null;
        PinchListener? listener = null;
        void Disconnect()
        {
            if (native is not null && listener is not null) native.RemoveOnItemTouchListener(listener);
            listener?.Dispose(); listener = null; native = null;
        }
        void Connect()
        {
            if (view.Handler?.PlatformView is not RecyclerView recycler || ReferenceEquals(native, recycler)) return;
            Disconnect(); native = recycler;
            listener = new PinchListener(recycler.Context!, zoom, swipe); recycler.AddOnItemTouchListener(listener);
        }
        view.HandlerChanging += (_, _) => Disconnect();
        view.HandlerChanged += (_, _) => Connect();
        view.Loaded += (_, _) => Connect();
        view.Unloaded += (_, _) => Disconnect();
        Connect();
    }

    private sealed class ScaleListener(Action<double> zoom) : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        public override bool OnScaleBegin(ScaleGestureDetector detector) => true;
        public override bool OnScale(ScaleGestureDetector detector)
        {
            var factor = detector.ScaleFactor;
            if (float.IsFinite(factor) && factor > 0) zoom(factor);
            return true;
        }
    }

    private sealed class PinchListener : RecyclerView.SimpleOnItemTouchListener
    {
        private readonly ScaleListener _scale;
        private readonly ScaleGestureDetector _detector;
        private bool _pinching;
        private float _downX, _downY;
        private readonly float _swipeThreshold;
        private readonly Action<int>? _swipe;
        private long _lastTime = -1;
        private MotionEventActions _lastAction;
        public PinchListener(global::Android.Content.Context context, Action<double> zoom, Action<int>? swipe)
        {
            _swipe = swipe; _swipeThreshold = 75 * (context.Resources?.DisplayMetrics?.Density ?? 1);
            _scale = new(zoom); _detector = new(context, _scale);
            _detector.QuickScaleEnabled = false;
        }
        private void Feed(MotionEvent e)
        {
            if (_lastTime == e.EventTime && _lastAction == e.Action) return;
            _lastTime = e.EventTime; _lastAction = e.Action; _detector.OnTouchEvent(e);
        }
        public override bool OnInterceptTouchEvent(RecyclerView recycler, MotionEvent e)
        {
            Feed(e);
            if (e.ActionMasked == MotionEventActions.Down) { _pinching = false; _downX = e.GetX(); _downY = e.GetY(); }
            if (e.PointerCount > 1) { _pinching = true; recycler.Parent?.RequestDisallowInterceptTouchEvent(true); }
            if (e.ActionMasked == MotionEventActions.Up && !_pinching && _swipe is not null)
            {
                var dx = e.GetX() - _downX; var dy = e.GetY() - _downY;
                if (Math.Abs(dx) >= _swipeThreshold && Math.Abs(dx) > Math.Abs(dy) * 2)
                { var direction = dx > 0 ? 1 : -1; recycler.Post(() => _swipe(direction)); }
            }
            // Returning false for an ordinary drag preserves RecyclerView's own scrolling.
            return _pinching;
        }
        public override void OnTouchEvent(RecyclerView recycler, MotionEvent e)
        {
            Feed(e);
            if (e.ActionMasked is MotionEventActions.Up or MotionEventActions.Cancel)
            { _pinching = false; recycler.Parent?.RequestDisallowInterceptTouchEvent(false); }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { _detector.Dispose(); _scale.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
