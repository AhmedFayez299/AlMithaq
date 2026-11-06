using AlMithaq.Shared.Controls;
namespace AlMithaq.Pages;

public sealed class TasbeehDial : IDrawable
{
    public int Value { get; set; }
    public int Goal { get; set; } = 33;
    public void Draw(ICanvas canvas, RectF rect)
    {
        var x = rect.Center.X; var y = rect.Center.Y; var r = Math.Min(rect.Width, rect.Height) / 2 - 8;
        canvas.FillColor = AppAppearance.IsDark ? Color.FromArgb("#203D34") : Color.FromArgb("#EFF3E8");
        canvas.FillCircle(x, y, r);
        canvas.StrokeSize = 1; canvas.StrokeColor = Ui.Gold.WithAlpha(.5f); canvas.DrawCircle(x, y, r - 13);
        var beads = 33; var progress = Value == 0 ? 0 : Value % Math.Max(1, Goal) == 0 ? 1d : Value % Math.Max(1, Goal) / (double)Goal;
        for (var i = 0; i < beads; i++)
        {
            var a = (-90 + i * 360d / beads) * Math.PI / 180;
            canvas.FillColor = i < progress * beads ? Ui.Gold : (AppAppearance.IsDark ? Color.FromArgb("#46675A") : Color.FromArgb("#C8D8CC"));
            canvas.FillCircle(x + (float)Math.Cos(a) * (r - 5), y + (float)Math.Sin(a) * (r - 5), i % 11 == 0 ? 4.7f : 3.4f);
        }
    }
}
public static class TasbeehFeedback
{
    public static void Pulse(bool goal = false)
    {
        try
        {
            if (Vibration.Default.IsSupported) Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(goal ? 180 : 28));
            else if (HapticFeedback.Default.IsSupported) HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }
}
