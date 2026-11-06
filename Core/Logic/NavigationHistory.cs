namespace AlMithaq.Core.Logic;

public sealed class NavigationHistory
{
    private readonly Stack<string> _previous = [];
    public string Current { get; private set; } = "home";
    public void Visit(string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        if (route == Current) return;
        if (route == "home") _previous.Clear(); else _previous.Push(Current);
        Current = route;
    }
    public string Back()
    {
        Current = _previous.TryPop(out var route) ? route : "home";
        return Current;
    }
}
