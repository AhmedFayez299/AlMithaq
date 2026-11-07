#if IOS
namespace AlMithaq.Platforms.iOS;

internal static class CircularButton
{
    public static void Attach(Button button)
    {
        button.HorizontalOptions = button.VerticalOptions = LayoutOptions.Center;
        void Update()
        {
            if (button.Handler?.PlatformView is not UIKit.UIButton native) return;
            var radius = (System.Runtime.InteropServices.NFloat)(Math.Min(native.Bounds.Width, native.Bounds.Height) / 2);
            if (radius <= 0) return;
            if (native.Configuration is { } configuration)
            {
                configuration.CornerStyle = UIKit.UIButtonConfigurationCornerStyle.Fixed;
                var background = configuration.Background;
                background.CornerRadius = radius;
                configuration.Background = background;
                native.Configuration = configuration;
            }
            native.Layer.CornerRadius = radius;
            native.ClipsToBounds = true;
        }
        button.HandlerChanged += (_, _) => Update();
        button.SizeChanged += (_, _) => Update();
        button.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Button.BackgroundColor)) Update(); };
    }
}
#endif
