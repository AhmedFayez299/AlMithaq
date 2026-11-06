using Android.Graphics;
using Android.Graphics.Drawables;
using Paint = Android.Graphics.Paint;

namespace AlMithaq.Platforms.Android;

public static class HeaderToolbarBackground
{
    public static void Attach(VisualElement title)
    {
        void Apply()
        {
            if (title.Handler?.PlatformView is not global::Android.Views.View view) return;
            for (global::Android.Views.View? current = view; current is not null; current = current.Parent as global::Android.Views.View)
            {
                if (current is not AndroidX.AppCompat.Widget.Toolbar toolbar) continue;
                if (toolbar.Background is not Ornament) toolbar.Background = new Ornament(toolbar.Resources?.DisplayMetrics?.Density ?? 1);
                break;
            }
        }
        title.Loaded += (_, _) => title.Dispatcher.Dispatch(Apply);
        title.HandlerChanged += (_, _) => title.Dispatcher.Dispatch(Apply);
    }
    private sealed class Ornament(float density) : Drawable
    {
        public override void Draw(global::Android.Graphics.Canvas canvas)
        {
            canvas.DrawColor(global::Android.Graphics.Color.Rgb(23, 107, 87));
            canvas.Save(); canvas.Scale(density, density);
            var width = Bounds.Width() / density; var height = Bounds.Height() / density;
            using var paint = new Paint(PaintFlags.AntiAlias);
            paint.Color = global::Android.Graphics.Color.Argb(35, 232, 216, 172); paint.StrokeWidth = .8f; paint.SetStyle(Paint.Style.Stroke);
            for (float x = -12; x < width + 40; x += 68)
            {
                var y = height * .56f;
                for (var i = 0; i < 2; i++) { canvas.Save(); canvas.Rotate(i * 45, x, y); canvas.DrawRect(x - 16, y - 16, x + 16, y + 16, paint); canvas.Restore(); }
                canvas.DrawLine(x + 22, y, x + 46, y, paint);
            }
            paint.Color = global::Android.Graphics.Color.Argb(48, 232, 216, 172); canvas.DrawLine(0, height - 1, width, height - 1, paint);
            canvas.Restore();
        }
        public override int Opacity => (int)Format.Opaque;
        public override void SetAlpha(int alpha) { }
        public override void SetColorFilter(ColorFilter? colorFilter) { }
    }
}
