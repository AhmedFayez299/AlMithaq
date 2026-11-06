namespace AlMithaq.Core.Logic;

public sealed record ReadingAssignment(int StartPage, int EndPage, bool Completed)
{
    public int Pages => EndPage - StartPage + 1;
}
public sealed record KhatmahPlan(int StartPage, int NextPage, DateOnly StartedOn, DateOnly TargetDate,
    DateOnly? LastCompletedOn = null, int LastStartPage = 0, int LastEndPage = 0)
{
    public bool IsValid => StartPage is >= 1 and <= 604 && NextPage >= StartPage && NextPage <= 605 && TargetDate >= StartedOn
        && (LastCompletedOn is null || LastStartPage >= StartPage && LastEndPage >= LastStartPage && LastEndPage <= 604 && NextPage == LastEndPage + 1);
    public bool IsFinished => NextPage == 605;
    public int RemainingPages => 605 - NextPage;
    public int ReadPages => NextPage - StartPage;
    public double Progress => ReadPages / (double)(605 - StartPage);
    public ReadingAssignment? Assignment(DateOnly today)
    {
        if (!IsValid) throw new ArgumentOutOfRangeException(nameof(StartPage));
        if (LastCompletedOn is { } date && date >= today) return new(LastStartPage, LastEndPage, true);
        if (IsFinished) return null;
        var days = Math.Max(1, TargetDate.DayNumber - Math.Max(today.DayNumber, StartedOn.DayNumber) + 1);
        var pages = Math.Max(1, (RemainingPages + days - 1) / days);
        return new(NextPage, Math.Min(604, NextPage + pages - 1), false);
    }
    public KhatmahPlan Complete(DateOnly today)
    {
        if (Assignment(today) is not { Completed: false } range) return this;
        return this with { NextPage = range.EndPage + 1, LastCompletedOn = today, LastStartPage = range.StartPage, LastEndPage = range.EndPage };
    }
}
