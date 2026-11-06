using AlMithaq.Core.Logic;
namespace AlMithaq.Features.Quran.Services;

public static class DailyWirdStore
{
    private const string Key = "quran.daily-wird.v1";
    public static DailyWird? Load()
    {
        if (KhatmahStore.Linked) return KhatmahStore.DailyAssignment();
        try
        {
            var plan = JsonSerializer.Deserialize<DailyWird>(Preferences.Get(Key, "null"));
            if (plan?.IsValid != true) return null;
            var current = plan.ForDate(DateOnly.FromDateTime(DateTime.Today)); if (current != plan) Save(current); return current;
        }
        catch (JsonException) { return null; }
    }
    public static DailyWird? ForReminders()
    {
        if (KhatmahStore.Linked) return KhatmahStore.DailyAssignment();
        try { var plan = JsonSerializer.Deserialize<DailyWird>(Preferences.Get(Key, "null")); return plan?.IsValid == true ? plan.ForDate(DateOnly.FromDateTime(DateTime.Today)) : null; }
        catch (JsonException) { return null; }
    }
    public static void Save(DailyWird plan)
    {
        if (!plan.IsValid) throw new ArgumentOutOfRangeException(nameof(plan));
        Preferences.Set(Key, JsonSerializer.Serialize(plan));
        WirdReminders.Refresh();
    }
    public static bool Complete()
    {
        if (Load() is not { } plan) return false;
        var today = DateOnly.FromDateTime(DateTime.Today); if (plan.IsComplete(today)) return false;
        var completionKey = $"quran.wird-completed.{today:yyyyMMdd}";
        var firstCompletion = !Preferences.ContainsKey(completionKey);
        var finished = plan.Complete(today);
        if (KhatmahStore.Linked) KhatmahStore.CompleteToday(); else Save(finished);
        Preferences.Set(completionKey, JsonSerializer.Serialize(finished)); return firstCompletion;
    }
}
