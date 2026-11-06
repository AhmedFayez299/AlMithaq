namespace AlMithaq.Core.Logic;

public sealed record ZakatAmount(decimal Basis, decimal Nisab, decimal Rate, decimal Due, bool Reached);
public sealed record LivestockDue(int Count, string Kind);
public static class ZakatCalculator
{
    public static ZakatAmount Wealth(decimal assets, decimal deductions, decimal nisab, bool lunarYear)
    {
        NonNegative(assets, deductions); if (nisab <= 0) throw new ArgumentOutOfRangeException(nameof(nisab));
        var basis = Math.Max(0, assets - deductions); var reached = basis >= nisab;
        return new(basis, nisab, .025m, reached && lunarYear ? basis * .025m : 0, reached);
    }
    public static decimal PureGold(decimal grams, decimal karat)
    {
        NonNegative(grams); if (karat <= 0 || karat > 24) throw new ArgumentOutOfRangeException(nameof(karat)); return grams * karat / 24m;
    }
    public static decimal PureSilver(decimal grams, decimal fineness)
    {
        NonNegative(grams); if (fineness <= 0 || fineness > 1000) throw new ArgumentOutOfRangeException(nameof(fineness)); return grams * fineness / 1000m;
    }
    public static ZakatAmount Harvest(decimal kg, decimal nisabKg, decimal rate)
    {
        NonNegative(kg); if (nisabKg <= 0 || rate is not (.05m or .075m or .1m)) throw new ArgumentOutOfRangeException(nameof(rate));
        var reached = kg >= nisabKg; return new(kg, nisabKg, rate, reached ? kg * rate : 0, reached);
    }
    public static decimal Fitr(int people, decimal perPerson)
    {
        if (people < 1 || perPerson <= 0) throw new ArgumentOutOfRangeException(nameof(people)); return checked(people * perPerson);
    }
    public static decimal Rikaz(decimal value) { NonNegative(value); return value * .2m; }
    public static IReadOnlyList<LivestockDue> Livestock(string kind, int count)
    {
        if (count < 0 || count > 100000) throw new ArgumentOutOfRangeException(nameof(count));
        return kind switch
        {
            "sheep" => count < 40 ? [] : [new(count <= 120 ? 1 : count <= 200 ? 2 : count < 400 ? 3 : count / 100, "شاة مستوفية للسن والسلامة")],
            "cattle" => Combine(count, 30, "تبيع أو تبيعة (سنة)", 40, "مسنّة (سنتان)"),
            "camels" => count switch
            {
                < 5 => [], < 25 => [new(count / 5, "شاة مستوفية للسن والسلامة")],
                < 36 => [new(1, "بنت مخاض (سنة)")], < 46 => [new(1, "بنت لبون (سنتان)")],
                < 61 => [new(1, "حِقّة (ثلاث سنوات)")], < 76 => [new(1, "جذعة (أربع سنوات)")],
                < 91 => [new(2, "بنت لبون (سنتان)")], < 121 => [new(2, "حِقّة (ثلاث سنوات)")],
                _ => Combine(count, 40, "بنت لبون (سنتان)", 50, "حِقّة (ثلاث سنوات)")
            },
            _ => throw new ArgumentException("نوع الأنعام غير معروف", nameof(kind))
        };
    }
    private static IReadOnlyList<LivestockDue> Combine(int total, int unitA, string nameA, int unitB, string nameB)
    {
        int best = 0, bestA = 0, bestB = 0;
        for (var b = 0; b <= total / unitB; b++)
        {
            var a = (total - b * unitB) / unitA; var used = a * unitA + b * unitB;
            if (used > best || used == best && a + b < bestA + bestB) { best = used; bestA = a; bestB = b; }
        }
        var result = new List<LivestockDue>(); if (bestA > 0) result.Add(new(bestA, nameA)); if (bestB > 0) result.Add(new(bestB, nameB)); return result;
    }
    private static void NonNegative(params decimal[] values) { if (values.Any(v => v < 0)) throw new ArgumentOutOfRangeException(nameof(values)); }
}
