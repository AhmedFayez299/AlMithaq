using Microsoft.Maui.Graphics.Platform;

namespace AlMithaq.Shared.Controls;

// Draw the approved illustration regions without rasterizing UI text.
public sealed class HomeDesignArt : GraphicsView, IDrawable
{
    private static readonly Lazy<Task<Microsoft.Maui.Graphics.IImage>> Artwork = new(LoadAsync);
    public static async Task PreloadAsync() => _ = await Artwork.Value;
    private Microsoft.Maui.Graphics.IImage? _image;
    private readonly RectF _crop;
    private readonly bool _cover;
    public HomeDesignArt(RectF crop, bool cover = false)
    {
        _crop = crop; _cover = cover; Drawable = this; InputTransparent = true;
        if (Artwork.IsValueCreated && Artwork.Value.IsCompletedSuccessfully) _image = Artwork.Value.Result;
        var weak = new WeakReference<HomeDesignArt>(this);
        Action? changed = null; changed = () => { if (weak.TryGetTarget(out var view)) view.Invalidate(); else AppAppearance.Changed -= changed; };
        AppAppearance.Changed += changed;
        Loaded += async (_, _) =>
        {
            try { _image = await Artwork.Value; Invalidate(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        };
    }
    private static async Task<Microsoft.Maui.Graphics.IImage> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("home_shortcuts.png").ConfigureAwait(false);
        return PlatformImage.FromStream(stream);
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        if (_image is null || bounds.Width <= 0 || bounds.Height <= 0) return;
        var crop = new RectF(_crop.X * _image.Width / 4, _crop.Y * _image.Height / 2, _image.Width / 4, _image.Height / 2);

        var scale = _cover ? Math.Max(bounds.Width / crop.Width, bounds.Height / crop.Height) : Math.Min(bounds.Width / crop.Width, bounds.Height / crop.Height);
        canvas.SaveState();
        canvas.ClipRectangle(bounds);
        var x = bounds.X + (bounds.Width - crop.Width * scale) / 2 - crop.X * scale;
        var y = bounds.Y + (bounds.Height - crop.Height * scale) / 2 - crop.Y * scale;
        canvas.DrawImage(_image, x, y, _image.Width * scale, _image.Height * scale);
        canvas.RestoreState();
    }
}
