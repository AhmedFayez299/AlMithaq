namespace AlMithaq.Core.Logic;

// A completion belongs to one source and may be consumed only once.
public sealed class PlaybackGate
{
    private long _generation;
    private bool _played, _paused, _completed;
    public long Begin() { _generation++; _played = _paused = _completed = false; return _generation; }
    public bool IsCurrent(long generation) => generation == _generation;
    public void Playing(long generation) { if (IsCurrent(generation) && !_completed) { _played = true; _paused = false; } }
    public void Pause() => _paused = true;
    public void Resume() => _paused = false;
    public void Stop() { _generation++; _played = false; _paused = false; _completed = true; }
    public bool TryComplete(long generation)
    {
        if (!IsCurrent(generation) || !_played || _paused || _completed) return false;
        _completed = true; return true;
    }
}
