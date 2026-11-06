namespace AlMithaq.Core.Logic;

public sealed record DailyWird(int StartPage, int PagesPerDay, DateOnly? CompletedOn = null)
{
    public bool IsValid => StartPage is >= 1 and <= 604 && PagesPerDay is >= 1 and <= 604;
    public int EndPage => Math.Min(604, StartPage + PagesPerDay - 1);
    public bool IsComplete(DateOnly today) => CompletedOn is { } date && date >= today;
    public DailyWird ForDate(DateOnly today)
    {
        if (!IsValid) throw new ArgumentOutOfRangeException(nameof(StartPage));
        return CompletedOn is { } date && today > date ? new(EndPage == 604 ? 1 : EndPage + 1, PagesPerDay) : this;
    }
    public DailyWird Complete(DateOnly today)
    {
        var current = ForDate(today); return current.IsComplete(today) ? current : current with { CompletedOn = today };
    }
}
