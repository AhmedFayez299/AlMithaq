using System.Globalization;
using System.Xml.Linq;

namespace AlMithaq.Core.Logic;

/// <summary>Bounds measured from the original title outlines; Qur'an paths remain untouched.</summary>
public static class MushafSurahFrames
{
    private static readonly (int Page, int Surah, double Left, double Top, double Right, double Bottom)[] Titles =
    [
        (1, 1, 87.985, -60.842, 150.353, -41.814),
        (2, 2, 89.852, -61.092, 147.034, -41.562),
        (50, 3, 129.501, 10.048, 211.922, 38.252),
        (77, 4, 136.489, 12.928, 201.852, 38.35),
        (106, 5, 136.254, 194.148, 206.255, 218.224),
        (128, 6, 131.792, 10.618, 209.043, 37.503),
        (151, 7, 129.238, 10.95, 208.183, 37.063),
        (177, 8, 129.233, 11.081, 208.38, 38.25),
        (187, 9, 133.225, 13.026, 205.167, 37.635),
        (208, 10, 138.006, 12.494, 203.485, 37.287),
        (221, 11, 138.189, 225.566, 199.18, 250.798),
        (235, 12, 133.614, 302.228, 204.035, 326.964),
        (249, 13, 134.738, 12.754, 202.826, 37.695),
        (255, 14, 128.425, 84.941, 208.56, 110.151),
        (262, 15, 136.631, 12.806, 205.387, 36.884),
        (267, 16, 134.499, 226.901, 202.524, 253.567),
        (282, 17, 135.866, 11.79, 206.147, 38.239),
        (293, 18, 129.675, 340.294, 207.796, 365.23),
        (305, 19, 132.911, 12.112, 205.623, 37.371),
        (312, 20, 141.435, 156.544, 200.824, 181.074),
        (322, 21, 133.54, 10.749, 208.432, 38.582),
        (332, 22, 143.608, 12.514, 198.869, 37.216),
        (342, 23, 131.163, 12.921, 209.675, 37.035),
        (350, 24, 138.162, 12.554, 204.289, 37.37),
        (359, 25, 129.409, 373.857, 207.569, 397.935),
        (367, 26, 133.189, 12.649, 205.4, 37.083),
        (377, 27, 132.838, 12.79, 204.167, 37.003),
        (385, 28, 129.879, 266.023, 207.437, 290.781),
        (396, 29, 129.843, 264.637, 213.184, 289.073),
        (404, 30, 136.252, 341.043, 206.263, 365.415),
        (411, 31, 134.214, 10.448, 202.706, 37.487),
        (415, 32, 134.051, 12.76, 204.484, 36.94),
        (418, 33, 132.376, 10.634, 209.16, 38.712),
        (428, 34, 141.99, 12.574, 200.421, 37.954),
        (434, 35, 136.292, 267.679, 204.728, 292.198),
        (440, 36, 142.438, 120.106, 198.616, 144.183),
        (446, 37, 129.674, 13.005, 211.87, 37.082),
        (453, 38, 136.587, 12.358, 201.828, 38.79),
        (458, 39, 136.951, 122.805, 204.455, 147.386),
        (467, 40, 135.031, 88.572, 203.543, 112.649),
        (477, 41, 130.834, 12.344, 206.587, 37.274),
        (483, 42, 129.789, 12.616, 207.408, 37.571),
        (489, 43, 129.725, 161.528, 207.951, 186.382),
        (496, 44, 129.971, 12.594, 211.46, 37.299),
        (499, 45, 131.818, 12.649, 206.612, 37.007),
        (502, 46, 128.67, 229.334, 212.606, 254.758),
        (507, 47, 132.635, 12.9, 204.316, 37.764),
        (511, 48, 136.957, 12.172, 201.623, 38.73),
        (515, 49, 129.883, 232.048, 207.953, 256.859),
        (518, 50, 142.258, 12.925, 200.147, 37.002),
        (520, 51, 128.379, 409.644, 213.046, 435.46),
        (523, 52, 133.819, 264.003, 204.734, 288.95),
        (526, 53, 134.195, 12.856, 208.259, 36.931),
        (528, 54, 135.855, 334.794, 206.513, 358.871),
        (531, 55, 130.861, 160.937, 205.847, 185.318),
        (534, 56, 132.123, 228.8, 210.357, 253.346),
        (537, 57, 130.561, 375.139, 206.849, 399.505),
        (542, 58, 131.137, 11.808, 211.364, 37.386),
        (545, 59, 131.941, 228.866, 206.189, 252.944),
        (549, 60, 125.202, 12.73, 212.853, 36.843),
        (551, 61, 134.195, 230.369, 203.115, 254.444),
        (553, 62, 132.905, 12.926, 205.613, 37.002),
        (554, 63, 128.197, 230.393, 212.812, 255.84),
        (556, 64, 132.491, 12.988, 208.947, 37.267),
        (558, 65, 132.836, 12.352, 208.909, 37.89),
        (560, 66, 131.407, 12.72, 209.077, 37.22),
        (562, 67, 137.635, 11.144, 203.347, 37.434),
        (564, 68, 137.244, 196.299, 205.215, 223.004),
        (566, 69, 134.396, 337.169, 207.883, 361.246),
        (568, 70, 135.071, 304.344, 207.467, 328.481),
        (570, 71, 143.544, 158.059, 197.818, 182.146),
        (572, 72, 139.539, 12.093, 201.497, 38.318),
        (574, 73, 135.291, 11.598, 206.033, 37.958),
        (575, 74, 133.267, 265.165, 205.212, 289.506),
        (577, 75, 131.186, 190.143, 207.203, 214.356),
        (578, 76, 131.418, 335.38, 209.619, 360.562),
        (580, 77, 130.202, 228.846, 211.148, 254.078),
        (582, 78, 139.839, 12.725, 202.724, 37.896),
        (583, 79, 127.999, 266.788, 209.661, 290.867),
        (585, 80, 136.251, 12.804, 201.183, 36.963),
        (586, 81, 128.921, 50.553, 213.53, 77.162),
        (587, 82, 126.758, 12.554, 211.727, 37.527),
        (587, 83, 126.953, 409.29, 210.568, 433.642),
        (589, 84, 127.429, 86.824, 210.228, 111.577),
        (590, 85, 135.499, 52.307, 206.213, 76.383),
        (591, 86, 130.451, 12.573, 207.188, 37.032),
        (591, 87, 132.843, 335.846, 204.456, 363.73),
        (592, 88, 128.959, 156.631, 213.465, 180.709),
        (593, 89, 135.546, 85.691, 202.819, 109.767),
        (594, 90, 134.267, 194.616, 206.659, 220.324),
        (595, 91, 130.982, 51.617, 205.923, 76.017),
        (595, 92, 136.647, 371.337, 200.98, 400.319),
        (596, 93, 134.207, 191.775, 207.669, 216.859),
        (596, 94, 137.25, 441.992, 204.944, 466.068),
        (597, 95, 136.091, 88.374, 201.159, 112.844),
        (597, 96, 134.278, 299.934, 204.114, 326.149),
        (598, 97, 132.695, 122.856, 209.843, 146.959),
        (598, 98, 136.284, 304.192, 206.148, 330.264),
        (599, 99, 132.067, 192.935, 206.075, 220.997),
        (599, 100, 124.741, 409.551, 213.215, 433.846),
        (600, 101, 131.069, 121.729, 211.316, 145.807),
        (600, 102, 132.986, 369.683, 209.492, 393.758),
        (601, 103, 130.775, 12.756, 206.207, 37.162),
        (601, 104, 132.401, 158.363, 205.167, 182.441),
        (601, 105, 130.778, 375.539, 206.316, 399.615),
        (602, 106, 136.516, 12.598, 204.903, 37.018),
        (602, 107, 128.849, 196.79, 212.44, 222.35),
        (602, 108, 134.591, 408.912, 207.852, 434.257),
        (603, 109, 123.516, 11.049, 213.38, 37.011),
        (603, 110, 133.985, 194.107, 204.327, 218.929),
        (603, 111, 133.991, 373.575, 204.068, 397.653),
        (604, 112, 127.405, 10.238, 215.05, 37.778),
        (604, 113, 136.31, 156.611, 206.149, 183.818),
        (604, 114, 131.788, 339.44, 209.124, 363.515),
    ];

    public static IEnumerable<XElement> Create(int page, XElement root)
    {
        var box = ((string?)root.Attribute("viewBox") ?? "0 0 345 550").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
        XNamespace ns = root.Name.Namespace;
        var verseBands = root.Elements().Where(e => (string?)e.Attribute("class") == "ayahPolygon").Select(e =>
        {
            var coordinates = System.Text.RegularExpressions.Regex.Matches((string?)e.Attribute("d") ?? "", @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?").Select(m => double.Parse(m.Value, CultureInfo.InvariantCulture)).Where((v, i) => i % 2 == 1).ToArray();
            return (Top: coordinates.Min(), Bottom: coordinates.Max());
        }).ToArray();
        foreach (var title in Titles.Where(t => t.Page == page))
        {
            var x = box[0] + box[2] * .07; var right = box[0] + box[2] * .93;
            var top = title.Top - 2.3; var bottom = title.Bottom + 2.3;
            // At-Tawbah has no basmala and a tighter first-line gap; keep the frame outside verse hit areas.
            foreach (var band in verseBands)
            {
                if (band.Bottom <= title.Top) top = Math.Max(top, band.Bottom + .5);
                if (band.Top >= title.Bottom) bottom = Math.Min(bottom, band.Top - .5);
            }
            var mid = (top + bottom) / 2;
            string F(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
            string P(params double[] n) => string.Join(" ", n.Select(F));
            var group = new XElement(ns + "g", new XAttribute("class", "surah-frame"), new XAttribute("data-surah", title.Surah), new XAttribute("pointer-events", "none"));
            const string ink = "var(--surah-ornament,#24735e)";
            const string gold = "var(--surah-gold,#ae894c)";
            foreach (var inset in new[] { 0d, 1.35 })
                group.Add(new XElement(ns + "rect", new XAttribute("x", F(x + inset)), new XAttribute("y", F(top + inset)), new XAttribute("width", F(right - x - 2 * inset)), new XAttribute("height", F(bottom - top - 2 * inset)), new XAttribute("fill", "none"), new XAttribute("stroke", inset == 0 ? gold : ink), new XAttribute("stroke-width", inset == 0 ? ".42" : ".18")));
            // Each side fits the original title bounds independently. Long titles keep their original scale.
            foreach (var side in new[] { (Origin: x + 3, End: title.Left - 8, Mirror: false), (Origin: right - 3, End: title.Right + 8, Mirror: true) })
            {
                var width = Math.Abs(side.End - side.Origin);
                var panel = new XElement(ns + "g", new XAttribute("class", "surah-spray"), new XAttribute("transform", $"translate({P(side.Origin, top + 2)}) scale({P((side.Mirror ? -1 : 1) * width / 230, (bottom - top - 4) / 96)})"));
                panel.Add(Spray(ns, ink, gold), new XElement(ns + "g", new XAttribute("transform", "translate(0 96) scale(1 -1)"), Spray(ns, ink, gold)));
                group.Add(panel);
            }
            var left = title.Left - 5; var edge = title.Right + 5;
            var h = bottom - top; var a = top + h * .22; var b = bottom - h * .22;
            var outline = $"M {P(left - 5, top + 1.35)} Q {P(left, top + 1.35, left, a)} Q {P(left + 2, mid - h * .12, left, mid)} Q {P(left + 2, mid + h * .12, left, b)} Q {P(left, bottom - 1.35, left - 5, bottom - 1.35)} M {P(edge + 5, top + 1.35)} Q {P(edge, top + 1.35, edge, a)} Q {P(edge - 2, mid - h * .12, edge, mid)} Q {P(edge - 2, mid + h * .12, edge, b)} Q {P(edge, bottom - 1.35, edge + 5, bottom - 1.35)}";
            group.Add(new XElement(ns + "path", new XAttribute("d", outline), new XAttribute("stroke", gold), new XAttribute("stroke-width", ".27"), new XAttribute("fill", "none")));
            yield return group;
        }
    }

    private static XElement Spray(XNamespace ns, string ink, string gold)
    {
        var g = new XElement(ns + "g", new XAttribute("stroke", ink), new XAttribute("fill", ink));
        g.Add(new XElement(ns + "path", new XAttribute("d", "M0 48 C29 47 20 9 51 11 C77 13 69 40 51 32 C40 26 50 17 59 21 M47 34 C82 51 80 1 108 10 C134 18 112 42 100 30 M98 31 C132 47 131 5 160 13 C184 20 167 43 155 31 M154 33 C185 48 187 11 208 20 C229 31 212 45 203 36 M13 46 C41 26 48 65 73 47 S119 28 141 46 S182 65 207 46 L229 48"), new XAttribute("stroke-width", "1"), new XAttribute("fill", "none")));
        foreach (var (x, y, angle) in new[] { (23,29,-25), (35,13,15), (77,24,-30), (90,11,20), (130,24,-25), (143,13,15), (179,28,-25), (194,20,10) })
            g.Add(new XElement(ns + "path", new XAttribute("transform", $"translate({x} {y}) rotate({angle})"), new XAttribute("d", "M0 0 C-6 -3 -7 -9 -2 -13 C4 -8 5 -3 0 0Z"), new XAttribute("stroke", "none")));
        foreach (var (x, scale) in new[] { (9, .58), (115, .8) })
        {
            var flower = new XElement(ns + "g", new XAttribute("transform", FormattableString.Invariant($"translate({x} 48) scale({scale})")), new XAttribute("stroke", gold), new XAttribute("stroke-width", ".45"));
            for (var angle = 0; angle < 360; angle += 45)
                flower.Add(new XElement(ns + "path", new XAttribute("transform", $"rotate({angle})"), new XAttribute("d", "M0 0 C-6 -4 -6 -11 0 -14 C6 -11 6 -4 0 0Z")));
            flower.Add(new XElement(ns + "circle", new XAttribute("r", "2.4"), new XAttribute("fill", gold), new XAttribute("stroke", "none")));
            g.Add(flower);
        }
        return g;
    }
}
