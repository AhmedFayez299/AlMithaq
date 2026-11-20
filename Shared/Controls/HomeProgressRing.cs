namespace AlMithaq.Shared.Controls;

public sealed class HomeProgressRing : GraphicsView, IDrawable
{
    private double _progress;
    public double Progress { get => _progress; set { _progress = Math.Clamp(value, 0, 1); Invalidate(); } }
    public HomeProgressRing()
    {
        Drawable = this; WidthRequest = HeightRequest = 57; InputTransparent = true;
        var weak = new WeakReference<HomeProgressRing>(this);
        Action? changed = null;
        changed = () => { if (weak.TryGetTarget(out var view)) view.Invalidate(); else AppAppearance.Changed -= changed; };
        AppAppearance.Changed += changed;
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        var side = Math.Min(bounds.Width, bounds.Height) - 8;
        var x = (bounds.Width - side) / 2; var y = (bounds.Height - side) / 2;
        canvas.StrokeSize = 4;
        canvas.StrokeColor = AppAppearance.IsDark ? Color.FromArgb("#345344") : Color.FromArgb("#E4DDC8");
        canvas.DrawEllipse(x, y, side, side);
        canvas.StrokeColor = AppAppearance.IsDark ? Color.FromArgb("#9BD3AE") : Ui.Green;
        if (_progress >= 1) canvas.DrawEllipse(x, y, side, side);
        else if (_progress > 0) canvas.DrawArc(x, y, side, side, 90, (float)(90 - _progress * 360), true, false);
        canvas.FontColor = Ui.Ink; canvas.FontSize = 15;
        canvas.DrawString($"{_progress:P0}", bounds, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
