using System.Xml.Linq;
namespace AlMithaq.Core.Logic;
public sealed record QuarterStart(int Index, int Surah, int Ayah)
{
    public int Juz => (Index - 1) / 8 + 1;
    public int Hizb => (Index - 1) / 4 + 1;
    public int Quarter => (Index - 1) % 4;
}
public static class QuranPartitions
{
    public static IReadOnlyList<QuarterStart> Read(Stream input)
    {
        using var reader = System.Xml.XmlReader.Create(input, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null });
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing Quran metadata");
        var rows = root.Element("hizbs")?.Elements("quarter").Select(x => new QuarterStart((int)x.Attribute("index")!, (int)x.Attribute("sura")!, (int)x.Attribute("aya")!)).ToArray() ?? [];
        if (rows.Length != 240 || rows.Where((x, i) => x.Index != i + 1 || x.Surah is < 1 or > 114 || x.Ayah is < 1 or > 286).Any()) throw new InvalidDataException("Invalid Quran quarter metadata");
        return rows;
    }
}
