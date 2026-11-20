using global::Android.App;
using global::Android.Graphics;
using global::Android.Views;
using Stopwatch = System.Diagnostics.Stopwatch;
using View = global::Android.Views.View;
using Canvas = global::Android.Graphics.Canvas;
using Paint = global::Android.Graphics.Paint;
using RectF = global::Android.Graphics.RectF;
using AColor = global::Android.Graphics.Color;

namespace AlMithaq.Platforms.Android;

// A small independent drawing surface: creating MAUI's native home tree must not freeze the intro.
internal sealed class AnimatedLaunchCover : Java.Lang.Object, ISurfaceHolderCallback
{
    private static AnimatedLaunchCover? _current;
    private readonly SurfaceView _surface;
    private readonly ViewGroup _decor;
    private readonly Bitmap _logo;
    private readonly Typeface _font;
    private readonly float _density;
    private readonly Stopwatch _clock = new();
    private CancellationTokenSource? _drawing;
    private Task? _renderTask;
    private volatile bool _ready;
    private bool _removed;

    private AnimatedLaunchCover(Activity activity, ViewGroup decor)
    {
        _decor = decor;
        _density = activity.Resources!.DisplayMetrics!.Density;
        _logo = BitmapFactory.DecodeResource(activity.Resources, Resource.Drawable.mithaqsplash_new)!;
        try { _font = Typeface.CreateFromAsset(activity.Assets, "ElMessiri-Bold.ttf")!; }
        catch { _font = Typeface.Create("sans-serif", TypefaceStyle.Bold)!; }
        _surface = new SurfaceView(activity) { Elevation = 1000, Clickable = true };
        _surface.SetZOrderOnTop(true);
        _surface.Holder!.AddCallback(this);
        decor.AddView(_surface, new ViewGroup.LayoutParams(-1, -1));
    }

    public static void Install(Activity activity)
    {
        if (activity.Window?.DecorView is not ViewGroup decor) return;
        _current?.Remove();
        _current = new AnimatedLaunchCover(activity, decor);
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        if (_removed) return;
        _drawing = new CancellationTokenSource();
        if (!_clock.IsRunning) _clock.Start();
        var token = _drawing.Token;
        _renderTask = Task.Run(() =>
        {
            using var paint = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Canvas? canvas = null;
                    try
                    {
                        canvas = holder.LockCanvas();
                        if (canvas is not null) Draw(canvas, paint, _clock.Elapsed.TotalSeconds);
                    }
                    finally { if (canvas is not null) holder.UnlockCanvasAndPost(canvas); }
                    if (_ready && _clock.Elapsed.TotalSeconds >= 2.8) break;
                    if (token.WaitHandle.WaitOne(16)) return;
                }
                if (!token.IsCancellationRequested) MainThread.BeginInvokeOnMainThread(Remove);
            }
            catch (Exception ex) when (token.IsCancellationRequested || _removed)
            { System.Diagnostics.Debug.WriteLine(ex); }
        });
    }

    private static float Progress(double t, double start, double duration)
    {
        var x = (float)Math.Clamp((t - start) / duration, 0, 1);
        return x * x * (3 - 2 * x);
    }

    private void Draw(Canvas canvas, Paint paint, double t)
    {
        canvas.DrawColor(AColor.Rgb(18, 63, 52));
        var cx = canvas.Width / 2f;
        var cy = canvas.Height / 2f;
        // Logo stays in the same place as the native first frame; other elements arrive around it.
        var moving = t >= 1.9;
        var pulse = moving ? (float)Math.Sin((t - 1.9) * Math.PI * 2 / 1.7) : 0;
        var logoSize = 150 * _density * (1 + pulse * .035f);
        var drift = moving ? pulse * 3 * _density : 0;
        paint.Alpha = 255;
        canvas.Save();
        canvas.Rotate(moving ? pulse * 2 : 0, cx, cy);
        using (var rect = new RectF(cx-logoSize/2, cy-logoSize/2+drift, cx+logoSize/2, cy+logoSize/2+drift))
            canvas.DrawBitmap(_logo, null, rect, paint);
        canvas.Restore();

        var ring = Progress(t, .3, .65);
        if (ring > 0)
        {
            paint.Color = AColor.Rgb(196, 163, 90); paint.Alpha = 210;
            paint.SetStyle(Paint.Style.Stroke); paint.StrokeWidth = 1.4f * _density;
            var r = 102 * _density;
            using var circle = new RectF(cx-r, cy-r, cx+r, cy+r);
            canvas.DrawArc(circle, -90, 360 * ring, false, paint);
            paint.SetStyle(Paint.Style.Fill);
        }

        var name = Progress(t, .95, .45);
        paint.SetTypeface(_font); paint.TextAlign = Paint.Align.Center;
        if (name > 0)
        {
            paint.Color = AColor.White; paint.Alpha = (int)(255 * name); paint.TextSize = 36 * _density;
            canvas.DrawText("الميثاق", cx, cy + (151 + (1-name)*10)*_density, paint);
        }
        var caption = Progress(t, 1.35, .45);
        if (caption > 0)
        {
            paint.Color = AColor.Rgb(229, 208, 162); paint.Alpha = (int)(255 * caption); paint.TextSize = Math.Min(16*_density, canvas.Width/22f);
            canvas.DrawText("لحظات تقربك... وتملأ يومك طمأنينة", cx, cy + (189+(1-caption)*8)*_density, paint);
        }
        paint.Alpha = 255;
    }

    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height) { }
    public void SurfaceDestroyed(ISurfaceHolder holder) => _drawing?.Cancel();

    public static bool RevealWhenReady(Microsoft.Maui.Controls.Window window, Page root, Page target)
    {
        if (_current is not { } cover || cover._removed) return false;
        var observer = cover._decor.ViewTreeObserver!;
        RootFrame? frame = null;
        frame = new RootFrame(() =>
        {
            if (target.Handler?.PlatformView is not View view || !view.IsAttachedToWindow || !view.IsLaidOut || view.Width <= 0 || view.Height <= 0) return;
            if (observer.IsAlive) observer.RemoveOnPreDrawListener(frame!);
            // Keep the live animation over the first rendered home frame, not a frozen screenshot.
            cover._decor.PostOnAnimation(new Java.Lang.Runnable(() => cover._ready = true));
        });
        observer.AddOnPreDrawListener(frame);
        window.Page = root;
        return true;
    }

    private void Remove()
    {
        if (_removed) return;
        _removed = true; _drawing?.Cancel();
        if (_surface.Parent is ViewGroup parent) parent.RemoveView(_surface);
        _surface.Holder?.RemoveCallback(this);
        if (ReferenceEquals(_current, this)) _current = null;
        var task = _renderTask ?? Task.CompletedTask;
        _ = task.ContinueWith(_ => { _logo.Dispose(); _font.Dispose(); _drawing?.Dispose(); }, TaskScheduler.Default);
        StartupTrace.Write($"Live startup revealed home after {_clock.ElapsedMilliseconds}ms");
    }

    private sealed class RootFrame(Action check) : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        public bool OnPreDraw() { check(); return true; }
    }
}


