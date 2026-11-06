using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;


namespace AlMithaq
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("AmiriQuran-Regular.ttf", "AmiriQuran");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("Font Awesome 6 Free-Solid-900.otf", "FontIconSolid");
                    fonts.AddFont("ElMessiri-Regular.ttf", "Almarai-Regular");
                    fonts.AddFont("ElMessiri-Bold.ttf", "Almarai-Bold");
                });

#if ANDROID
            builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ScrollView, AlMithaq.Platforms.Android.ZoomScrollViewHandler>());
#endif
            builder.Services.AddDependencies();
            ControlMappings.ControlsBackground();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
