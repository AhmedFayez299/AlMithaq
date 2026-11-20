namespace AlMithaq.Shared.Controls;

// Thin vector icons keep the same weight and shape in both appearances.
public sealed class HomeNavigationIcon : GraphicsView, IDrawable
{
    private readonly string _kind;
    public Color? Tint { get; set; }
    public HomeNavigationIcon(string kind)
    {
        _kind = kind; Drawable = this; InputTransparent = true;
        var weak = new WeakReference<HomeNavigationIcon>(this);
        Action? changed = null; changed = () => { if (weak.TryGetTarget(out var view)) view.Invalidate(); else AppAppearance.Changed -= changed; };
        AppAppearance.Changed += changed;
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        canvas.SaveState();
        var scale = Math.Min(bounds.Width, bounds.Height) / 26;
        canvas.Translate(bounds.X + (bounds.Width - 26 * scale) / 2, bounds.Y + (bounds.Height - 26 * scale) / 2);
        canvas.Scale(scale, scale); canvas.StrokeColor = Tint ?? Ui.Ink; canvas.FillColor = Tint ?? Ui.Ink;
        canvas.StrokeSize = 1.6f; canvas.StrokeLineCap = LineCap.Round; canvas.StrokeLineJoin = LineJoin.Round;
        if (_kind == "book")
        {
            var left = new PathF(); left.MoveTo(13, 6); left.CurveTo(9, 3, 5, 3, 2, 4); left.LineTo(2, 21); left.CurveTo(6, 20, 10, 21, 13, 23); canvas.DrawPath(left);
            var right = new PathF(); right.MoveTo(13, 6); right.CurveTo(17, 3, 21, 3, 24, 4); right.LineTo(24, 21); right.CurveTo(20, 20, 16, 21, 13, 23); canvas.DrawPath(right);
            canvas.DrawLine(13, 6, 13, 23);
            canvas.DrawLine(4, 6, 4, 18); canvas.DrawLine(22, 6, 22, 18);
            var pages = new PathF(); pages.MoveTo(4,18); pages.CurveTo(7,18,10,19,11,20); pages.MoveTo(22,18); pages.CurveTo(19,18,16,19,15,20); canvas.DrawPath(pages);
        }
        else if (_kind == "bookmark")
        {
            var mark = new PathF(); mark.MoveTo(6,3); mark.LineTo(20,3); mark.LineTo(20,24); mark.LineTo(13,19); mark.LineTo(6,24); mark.Close(); canvas.DrawPath(mark);
        }
        else if (_kind == "share")
        {
            canvas.DrawLine(7,12,19,6); canvas.DrawLine(7,14,19,20);
            canvas.DrawEllipse(2,10,6,6); canvas.DrawEllipse(18,2,6,6); canvas.DrawEllipse(18,18,6,6);
        }
        else if (_kind == "bars")
        {
            canvas.FillRoundedRectangle(3,15,4,9,1); canvas.FillRoundedRectangle(11,9,4,15,1); canvas.FillRoundedRectangle(19,3,4,21,1);
        }
        else if (_kind == "squares")
        {
            for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++) canvas.DrawRoundedRectangle(3 + x * 11, 3 + y * 11, 8, 8, 1.8f);
        }
        else if (_kind == "layers")
        {
            var top = new PathF(); top.MoveTo(13, 3); top.LineTo(24, 9); top.LineTo(13, 15); top.LineTo(2, 9); top.Close(); canvas.DrawPath(top);
            for (var y = 14; y <= 19; y += 5) { canvas.DrawLine(2, y, 13, y + 6); canvas.DrawLine(13, y + 6, 24, y); }
        }
        else if (_kind == "lantern")
        {
            canvas.DrawEllipse(10, 1, 6, 6);
            var roof = new PathF(); roof.MoveTo(5, 10); roof.LineTo(13, 5); roof.LineTo(21, 10); roof.Close(); canvas.DrawPath(roof);
            canvas.DrawRoundedRectangle(7, 10, 12, 11, 2);
            canvas.DrawLine(11, 11, 11, 20); canvas.DrawLine(15, 11, 15, 20);
            canvas.DrawLine(5, 23, 21, 23); canvas.DrawLine(13, 23, 13, 25);
        }
        canvas.RestoreState();
    }
}

// A single floral arabesque corner, without a repeating mesh.
public sealed class HomeCardOrnament : GraphicsView, IDrawable
{
    public HomeCardOrnament()
    {
        Drawable = this; InputTransparent = true;
        var weak = new WeakReference<HomeCardOrnament>(this);
        Action? changed = null; changed = () => { if (weak.TryGetTarget(out var view)) view.Invalidate(); else AppAppearance.Changed -= changed; };
        AppAppearance.Changed += changed;
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        canvas.SaveState(); canvas.ClipRectangle(bounds.X, bounds.Y, Math.Min(126,bounds.Width*.4f), Math.Min(90,bounds.Height*.58f));
        canvas.Translate(bounds.X - 5, bounds.Y - 5);
        canvas.StrokeColor = AppAppearance.IsDark ? Color.FromRgba(154,196,169,90) : Color.FromRgba(174,136,62,105);
        canvas.FillColor = AppAppearance.IsDark ? Color.FromRgba(154,196,169,18) : Color.FromRgba(194,157,84,32);
        canvas.StrokeSize = .7f;
        for (var petal = 0; petal < 7; petal++)
        {
            canvas.SaveState(); canvas.Rotate(petal * 15);
            var leaf = new PathF(); leaf.MoveTo(0,0); leaf.CurveTo(24,-15,47,-10,77,0); leaf.CurveTo(47,10,24,15,0,0); leaf.Close();
            canvas.FillPath(leaf); canvas.DrawPath(leaf); canvas.DrawLine(14,0,72,0);
            canvas.RestoreState();
        }
        for (var radius = 37; radius <= 89; radius += 26)
        {
            var star = new PathF();
            for (var i = 0; i <= 32; i++)
            {
                var angle = i * Math.PI / 16; var r = i % 2 == 0 ? radius : radius * .78;
                var x = (float)(r*Math.Cos(angle)); var y = (float)(r*Math.Sin(angle));
                if (i==0) star.MoveTo(x,y); else star.LineTo(x,y);
            }
            canvas.DrawPath(star);
        }
        canvas.RestoreState();
    }
}
