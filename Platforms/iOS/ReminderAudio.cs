using AVFoundation;
using Foundation;

namespace AlMithaq.Platforms.iOS;

internal static class ReminderAudio
{
    private static AVAudioPlayer? _player;

    internal static void Play(string name, string extension, double volume, double startSeconds = 0)
    {
        Stop();
        var url = NSBundle.MainBundle.GetUrlForResource(name, extension);
        if (url is null) return;
        _player = AVAudioPlayer.FromUrl(url);
        if (_player is null) return;
        _player.Volume = (float)volume;
        if (double.IsFinite(startSeconds))
            _player.CurrentTime = Math.Clamp(startSeconds, 0, Math.Max(0, _player.Duration - .1));
        _player.PrepareToPlay();
        _player.Play();
    }

    internal static void SetVolume(double volume)
    {
        if (_player is not null) _player.Volume = (float)volume;
    }

    internal static void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;
    }
}
