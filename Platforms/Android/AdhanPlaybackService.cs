using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.Media;
using global::Android.OS;
using global::AndroidX.Core.App;
using AlMithaq.Features.PrayerTimes.Services;

namespace AlMithaq.Platforms.Android;

[Service(Enabled = true, Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public sealed class AdhanPlaybackService : Service, AudioManager.IOnAudioFocusChangeListener
{
    private const string Channel = "almithaq_adhan_playback";
    private MediaPlayer? _player;
    private AudioFocusRequestClass? _focus;
    private AudioManager? _audio;
    private bool _preview;
    private static AdhanPlaybackService? _current;
    public static bool IsPlaying => _current is not null;
    public static void PlayPreview()
    {
        var context = global::Android.App.Application.Context;
        using var intent = new Intent(context, typeof(AdhanPlaybackService)); intent.PutExtra("preview", true);
        global::AndroidX.Core.Content.ContextCompat.StartForegroundService(context, intent);
    }
    public static void StopPreview() { if (_current?._preview == true) _current.StopSelf(); }
    public static void Stop() => _current?.StopSelf();
    public static void UpdateVolume() => _current?._player?.SetVolume((float)AdhanSettings.Volume, (float)AdhanSettings.Volume);
    public override IBinder? OnBind(Intent? intent) => null;
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "stop") { StopSelf(); return StartCommandResult.NotSticky; }
        _current = this; _preview = intent?.GetBooleanExtra("preview", false) == true;
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            using var channel = new NotificationChannel(Channel, "تشغيل الأذان", NotificationImportance.Low); channel.SetSound(null, null); manager.CreateNotificationChannel(channel);
        }
        using var stop = new Intent(this, typeof(AdhanPlaybackService)).SetAction("stop");
        using var stopPending = PendingIntent.GetService(this, 9001, stop, AdhanScheduler.PendingFlags);
        using var launch = PackageManager!.GetLaunchIntentForPackage(PackageName!)!;
        using var open = PendingIntent.GetActivity(this, 9002, launch, AdhanScheduler.PendingFlags);
        var builder = new NotificationCompat.Builder(this, Channel); builder.SetSmallIcon(Resource.Drawable.adhan_notification);
        builder.SetContentTitle(_preview ? "معاينة صوت الأذان" : $"حان وقت {intent?.GetStringExtra("name") ?? "الصلاة"}");
        builder.SetContentText(AdhanSettings.Voices[AdhanSettings.Voice]); builder.SetOngoing(true); builder.SetContentIntent(open);
        builder.AddAction(Resource.Drawable.adhan_notification, "إيقاف الأذان", stopPending); var notification = builder.Build()!;
        StartForeground(8099, notification);
        ReleasePlayer();
        try
        {
            _audio = (AudioManager)GetSystemService(AudioService)!;
            using var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Alarm)!.SetContentType(AudioContentType.Speech)!.Build()!;
            AudioFocusRequest focusResult;
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                _focus = new AudioFocusRequestClass.Builder(AudioFocus.GainTransient)!.SetAudioAttributes(attributes)!.SetOnAudioFocusChangeListener(this)!.Build();
                focusResult = _audio.RequestAudioFocus(_focus!);
            }
            else
            {
#pragma warning disable CS0618 // Legacy audio focus is required for API 21-25 compatibility.
                focusResult = _audio.RequestAudioFocus(this, global::Android.Media.Stream.Alarm, AudioFocus.GainTransient);
#pragma warning restore CS0618
            }
            if (focusResult != AudioFocusRequest.Granted) { StopSelf(); return StartCommandResult.NotSticky; }
            var resources = new[] { Resource.Raw.adhan_ali_mulla, Resource.Raw.adhan_haddad };
            using var file = Resources!.OpenRawResourceFd(resources[AdhanSettings.Voice])!;
            _player = new MediaPlayer(); _player.SetAudioAttributes(attributes);
            _player.SetWakeMode(this, WakeLockFlags.Partial);
            _player.SetDataSource(file.FileDescriptor, file.StartOffset, file.Length);
            _player.SetVolume((float)AdhanSettings.Volume, (float)AdhanSettings.Volume);
            _player.Prepared += (_, _) => { if (_player is not null) _player.Start(); };
            _player.Completion += (_, _) => StopSelf();
            _player.Error += (_, e) => { e.Handled = true; Preferences.Set("adhan.error", "تعذر تشغيل ملف الأذان"); StopSelf(); };
            _player.PrepareAsync();
            global::Android.Util.Log.Info("AlMithaq.Adhan", $"Playing voice={AdhanSettings.Voice} preview={_preview}");
        }
        catch (Exception ex) { global::Android.Util.Log.Error("AlMithaq.Adhan", ex.ToString()); StopSelf(); }
        return StartCommandResult.NotSticky;
    }
    public void OnAudioFocusChange(AudioFocus focusChange) { if (focusChange is AudioFocus.Loss or AudioFocus.LossTransient) StopSelf(); }
    private void ReleasePlayer()
    {
        _player?.Release(); _player?.Dispose(); _player = null;
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && _focus is not null) { _audio?.AbandonAudioFocusRequest(_focus); _focus.Dispose(); _focus = null; }
        else if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
#pragma warning disable CS0618
            _audio?.AbandonAudioFocus(this);
#pragma warning restore CS0618
        }
    }
    public override void OnDestroy() { ReleasePlayer(); if (_current == this) _current = null; StopForeground(StopForegroundFlags.Remove); base.OnDestroy(); }
}
