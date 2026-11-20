namespace AlMithaq.Core.Logic;

// Popup children close independently of their parent, and Back dismisses the top layer.
public sealed class OverlayStack
{
    private readonly List<Action> _layers = [];
    public int Count => _layers.Count;
    public void Push(Action close) => _layers.Add(close);
    public bool Remove(Action close) => _layers.Remove(close);
    public bool DismissTop()
    {
        if (_layers.Count == 0) return false;
        _layers[^1](); return true;
    }
}
