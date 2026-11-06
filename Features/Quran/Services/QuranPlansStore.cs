using AlMithaq.Core.Logic;
namespace AlMithaq.Features.Quran.Services;

public static class KhatmahStore
{
    private const string Key = "quran.khatmah.v1";
    public static bool Linked => Preferences.Get("quran.khatmah-linked", false) && Load() is not null;
    public static KhatmahPlan? Load()
    {
        try { var plan = JsonSerializer.Deserialize<KhatmahPlan>(Preferences.Get(Key, "null")); return plan?.IsValid == true ? plan : null; }
        catch (JsonException) { return null; }
    }
    public static void Save(KhatmahPlan plan)
    {
        if (!plan.IsValid) throw new ArgumentOutOfRangeException(nameof(plan));
        Preferences.Set(Key, JsonSerializer.Serialize(plan)); WirdReminders.Refresh();
    }
    public static void SetLinked(bool linked) { Preferences.Set("quran.khatmah-linked", linked); WirdReminders.Refresh(); }
    public static DailyWird? DailyAssignment()
    {
        var plan = Load(); if (plan is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (plan.Assignment(today) is { } range) return new(range.StartPage, range.Pages, range.Completed ? today : null);
        return null;
    }
    public static bool CompleteToday()
    {
        if (Load() is not { } old) return false;
        var next = old.Complete(DateOnly.FromDateTime(DateTime.Today)); if (next == old) return false;
        Save(next); return true;
    }
}

public static class MemorizationStore
{
    private const string Key = "quran.memorization.v1";
    public static List<MemorizationPlan> Load()
    {
        try { return (JsonSerializer.Deserialize<List<MemorizationPlan>>(Preferences.Get(Key, "[]")) ?? []).Where(p => p.IsValid).DistinctBy(p => p.Id).ToList(); }
        catch (JsonException) { return []; }
    }
    public static void Save(MemorizationPlan item)
    {
        if (!item.IsValid) throw new ArgumentOutOfRangeException(nameof(item));
        var items = Load(); items.RemoveAll(p => p.Id == item.Id); items.Add(item);
        Preferences.Set(Key, JsonSerializer.Serialize(items));
    }
    public static void Remove(string id) => Preferences.Set(Key, JsonSerializer.Serialize(Load().Where(p => p.Id != id).ToList()));
    public static void RequestAudio(PracticeRequest request) => Preferences.Set("quran.practice.request", JsonSerializer.Serialize(request));
}
