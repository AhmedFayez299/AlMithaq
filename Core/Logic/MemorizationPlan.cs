namespace AlMithaq.Core.Logic;

public enum ReviewRating { Again, Good, Easy }
public sealed record MemorizationPlan(string Id, int Surah, int StartAyah, int EndAyah, DateOnly AddedOn, DateOnly NextReviewOn,
    int Level = 0, int Sessions = 0, DateOnly? LastReviewedOn = null, ReviewRating LastRating = ReviewRating.Again, bool Memorized = false)
{
    public bool IsValid => Guid.TryParse(Id, out _) && Surah is >= 1 and <= 114 && StartAyah >= 1 && EndAyah >= StartAyah && EndAyah <= 286 && Level is >= 0 and <= 5 && Sessions >= 0;
    public bool IsDue(DateOnly today) => NextReviewOn <= today;
    public MemorizationPlan Review(DateOnly today, ReviewRating rating)
    {
        if (!IsValid || !Enum.IsDefined(rating)) throw new ArgumentOutOfRangeException(nameof(rating));
        if (LastReviewedOn is { } last && (last > today || last == today && (rating == LastRating || LastRating != ReviewRating.Again && rating != ReviewRating.Again))) return this;
        var level = rating == ReviewRating.Again ? 0 : Math.Min(5, Level + (rating == ReviewRating.Easy ? 2 : 1));
        var days = level == 0 ? 0 : new[] { 1, 3, 7, 14, 30 }[level - 1];
        return this with { Level = level, NextReviewOn = today.AddDays(days), Sessions = Sessions + 1, LastReviewedOn = today,
            LastRating = rating, Memorized = rating == ReviewRating.Again ? false : rating == ReviewRating.Easy || Memorized };
    }
}
public sealed record PracticeRequest(int Surah, int StartAyah, int EndAyah, int Repeats)
{
    public bool IsValid(int verses) => Surah is >= 1 and <= 114 && StartAyah >= 1 && EndAyah >= StartAyah && EndAyah <= verses && Repeats is 1 or 3 or 5 or 10;
}
