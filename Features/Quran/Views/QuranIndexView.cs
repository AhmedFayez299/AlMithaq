using AlMithaq.Core.Models;
using AlMithaq.Core.Logic;
using AlMithaq.Shared.Controls;
namespace AlMithaq.Features.Quran.Views;

public sealed class QuranIndexView : Grid
{
    private sealed record IndexRow(string Title, string Detail, string Badge, int Page, QuranVerse Verse, int Quarter = -1);
    private sealed class IndexGroup(string title, int page, QuranVerse anchor, IEnumerable<IndexRow> rows) : List<IndexRow>(rows)
    {
        public string Title { get; } = title;
        public string Page { get; } = $"ص {page}";
        public QuranVerse Anchor { get; } = anchor;
    }
    private readonly SearchBar _search;
    public bool TryNavigateBack()
    {
        if (string.IsNullOrEmpty(_search.Text)) return false;
        _search.Text = ""; return true;
    }
    public QuranIndexView(QuranDocument document, Func<QuranVerse, int> page, IReadOnlyList<VerseBookmark> bookmarks,
        IReadOnlyList<QuarterStart> quarters, Action<QuranVerse> choose, QuranVerse? resume = null)
    {
        ClassId = "choice-host"; RowSpacing = 5; Padding = new Thickness(12, 8, 12, 0);
        for (var i = 0; i < 4; i++) RowDefinitions.Add(new(i == 3 ? GridLength.Star : GridLength.Auto));
        var all = document.Chapters.SelectMany(c => c.Verses).ToArray();
        var byKey = all.ToDictionary(v => v.Key);
        var tab = 0;
        _search = new SearchBar { Placeholder = "ابحث باسم السورة أو رقمها…", FontFamily = "Almarai-Regular", FontSize = 13, HeightRequest = 44 };
        Ui.ThemeProperty(_search, SearchBar.TextColorProperty, "AppInk");
        Ui.ThemeProperty(_search, SearchBar.PlaceholderColorProperty, "AppMuted");
        var list = new CollectionView { IsGrouped = true, SelectionMode = SelectionMode.None, VerticalOptions = LayoutOptions.Fill, ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem };
        var tabs = new Grid { ColumnSpacing = 3 };
        var buttons = new List<Button>();
        var quickPage = new PageNumberDropdown { IsVisible = false };
        quickPage.SelectedPageChanged += (_, _) => { var verse = all.FirstOrDefault(v => page(v) == quickPage.SelectedPage); if (verse is not null) choose(verse); };
        var continueReading = Ui.Button("متابعة القراءة", () => choose(resume ?? all[0]));
        continueReading.ImageSource = new FontImageSource { FontFamily = "FontIconSolid", Glyph = "\uf053", Size = 14, Color = Colors.White };
        continueReading.FlowDirection = FlowDirection.LeftToRight;
        continueReading.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8);
        continueReading.FontSize = 13; continueReading.MinimumHeightRequest = continueReading.HeightRequest = 42; continueReading.Padding = 6;
        var shortcut = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
        shortcut.Add(continueReading); shortcut.Add(quickPage, 1);
        void Render()
        {
            var query = ArabicSearch.Normalize(NumberInput.Normalize(_search.Text ?? ""));
            IEnumerable<IndexGroup> groups;
            if (tab == 0)
            {
                groups = Enumerable.Range(1, 30).Select(j =>
                {
                    var start = all.First(v => v.Juz == j);
                    var rows = document.Chapters.Where(c => c.Verses[0].Juz == j).Select(c =>
                        new IndexRow(c.Name, $"{(c.Revelation == "Meccan" ? "مكية" : c.Revelation == "Medinan" ? "مدنية" : c.Revelation)} · {c.Verses.Count} آية", c.Number.ToString(), page(c.Verses[0]), c.Verses[0])).ToList();
                    if (start.Ayah > 1) rows.Insert(0, new("متابعة " + document.Chapters[start.Surah - 1].Name, $"بداية الجزء {j} · آية {start.Ayah}", start.Surah.ToString(), page(start), start));
                    return new IndexGroup($"الجزء {j}", page(start), start, rows);
                });
            }
            else if (tab == 1)
            {
                groups = quarters.GroupBy(q => q.Juz).Select(g =>
                {
                    var first = byKey[$"{g.First().Surah}:{g.First().Ayah}"];
                    return new IndexGroup($"الجزء {g.Key} · ٨ أرباع", page(first), first, g.Select(q =>
                    {
                        var v = byKey[$"{q.Surah}:{q.Ayah}"];
                        var fraction = q.Quarter switch { 0 => "بداية الحزب", 1 => "ربع الحزب", 2 => "نصف الحزب", _ => "ثلاثة أرباع الحزب" };
                        return new IndexRow(v.Text, $"{document.Chapters[v.Surah - 1].Name} · آية {v.Ayah} · {fraction} {q.Hizb}", q.Hizb.ToString(), page(v), v, q.Quarter);
                    }));
                });
            }
            else if (tab == 2)
                groups = [new("صفحات المصحف", 1, all[0], all.GroupBy(page).OrderBy(g => g.Key).Select(g => new IndexRow($"صفحة {g.Key}", $"{document.Chapters[g.First().Surah - 1].Name} · الجزء {g.First().Juz}", g.Key.ToString(), g.Key, g.First())))];
            else
                groups = [new("علاماتي المحفوظة", 1, all[0], bookmarks.Where(b => byKey.ContainsKey($"{b.Surah}:{b.Ayah}")).Select(b =>
                {
                    var v = byKey[$"{b.Surah}:{b.Ayah}"];
                    return new IndexRow($"{document.Chapters[b.Surah - 1].Name} · آية {b.Ayah}", string.IsNullOrWhiteSpace(b.Note) ? v.Text : b.Note, "★", page(v), v);
                }))];
            list.ItemsSource = groups.Select(g => new IndexGroup(g.Title, page(g.Anchor), g.Anchor,
                    g.Where(r => query.Length == 0 || ArabicSearch.Normalize(g.Title).Contains(query, StringComparison.OrdinalIgnoreCase) || ArabicSearch.Normalize(r.Title + " " + r.Detail + " " + r.Badge + " " + r.Page).Contains(query, StringComparison.OrdinalIgnoreCase))))
                .Where(g => g.Count > 0).ToArray();
            quickPage.IsVisible = tab == 2;
            Grid.SetColumnSpan(continueReading, tab == 2 ? 1 : 2);
            foreach (var (button, index) in buttons.Select((b, i) => (b, i)))
            {
                if (index == tab) { button.BackgroundColor = Ui.Green; button.TextColor = Colors.White; }
                else { Ui.ThemeProperty(button, Button.BackgroundColorProperty, "AppSoft"); Ui.ThemeProperty(button, Button.TextColorProperty, "AppInk"); }
            }
        }
        list.GroupHeaderTemplate = new DataTemplate(() =>
        {
            var title = Ui.Label("", 13, true); title.SetBinding(Label.TextProperty, nameof(IndexGroup.Title));
            var number = Ui.Label("", 12); number.SetBinding(Label.TextProperty, nameof(IndexGroup.Page)); number.HorizontalTextAlignment = TextAlignment.End;
            var row = new Grid { Padding = new Thickness(12, 7), ColumnDefinitions = { new(GridLength.Star), new(52) } };
            Ui.ThemeProperty(row, BackgroundColorProperty, "AppSoft"); row.Add(title); row.Add(number, 1);
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => { if (row.BindingContext is IndexGroup group) choose(group.Anchor); };
            row.GestureRecognizers.Add(tap); return row;
        });
        list.ItemTemplate = new DataTemplate(() =>
        {
            var name = Ui.Label("", 15, true); name.MaxLines = 1; name.LineBreakMode = LineBreakMode.TailTruncation; name.SetBinding(Label.TextProperty, nameof(IndexRow.Title));
            var detail = Ui.Label("", 12); detail.MaxLines = 1; detail.LineBreakMode = LineBreakMode.TailTruncation; detail.SetBinding(Label.TextProperty, nameof(IndexRow.Detail)); Ui.ThemeProperty(detail, Label.TextColorProperty, "AppMuted");
            var badge = Ui.Label("", 13, true); badge.HorizontalTextAlignment = TextAlignment.Center; badge.VerticalTextAlignment = TextAlignment.Center;
            var glyph = new QuarterGlyph();
            var drawing = new GraphicsView { Drawable = glyph, InputTransparent = true };
            var weakDrawing = new WeakReference<GraphicsView>(drawing);
            Action? repaint = null; repaint = () => { if (weakDrawing.TryGetTarget(out var view)) view.Invalidate(); else AppAppearance.Changed -= repaint; };
            AppAppearance.Changed += repaint;
            var circle = new Grid { WidthRequest = 38, HeightRequest = 38, VerticalOptions = LayoutOptions.Center, InputTransparent = true }; circle.Add(drawing); circle.Add(badge);
            var number = Ui.Label("", 12); number.SetBinding(Label.TextProperty, new Binding(nameof(IndexRow.Page), stringFormat: "ص {0}")); number.VerticalTextAlignment = TextAlignment.Center; number.HorizontalTextAlignment = TextAlignment.End; Ui.ThemeProperty(number, Label.TextColorProperty, "AppMuted");
            var words = Ui.Stack(name, detail); words.Spacing = 3; words.VerticalOptions = LayoutOptions.Center;
            var row = new Grid { ColumnDefinitions = { new(40), new(GridLength.Star), new(44) }, ColumnSpacing = 9, Padding = new Thickness(8, 8), MinimumHeightRequest = 70 };
            row.Add(circle); row.Add(words, 1); row.Add(number, 2);
            row.BindingContextChanged += (_, _) =>
            {
                if (row.BindingContext is not IndexRow item) return;
                glyph.Quarter = item.Quarter; drawing.Invalidate();
                badge.Text = item.Quarter > 0 ? "" : item.Badge;
                if (item.Quarter == 0) badge.TextColor = Colors.White;
                else Ui.ThemeProperty(badge, Label.TextColorProperty, "AppInk");
                name.FontFamily = item.Quarter >= 0 ? "AmiriQuran" : "Almarai-Bold"; name.FontSize = item.Quarter >= 0 ? 20 : 15;
                SemanticProperties.SetDescription(row, item.Title + "، " + item.Detail + "، صفحة " + item.Page);
            };
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => { if (row.BindingContext is IndexRow item) choose(item.Verse); }; row.GestureRecognizers.Add(tap);
            return row;
        });
        foreach (var text in new[] { "السور", "الأجزاء", "الصفحات", "العلامات" })
        {
            var index = buttons.Count; tabs.ColumnDefinitions.Add(new(GridLength.Star));
            var button = Ui.Button(text, () => { tab = index; _search.Text = ""; _search.Placeholder = index == 0 ? "اسم السورة أو رقمها…" : index == 1 ? "الجزء أو السورة أو بداية الربع…" : index == 2 ? "رقم الصفحة…" : "ابحث في علاماتك…"; Render(); }, true);
            button.FontSize = 12; button.Padding = new Thickness(2, 4); button.MinimumHeightRequest = button.HeightRequest = 42; buttons.Add(button); tabs.Add(button, index);
        }
        list.EmptyView = Ui.Label("لا توجد نتائج أو علامات محفوظة هنا", 13);
        _search.TextChanged += (_, _) => Render();
        this.Add(tabs); this.Add(_search, 0, 1); this.Add(shortcut, 0, 2); this.Add(list, 0, 3);
        Render();
    }
    private sealed class QuarterGlyph : IDrawable
    {
        public int Quarter { get; set; } = -1;
        public void Draw(ICanvas canvas, RectF bounds)
        {
            var size = Math.Min(bounds.Width, bounds.Height) - 2; var x = (bounds.Width - size) / 2; var y = (bounds.Height - size) / 2;
            canvas.FillColor = Color.FromArgb(AppAppearance.IsDark ? "#29483D" : "#E3EDE5"); canvas.FillEllipse(x, y, size, size);
            if (Quarter < 0) return;
            canvas.FillColor = Ui.Green;
            if (Quarter == 0) { canvas.FillEllipse(x, y, size, size); return; }
            var cx = x + size / 2; var cy = y + size / 2; var radius = size / 2;
            var sector = new PathF(); sector.MoveTo(cx, cy);
            for (var i = 0; i <= 32; i++) { var angle = -Math.PI / 2 + Math.PI * 2 * (Quarter / 4d) * i / 32; sector.LineTo(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius); }
            sector.Close(); canvas.FillPath(sector);
        }
    }
}
