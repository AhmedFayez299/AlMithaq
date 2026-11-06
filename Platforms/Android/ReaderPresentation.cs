using global::AndroidX.Core.View;
namespace AlMithaq.Platforms.Android;

public static class ReaderPresentation
{
    public static void SetImmersive(bool immersive)
    {
        var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
        if (window?.DecorView is not { } decor) return;
        var controller = WindowCompat.GetInsetsController(window, decor);
        if (controller is null) return;
        if (immersive)
        {
            controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            controller.Hide(WindowInsetsCompat.Type.SystemBars());
        }
        else controller.Show(WindowInsetsCompat.Type.SystemBars());
    }
}
