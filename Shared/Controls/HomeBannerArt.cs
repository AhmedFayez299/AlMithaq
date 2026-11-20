using Microsoft.Maui.Graphics.Platform;
namespace AlMithaq.Shared.Controls;

// A softly faded mosque photograph at the left, on the natural card color.
public sealed class HomeBannerArt : GraphicsView, IDrawable
{
    private static readonly Lazy<Task<Microsoft.Maui.Graphics.IImage>> Artwork = new(LoadAsync);
    private readonly bool _night;
    public static async Task PreloadAsync() => _ = await Artwork.Value;
    private Microsoft.Maui.Graphics.IImage? _image;
    public HomeBannerArt(bool night = false)
    {
        _night = night; Drawable = this; InputTransparent = true;
        if (Artwork.IsValueCreated && Artwork.Value.IsCompletedSuccessfully) _image = Artwork.Value.Result;
        var weak = new WeakReference<HomeBannerArt>(this);
        Action? changed = null; changed = () => { if (weak.TryGetTarget(out var view)) view.Invalidate(); else AppAppearance.Changed -= changed; };
        AppAppearance.Changed += changed;
        Loaded += async (_, _) => { try { _image = await Artwork.Value; Invalidate(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); } };
    }
    private static async Task<Microsoft.Maui.Graphics.IImage> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("home_banner_scenes.png").ConfigureAwait(false); return PlatformImage.FromStream(stream);
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        if (_image is null || bounds.Width <= 0 || bounds.Height <= 0) return;
        canvas.SaveState(); canvas.ClipRectangle(bounds);
        // Two panoramic scenes share an atlas; cover each card without changing proportions.
        var rowHeight = _image.Height / 2;
        var scale = Math.Max(bounds.Width / _image.Width, bounds.Height / rowHeight);
        var x = bounds.X + (bounds.Width - _image.Width * scale) / 2;
        var y = bounds.Y + (bounds.Height - rowHeight * scale) / 2 - (_night ? rowHeight * scale : 0);
        canvas.Alpha = AppAppearance.IsDark ? .56f : .62f;
        canvas.DrawImage(_image, x, y, _image.Width * scale, _image.Height * scale);
        canvas.Alpha = 1;
        var card = Application.Current?.Resources["HomeBanner"] as Color ?? Ui.Paper;
        var transparent = new Color(card.Red, card.Green, card.Blue, 0);
        var gradient = new LinearGradientPaint(new PaintGradientStop[]
        {
            new(0, transparent), new(.25f, transparent),
            new(.60f, new Color(card.Red, card.Green, card.Blue, .65f)), new(1, card)
        }, new Point(0,0), new Point(1,0));
        canvas.SetFillPaint(gradient, bounds); canvas.FillRectangle(bounds);
        canvas.RestoreState();
    }
}
