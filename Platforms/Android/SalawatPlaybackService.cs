using global::Android.App;
using global::Android.Content;
using global::Android.Content.PM;
using global::Android.Media;
using global::Android.OS;
using global::AndroidX.Core.App;
using AlMithaq.Features.Salawat.Services;

namespace AlMithaq.Platforms.Android;

[Service(Enabled = true, Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public sealed class SalawatPlaybackService : Service, AudioManager.IOnAudioFocusChangeListener
{
    private static SalawatPlaybackService? _current;
    private MediaPlayer? _player;
    private AudioManager? _audio;
    private AudioFocusRequestClass? _focus;
    private int? _previousAlarmVolume;
    private int _appliedAlarmVolume;
    private bool _preview;
    public static void Preview()
    {
        var context = global::Android.App.Application.Context;
        using var intent = new Intent(context, typeof(SalawatPlaybackService)); intent.PutExtra("preview", true);
        global::AndroidX.Core.Content.ContextCompat.StartForegroundService(context, intent);
    }
    public static void Stop() => _current?.StopSelf();
    public static void StopPreview() { if (_current?._preview == true) Stop(); }
    public static void UpdateVolume() => _current?._player?.SetVolume((float)SalawatSettings.Volume, (float)SalawatSettings.Volume);
    public override IBinder? OnBind(Intent? intent) => null;
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "stop") { StopSelf(); return StartCommandResult.NotSticky; }
        _current = this; _preview = intent?.GetBooleanExtra("preview", false) == true;
        using var channel = new NotificationChannel("salawat_playback", "صوت الصلاة على النبي", NotificationImportance.Low); channel.SetSound(null, null);
        ((NotificationManager)GetSystemService(NotificationService)!).CreateNotificationChannel(channel);
        using var stop = new Intent(this, typeof(SalawatPlaybackService)).SetAction("stop"); using var stopPending = PendingIntent.GetService(this, 9511, stop, AdhanScheduler.PendingFlags);
        using var builder = new NotificationCompat.Builder(this, "salawat_playback"); builder.SetSmallIcon(Resource.Drawable.notification_wird); builder.SetContentTitle(_preview ? "معاينة صوت التذكير" : "الصلاة على النبي ﷺ"); builder.SetContentText("محمد صلى الله عليه وسلم"); builder.SetOngoing(true); builder.AddAction(Resource.Drawable.notification_wird, "إيقاف", stopPending);
        StartForeground(9510, builder.Build()); ReleasePlayer();
        if (AdhanPlaybackService.IsPlaying) { StopSelf(); return StartCommandResult.NotSticky; }
        try
        {
            _audio = (AudioManager)GetSystemService(AudioService)!;
            using var attributes = new AudioAttributes.Builder().SetUsage(AudioUsageKind.Alarm)!.SetContentType(AudioContentType.Speech)!.Build()!;
            _focus = new AudioFocusRequestClass.Builder(AudioFocus.GainTransient)!.SetAudioAttributes(attributes)!.SetOnAudioFocusChangeListener(this)!.Build();
            if (_audio.RequestAudioFocus(_focus!) != AudioFocusRequest.Granted) { StopSelf(); return StartCommandResult.NotSticky; }
            _previousAlarmVolume = _audio.GetStreamVolume(global::Android.Media.Stream.Alarm);
            _appliedAlarmVolume = _audio.GetStreamMaxVolume(global::Android.Media.Stream.Alarm);
            _audio.SetStreamVolume(global::Android.Media.Stream.Alarm, _appliedAlarmVolume, (VolumeNotificationFlags)0);
            using var file = Resources!.OpenRawResourceFd(Resource.Raw.salawat_reminder)!;
            _player = new MediaPlayer(); _player.SetAudioAttributes(attributes); _player.SetWakeMode(this, WakeLockFlags.Partial);
            _player.SetDataSource(file.FileDescriptor, file.StartOffset, file.Length); _player.SetVolume((float)SalawatSettings.Volume, (float)SalawatSettings.Volume);
            _player.Prepared += (_, _) => _player?.Start(); _player.Completion += (_, _) => StopSelf();
            _player.Error += (_, e) => { e.Handled = true; Preferences.Set("salawat.error", "تعذر تشغيل صوت التذكير"); StopSelf(); };
            _player.PrepareAsync();
            global::Android.Util.Log.Info("AlMithaq.Salawat", $"Playing alarm stream; saved={_previousAlarmVolume} applied={_appliedAlarmVolume} gain={SalawatSettings.Volume:F2} preview={_preview}");
        }
        catch (Exception ex) { Preferences.Set("salawat.error", "راجع إعدادات صوت المنبّه وعدم الإزعاج"); global::Android.Util.Log.Error("AlMithaq.Salawat", ex.ToString()); StopSelf(); }
        return StartCommandResult.NotSticky;
    }
    public void OnAudioFocusChange(AudioFocus change) { if (change is AudioFocus.Loss or AudioFocus.LossTransient) StopSelf(); }
    private void ReleasePlayer()
    {
        _player?.Release(); _player?.Dispose(); _player = null;
        if (_previousAlarmVolume is { } previous && _audio is not null)
        {
            try { if (_audio.GetStreamVolume(global::Android.Media.Stream.Alarm) == _appliedAlarmVolume) _audio.SetStreamVolume(global::Android.Media.Stream.Alarm, previous, (VolumeNotificationFlags)0); }
            catch (Exception ex) { global::Android.Util.Log.Warn("AlMithaq.Salawat", ex.Message); }
            _previousAlarmVolume = null;
        }
        if (_focus is not null) { _audio?.AbandonAudioFocusRequest(_focus); _focus.Dispose(); _focus = null; }
    }
    public override void OnDestroy() { ReleasePlayer(); if (_current == this) _current = null; StopForeground(StopForegroundFlags.Remove); base.OnDestroy(); }
}
