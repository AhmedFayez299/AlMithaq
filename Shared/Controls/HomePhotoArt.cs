using Microsoft.Maui.Graphics.Platform;
namespace AlMithaq.Shared.Controls;

public sealed class HomePhotoArt : GraphicsView, IDrawable
{
    private static readonly Lazy<Task<Microsoft.Maui.Graphics.IImage>> Photo = new(LoadAsync);
    private Microsoft.Maui.Graphics.IImage? _image;
    public static async Task PreloadAsync() => _ = await Photo.Value;
    public HomePhotoArt()
    {
        Drawable = this; InputTransparent = true;
        if (Photo.IsValueCreated && Photo.Value.IsCompletedSuccessfully) _image = Photo.Value.Result;
        Loaded += async (_,_) => { _image = await Photo.Value; Invalidate(); };
    }
    private static async Task<Microsoft.Maui.Graphics.IImage> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("home_prayer.png").ConfigureAwait(false);
        return PlatformImage.FromStream(stream);
    }
    public void Draw(ICanvas canvas, RectF bounds)
    {
        if (_image is null || bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = Math.Max(bounds.Width/_image.Width,bounds.Height/_image.Height);
        var width = _image.Width*scale; var height = _image.Height*scale;
        canvas.SaveState(); canvas.ClipRectangle(bounds);
        canvas.DrawImage(_image,bounds.X+(bounds.Width-width)/2,bounds.Y+(bounds.Height-height)/2,width,height);
        canvas.RestoreState();
    }
}
