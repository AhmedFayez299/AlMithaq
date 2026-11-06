using AlMithaq.Features.Azkar.Services;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;

namespace AlMithaq.Pages;

public partial class AzkarPage : ContentPage
{
    private readonly AzkarRepository _repository;
    private readonly ChoicePicker _category = new() { Title = "قسم الأذكار", ItemsSource = new[] { "أذكار الصباح", "أذكار المساء", "أذكار النوم", "بعد الصلاة", "المفضلة" }, SelectedIndex = 0 };
    private readonly SearchBar _search = new() { Placeholder = "ابحث في أذكار القسم", FontFamily = "Almarai-Regular", FontSize = 13 };
    private readonly Label _summary = Ui.Label("", 11);
    private readonly ProgressBar _progress = new() { ProgressColor = Ui.Green, HeightRequest = 4 };
    private readonly Switch _hide = new();
    private readonly CollectionView _list = new() { SelectionMode = SelectionMode.None };
    private List<Dhikr> _data = [];
    private bool _active, _duas;
    private sealed record Section(string Title, int Type = 0, int Chapter = 0, bool All = false, bool Favorite = false);
    private List<Section> _sections = [];
    private readonly Button _azkarTab, _duaTab;
    public AzkarPage(AzkarRepository repository)
    {
        InitializeComponent(); Ui.Page(this, "الأذكار والأدعية"); _repository = repository;
        var font = new Slider { Minimum = 16, Maximum = 28, Value = Math.Clamp(Preferences.Get("azkar.font", 18d), 16, 28), HorizontalOptions = LayoutOptions.Fill };
        Ui.ThemeProperty(_search, SearchBar.TextColorProperty, "AppInk"); Ui.ThemeProperty(_search, SearchBar.PlaceholderColorProperty, "AppMuted");
        Ui.ThemeProperty(_summary, Label.TextColorProperty, "AppMuted");
        _category.UseCompactLayout(); _search.IsVisible = false; _search.HeightRequest = 44;
        font.ValueChanged += (_, _) => Preferences.Set("azkar.font", font.Value);
        _list.ItemTemplate = new DataTemplate(() =>
        {
            var text = Ui.Label("", 18); text.LineHeight = 1.3; text.SetBinding(Label.TextProperty, "Item.Text");
            text.SetBinding(Label.FontSizeProperty, new Binding(nameof(Slider.Value), source: font));
            var chapter = Ui.Label("", 12, true); chapter.SetBinding(Label.TextProperty, "Item.ChapterTitle");
            var source = Ui.Label("", 12); source.SetBinding(Label.TextProperty, nameof(DhikrRow.SourceCaption)); Ui.ThemeProperty(source, Label.TextColorProperty, "AppMuted");
            var count = Ui.Label("", 12, true); count.SetBinding(Label.TextProperty, nameof(DhikrRow.Status)); count.VerticalOptions = LayoutOptions.Center; count.FlowDirection = FlowDirection.LeftToRight;
            var button = Ui.Button("قرأت  ✓", () =>
            {
                if (text.BindingContext is not DhikrRow row || row.Done >= row.Item.Repeat) return;
                row.Done++; Preferences.Set(row.Key, row.Done); UpdateSummary(); if (_hide.IsToggled && row.Done == row.Item.Repeat) Filter();
            });
            button.SetBinding(Button.IsEnabledProperty, nameof(DhikrRow.CanCount));
            var details = Ui.Icon("\uf142", "خيارات الذكر والمفضلة", async () =>
            {
                if (text.BindingContext is not DhikrRow row) return;
                var choice = await PopupSheet.ChooseAsync(this, _duas ? "خيارات الدعاء" : "خيارات الذكر", "المصدر والفضل", "تبديل المفضلة", "نسخ الذكر", "تصفير هذا الذكر");
                switch (choice)
                {
                    case "المصدر والفضل": await DisplayAlertAsync("المصدر كما ورد في البيانات", $"{row.Item.Source}\n\n{row.Item.Benefit}", "إغلاق"); break;
                    case "تبديل المفضلة": Preferences.Set($"azkar.favorite.{row.Item.Id}", !Preferences.Get($"azkar.favorite.{row.Item.Id}", false)); Filter(); break;
                    case "نسخ الذكر": await Clipboard.Default.SetTextAsync(row.Item.Text + "\n" + row.Item.Source); break;
                    case "تصفير هذا الذكر": row.Done = 0; Preferences.Set(row.Key, 0); UpdateSummary(); break;
                }
            });
            button.FontSize = 13; button.MinimumHeightRequest = button.HeightRequest = 44; button.Padding = new Thickness(10, 2);
            var footer = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(48) }, ColumnSpacing = 8 };
            footer.Add(button); footer.Add(count, 1); footer.Add(details, 2);
            var body = Ui.Stack(chapter, text, source, footer); body.Spacing = 8;
            var card = Ui.Card(body); card.Padding = 14; card.Margin = new Thickness(12, 4); return card;
        });
        async Task ShowSettingsAsync()
        {
            await PopupSheet.PresentAsync<string>(this, "عرض الأذكار", done =>
            {
                var size = new Slider { Minimum = font.Minimum, Maximum = font.Maximum, Value = font.Value };
                size.ValueChanged += (_, e) => font.Value = e.NewValue;
                var completed = new Switch { IsToggled = _hide.IsToggled };
                completed.Toggled += (_, e) => _hide.IsToggled = e.Value;
                var body = Ui.Stack(Ui.Label("حجم النص", 13, true), size,
                    Ui.Row(Ui.Label("إخفاء المكتمل", 13), completed),
                    Ui.AsyncButton("بدء جلسة جديدة للقسم", ResetAsync, true), Ui.Button("تم", () => done("done")));
                body.Spacing = 10; return body;
            });
        }
        _azkarTab = Ui.Button("الأذكار", () => SetSection(false), true);
        _duaTab = Ui.Button("الأدعية", () => SetSection(true), true);
        foreach (var tab in new[] { _azkarTab, _duaTab }) { tab.MinimumHeightRequest = tab.HeightRequest = 44; tab.FontSize = 13; tab.Padding = new Thickness(6, 2); }
        Button? searchButton = null;
        void PaintSearch()
        {
            searchButton!.Text = _search.IsVisible ? "\uf00d" : "\uf002";
            SemanticProperties.SetDescription(searchButton, _search.IsVisible ? "إغلاق البحث" : "البحث في الأذكار والأدعية");
        }
        searchButton = Ui.Icon("\uf002", "البحث في الأذكار والأدعية", () =>
        {
            _search.IsVisible = !_search.IsVisible;
            if (_search.IsVisible) _search.Focus(); else { _search.Unfocus(); _search.Text = ""; }
            PaintSearch(); return Task.CompletedTask;
        });
        _search.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IsVisible)) PaintSearch(); };
        var options = Ui.Icon("\uf1de", "خيارات عرض الأذكار", ShowSettingsAsync);
        foreach (var icon in new[] { searchButton, options }) { icon.MinimumWidthRequest = icon.WidthRequest = 44; icon.MinimumHeightRequest = icon.HeightRequest = 44; }
        var sectionTabs = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(44), new(44) }, ColumnSpacing = 5 };
        sectionTabs.Add(_azkarTab); sectionTabs.Add(_duaTab, 1); sectionTabs.Add(searchButton, 2); sectionTabs.Add(options, 3);
#if ANDROID
        AlMithaq.Platforms.Android.CollectionPinch.Attach(_list, scale => font.Value = Math.Clamp(font.Value * scale, font.Minimum, font.Maximum));
#else
        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, e) => { if (e.Status == GestureStatus.Running) font.Value = Math.Clamp(font.Value * e.Scale, font.Minimum, font.Maximum); };
        _list.GestureRecognizers.Add(pinch);
#endif
        _summary.VerticalOptions = LayoutOptions.Center; _summary.FontSize = 12;
        var filters = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 10 };
        filters.Add(_category); filters.Add(_summary, 1);
        _progress.HeightRequest = 2;
        var header = Ui.Stack(sectionTabs, filters, _search, _progress);
        header.Padding = new Thickness(12, 4, 12, 4); header.Spacing = 3;
        _list.EmptyView = Ui.Label("لا توجد نتائج مطابقة. جرّب بحثًا آخر أو أظهر الأذكار المكتملة.", 13);
        _list.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 2 };
        var grid = new Grid { ClassId = "choice-host", RowDefinitions = { new(GridLength.Auto), new(GridLength.Star) } }; grid.Add(header); grid.Add(_list, 0, 1); Content = grid;
        _category.SelectedIndexChanged += (_, _) => Filter(); _search.TextChanged += (_, _) => Filter(); _hide.Toggled += (_, _) => Filter(); SetSection(false);
    }
    protected override bool OnBackButtonPressed()
    {
        if (PopupSheet.Dismiss(this)) return true;
        if (_search.IsVisible) { _search.IsVisible = false; _search.Unfocus(); _search.Text = ""; return true; }
        return base.OnBackButtonPressed();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _active = true;
        try { _data = await _repository.LoadAsync(); if (_active) SetSection(_duas); }
        catch (Exception ex) { _summary.Text = "تعذر قراءة الأذكار. أعد فتح الصفحة."; System.Diagnostics.Debug.WriteLine(ex); }
    }
    protected override void OnDisappearing() { _active = false; base.OnDisappearing(); }
    private void SetSection(bool duas)
    {
        _duas = duas; _search.Text = "";
        _category.Title = duas ? "قسم الأدعية" : "قسم الأذكار";
        _sections = [new(duas ? "كل الأدعية" : "كل الأذكار", All: true)];
        if (duas) _sections.AddRange([new("أدعية من القرآن", Type: 5), new("أدعية نبوية جامعة", Type: 6)]);
        else _sections.AddRange([new("أذكار الصباح", Chapter: 27), new("أذكار المساء", Chapter: 28), new("أذكار النوم", Chapter: 29), new("بعد الصلاة", Chapter: 25)]);
        _sections.Add(new("المفضلة", Favorite: true));
        var chapters = _data.Where(d => d.Chapter > 0).GroupBy(d => d.Chapter).OrderBy(g => g.Key);
        foreach (var group in chapters)
        {
            var title = group.First().ChapterTitle;
            var isDua = IsDuaChapter(title);
            if (isDua != duas || (!duas && group.Key is 25 or 27 or 28 or 29)) continue;
            _sections.Add(new(title, Chapter: group.Key));
        }
        _category.ItemsSource = _sections.Select(c => c.Title).ToArray();
        _category.SelectedIndex = 0;
        _search.Placeholder = "ابحث في جميع الأذكار والأدعية…";
        foreach (var (button, selected) in new[] { (_azkarTab, !duas), (_duaTab, duas) })
        {
            if (selected) { button.BackgroundColor = Ui.Green; button.TextColor = Colors.White; }
            else { Ui.ThemeProperty(button, Button.BackgroundColorProperty, "AppSoft"); Ui.ThemeProperty(button, Button.TextColorProperty, "AppInk"); }
        }
        Filter();
    }
    private static bool IsDuaChapter(string title) => AlMithaq.Core.Logic.ArabicSearch.Normalize(title).Contains("دعا", StringComparison.Ordinal);
    private IEnumerable<Dhikr> CategoryItems
    {
        get
        {
            var section = _sections.ElementAtOrDefault(_category.SelectedIndex);
            if (section is null) return [];
            if (!string.IsNullOrWhiteSpace(_search.Text))
            {
                var query = QuranRepository.Normalize(_search.Text);
                return _data.Where(d => (d.Chapter > 0 || d.Type is 5 or 6) && QuranRepository.Normalize(d.Text + " " + d.ChapterTitle + " " + d.Source).Contains(query, StringComparison.Ordinal));
            }
            if (section.Favorite) return _data.Where(d => Preferences.Get($"azkar.favorite.{d.Id}", false));
            if (section.All) return _data.Where(d => _duas ? d.Type is 5 or 6 || d.Chapter > 0 && IsDuaChapter(d.ChapterTitle) : d.Chapter > 0 && !IsDuaChapter(d.ChapterTitle));
            if (section.Chapter > 0) return _data.Where(d => d.Chapter == section.Chapter);
            return _data.Where(d => d.Type == section.Type);
        }
    }
    private string Key(Dhikr item) => item.Chapter > 0 ? $"azkar.progress.{DateTime.Today:yyyyMMdd}.hisn.{item.Id}" : $"azkar.progress.{DateTime.Today:yyyyMMdd}.{_category.SelectedIndex}.{item.Id}";
    private void Filter()
    {
        var rows = CategoryItems
            .Select(d => new DhikrRow(d, Key(d), Math.Clamp(Preferences.Get(Key(d), 0), 0, d.Repeat)));
        _list.ItemsSource = (_hide.IsToggled ? rows.Where(r => r.CanCount) : rows).ToList(); UpdateSummary();
    }
    private void UpdateSummary()
    {
        var items = CategoryItems.ToList(); var done = items.Count(d => Preferences.Get(Key(d), 0) >= d.Repeat);
        _summary.Text = $"{done} / {items.Count}";
        SemanticProperties.SetDescription(_summary, $"مكتمل اليوم {done} من {items.Count}، التقدم محفوظ تلقائيًا");
        _progress.Progress = items.Count == 0 ? 0 : done / (double)items.Count;
    }
    private async Task ResetAsync()
    {
        if (!await DisplayAlertAsync("جلسة جديدة", "تصفير تقدم القسم الحالي فقط؟", "تصفير", "إلغاء")) return;
        foreach (var item in CategoryItems) Preferences.Remove(Key(item)); Filter();
    }
}
public sealed partial class DhikrRow(Dhikr item, string key, int count) : ObservableObject
{
    public Dhikr Item { get; } = item;
    public string SourceCaption => Item.Source.Split(" — حصن المسلم · ", StringSplitOptions.None)[0];
    public string Key { get; } = key;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Status), nameof(CanCount))] private int done = count;
    public bool CanCount => Done < Item.Repeat;
    public string Status => CanCount ? $"{Done} / {Item.Repeat}" : "✓ اكتمل هذا الذكر";
}

