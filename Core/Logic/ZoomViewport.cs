namespace AlMithaq.Core.Logic;

public static class ZoomViewport
{
    public static double ContentFocus(double offset, double viewportFocus, double scale) => (offset + viewportFocus) / scale;
    public static double OffsetAtFocus(double contentFocus, double viewportFocus, double scale) => contentFocus * scale - viewportFocus;
    public static double ClampOffset(double offset, double contentSize, double viewportSize)
        => Math.Clamp(offset, 0, Math.Max(0, contentSize - viewportSize));
}
