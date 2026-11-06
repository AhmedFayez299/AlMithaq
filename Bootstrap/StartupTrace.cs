using System.Diagnostics;

namespace AlMithaq;

internal static class StartupTrace
{
    [Conditional("DEBUG")]
    public static void Write(string stage)
    {
#if ANDROID
        Android.Util.Log.Info("AlMithaq.Startup", stage);
#endif
    }
    public static void Failure(Exception exception)
    {
        Debug.WriteLine(exception);
#if ANDROID
        Android.Util.Log.Error("AlMithaq.Startup", "Startup failed: " + exception);
#endif
    }

}
