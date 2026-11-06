namespace AlMithaq.Shared.Controls;

// Vector ornament: no bitmap assets, and never receives taps over header controls.
public sealed class HeaderBackdrop : GraphicsView
{
    public HeaderBackdrop()
    {
        Drawable = new Ornament(); InputTransparent = true;
        HorizontalOptions = LayoutOptions.Fill; VerticalOptions = LayoutOptions.Fill;
    }
    private sealed class Ornament : IDrawable
    {
        public void Draw(ICanvas canvas, RectF bounds)
        {
            canvas.SaveState(); canvas.StrokeColor = Color.FromRgba(232, 216, 172, 35); canvas.StrokeSize = .8f;
            for (float x = -12; x < bounds.Width + 40; x += 68)
            {
                var y = bounds.Height * .56f;
                for (var i = 0; i < 2; i++)
                {
                    canvas.SaveState(); canvas.Rotate(i * 45, x, y); canvas.DrawRectangle(x - 16, y - 16, 32, 32); canvas.RestoreState();
                }
                canvas.DrawLine(x + 22, y, x + 46, y);
            }
            canvas.StrokeColor = Color.FromRgba(232, 216, 172, 48);
            canvas.DrawLine(0, bounds.Height - 1, bounds.Width, bounds.Height - 1);
            canvas.RestoreState();
        }
    }
}
