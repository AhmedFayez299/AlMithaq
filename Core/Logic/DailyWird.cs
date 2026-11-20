namespace AlMithaq.Core.Logic;

public sealed record DailyWird(int StartPage, int PagesPerDay, DateOnly? CompletedOn = null, DateOnly? AssignedOn = null)
{
    public bool IsValid => StartPage is >= 1 and <= 604 && PagesPerDay is >= 1 and <= 604;
    public int EndPage => Math.Min(604, StartPage + PagesPerDay - 1);
    public bool IsComplete(DateOnly today) => CompletedOn is { } date && date >= today;
    public bool IsOverdue(DateOnly today) => CompletedOn is null && AssignedOn is { } date && date < today;
    public DailyWird Next(DateOnly today) => new(EndPage == 604 ? 1 : EndPage + 1, PagesPerDay, AssignedOn: today);
    public DailyWird ForDate(DateOnly today)
    {
        if (!IsValid) throw new ArgumentOutOfRangeException(nameof(StartPage));
        if (CompletedOn is { } date && today > date) return Next(today);
        return AssignedOn is null ? this with { AssignedOn = CompletedOn ?? today } : this;
    }
    public DailyWird Complete(DateOnly today)
    {
        var current = ForDate(today);
        if (current.IsComplete(today)) return current;
        // Catch up the missed assignment, then make today's assignment available immediately.
        return current.IsOverdue(today) ? current.Next(today) : current with { CompletedOn = today };
    }
}
