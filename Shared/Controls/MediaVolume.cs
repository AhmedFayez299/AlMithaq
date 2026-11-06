namespace AlMithaq.Shared.Controls;

public static class MediaVolume
{
    public static double Current
    {
        get
        {
#if ANDROID
            var audio = Android.App.Application.Context.GetSystemService(Android.Content.Context.AudioService) as Android.Media.AudioManager;
            if (audio is not null) return audio.GetStreamVolume(Android.Media.Stream.Music) / (double)Math.Max(1, audio.GetStreamMaxVolume(Android.Media.Stream.Music));
#endif
            return Preferences.Get("audio.volume", 1d);
        }
    }
    public static void Set(double value)
    {
        value = Math.Clamp(value, 0, 1); Preferences.Set("audio.volume", value);
#if ANDROID
        var audio = Android.App.Application.Context.GetSystemService(Android.Content.Context.AudioService) as Android.Media.AudioManager;
        if (audio is not null) audio.SetStreamVolume(Android.Media.Stream.Music, (int)Math.Round(value * audio.GetStreamMaxVolume(Android.Media.Stream.Music)), 0);
#endif
    }
}
