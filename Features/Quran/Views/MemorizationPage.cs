using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Logic;
using AlMithaq.Core.Models;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;
namespace AlMithaq.Pages;

public sealed class MemorizationPage : ContentPage
{
    private readonly IQuranRepository _repository;
    private readonly IAppNavigator _navigator;
    private QuranDocument? _document;
    private readonly ChoicePicker _chapter = new() { Title = "السورة" };
    private readonly ChoicePicker _filter = new() { Title = "عرض المقاطع", ItemsSource = new[] { "المستحقة للمراجعة", "كل مقاطعي" }, SelectedIndex = 0 };
    private readonly Entry _from = Ui.Input("أول آية", true), _to = Ui.Input("آخر آية", true);
    private readonly Label _status = Ui.Label("", 13), _summary = Ui.Label("", 14, true);
    private readonly VerticalStackLayout _items = new() { Spacing = 7 }, _editor;
    private bool _active, _loaded;
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public MemorizationPage(IQuranRepository repository, IAppNavigator navigator)
    {
        _repository = repository; _navigator = navigator; Ui.Page(this, "الحفظ والمراجعة");
        var fields = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10 };
        fields.Add(Ui.Stack(Ui.Label("من آية", 13), _from)); fields.Add(Ui.Stack(Ui.Label("إلى آية", 13), _to), 1);
        _editor = Ui.Stack(Ui.Label("مقطع جديد لخطة الحفظ", 15, true), _chapter, fields, Ui.Button("إضافة المقطع", AddPlan)); _editor.Spacing = 7;
        _editor.IsVisible = MemorizationStore.Load().Count == 0;
        var add = Ui.Button("إضافة مقطع للحفظ", () => _editor.IsVisible = !_editor.IsVisible, true);
        _chapter.SelectedIndexChanged += (_, _) =>
        {
            if (_chapter.SelectedItem is not QuranChapter c) return;
            _from.Text = "1"; _to.Text = Math.Min(5, c.Verses.Count).ToString();
        };
        _filter.SelectedIndexChanged += (_, _) => Render();
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        var body = Ui.Stack(Ui.Label("احفظ بهدوء وراجع باستمرار", 18, true), Ui.Label("اختر مقطعًا، ثم تدرّب على قراءته واستماعه، واختبر نفسك قبل تسجيل تقييمك. كل شيء محفوظ على جهازك.", 13),
            Ui.Card(Ui.Stack(_summary, Ui.Label("المواعيد تنظيم شخصي للمراجعة: 1، 3، 7، 14، ثم 30 يومًا بحسب تقييمك. يمكنك إعادة التدريب في اليوم نفسه.", 12))),
            add, Ui.Card(_editor), _status, _filter, _items);
        body.Padding = new Thickness(16, 12, 16, 24); body.Spacing = 8;
        var host = new Grid { ClassId = "choice-host" }; host.Add(new ScrollView { Content = body }); Content = host;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); _active = true;
        try
        {
            _document = await _repository.LoadAsync(); if (!_active) return;
            if (!_loaded) { _chapter.ItemsSource = _document.Chapters; _chapter.SelectedIndex = 0; _loaded = true; }
            Render();
        }
        catch (Exception ex) { _status.Text = "تعذر تحميل بيانات القرآن. أعد فتح الصفحة."; System.Diagnostics.Debug.WriteLine(ex); }
    }
    protected override void OnDisappearing() { _active = false; base.OnDisappearing(); }
    protected override bool OnBackButtonPressed() => PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    private List<MemorizationPlan> Plans() => MemorizationStore.Load().Where(p => _document is not null && p.EndAyah <= _document.Chapters[p.Surah - 1].Verses.Count).ToList();
    private void AddPlan()
    {
        _from.Unfocus(); _to.Unfocus();
        if (_chapter.SelectedItem is not QuranChapter chapter) return;
        if (!int.TryParse(NumberInput.Normalize(_from.Text ?? ""), out var from) || !int.TryParse(NumberInput.Normalize(_to.Text ?? ""), out var to) || from < 1 || to < from || to > chapter.Verses.Count)
        { _status.Text = $"اختر بداية ونهاية صحيحتين من 1 إلى {chapter.Verses.Count}"; return; }
        var plans = Plans();
        if (plans.Any(p => p.Surah == chapter.Number && p.StartAyah == from && p.EndAyah == to)) { _status.Text = "هذا المقطع موجود في خطتك بالفعل"; return; }
        if (plans.Count >= 200) { _status.Text = "لديك 200 مقطع. احذف مقطعًا غير مستخدم لإضافة آخر."; return; }
        MemorizationStore.Save(new(Guid.NewGuid().ToString(), chapter.Number, from, to, Today, Today));
        _editor.IsVisible = false; _status.Text = "أُضيف المقطع. ابدأ التدريب عندما يناسبك."; Render();
    }
    private void Render()
    {
        if (_document is null) return;
        var plans = Plans(); var due = plans.Count(p => p.IsDue(Today));
        _summary.Text = $"{plans.Count} مقطع · {due} للمراجعة · {plans.Count(p => p.Memorized)} متقن بتقييمك";
        _items.Clear();
        foreach (var plan in plans.Where(p => _filter.SelectedIndex == 1 || p.IsDue(Today)).OrderBy(p => p.NextReviewOn).ThenBy(p => p.Surah).ThenBy(p => p.StartAyah))
        {
            var name = _document.Chapters[plan.Surah - 1].Name;
            var title = Ui.Label($"{name} · الآيات {plan.StartAyah}–{plan.EndAyah}", 15, true);
            var state = Ui.Label(plan.LastReviewedOn is null ? "جديد · جاهز للتدريب" : plan.IsDue(Today) ? "حان موعد المراجعة" : $"المراجعة القادمة: {plan.NextReviewOn:dd / MM / yyyy}", 13); state.TextColor = Ui.Gold;
            var details = Ui.Label($"الجلسات المسجلة: {plan.Sessions} · " + (plan.Memorized ? "تقييمك: متقن" : "قيد الحفظ والتثبيت"), 12);
            var controls = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(48) }, ColumnSpacing = 6 };
            controls.Add(Ui.AsyncButton("تدرّب", () => OpenSessionAsync(plan, false))); controls.Add(Ui.AsyncButton("اختبر نفسك", () => OpenSessionAsync(plan, true), true), 1);
            controls.Add(Ui.Icon("\uf142", "إدارة المقطع", async () =>
            {
                var choice = await PopupSheet.ChooseAsync(this, "إدارة مقطع الحفظ", "إعادة بدء هذا المقطع", "حذف هذا المقطع");
                if (choice is null) return;
                if (await PopupSheet.ChooseAsync(this, choice + "؟", "تأكيد", "إلغاء") != "تأكيد") return;
                if (choice == "حذف هذا المقطع") MemorizationStore.Remove(plan.Id);
                else MemorizationStore.Save(new(plan.Id, plan.Surah, plan.StartAyah, plan.EndAyah, Today, Today)); Render();
            }), 2);
            foreach (var button in controls.Children.OfType<Button>().Where(b => b.FontFamily != "FontIconSolid")) { button.FontSize = 13; button.Padding = 5; }
            var content = Ui.Stack(title, state, details, controls); content.Spacing = 6; var card = Ui.Card(content); card.Padding = 14; _items.Add(card);
        }
        if (_items.Count == 0) _items.Add(Ui.Card(Ui.Label(plans.Count == 0 ? "أضف أول مقطع لتبدأ رحلة الحفظ." : "لا توجد مراجعات مستحقة الآن. يمكنك التدريب من «كل مقاطعي».", 14)));
    }
    private Task OpenSessionAsync(MemorizationPlan plan, bool test)
    {
        if (_document is null) return Task.CompletedTask;
        return Navigation.PushModalAsync(new NavigationPage(new MemorizationSessionPage(plan, _document.Chapters[plan.Surah - 1], test, _navigator)) { BarBackgroundColor = Ui.Green, BarTextColor = Colors.White });
    }
}

internal sealed class MemorizationSessionPage : ContentPage
{
    private readonly MemorizationPlan _plan;
    private readonly IAppNavigator _navigator;
    private readonly ChoicePicker _repeat = new() { Title = "تكرار المقطع", ItemsSource = new[] { "مرة واحدة", "3 مرات", "5 مرات", "10 مرات" }, SelectedIndex = 1 };
    private readonly Label _outcome = Ui.Label("", 13);
    private readonly List<PracticeVerseRow> _rows;
    private readonly List<Button> _ratings = [];
    private bool _closing, _recorded;
    public MemorizationSessionPage(MemorizationPlan plan, QuranChapter chapter, bool test, IAppNavigator navigator)
    {
        _plan = plan; _navigator = navigator; Ui.Page(this, test ? "اختبار الحفظ" : "جلسة الحفظ"); NavigationPage.SetHasBackButton(this, false);
        _rows = chapter.Verses.Where(v => v.Ayah >= plan.StartAyah && v.Ayah <= plan.EndAyah).Select(v => new PracticeVerseRow(v, test)).ToList();
        var font = new Slider { Minimum = 18, Maximum = 32, Value = 22 };
        var list = new CollectionView { SelectionMode = SelectionMode.None, ItemsSource = _rows };
        list.ItemTemplate = new DataTemplate(() =>
        {
            var reference = Ui.Label("", 12); reference.SetBinding(Label.TextProperty, "Reference"); reference.TextColor = Ui.Gold;
            var text = Ui.Label("", 22); text.FontFamily = "AmiriQuran"; text.LineHeight = 1.25; text.SetBinding(Label.TextProperty, nameof(PracticeVerseRow.Display)); text.SetBinding(Label.FontSizeProperty, new Binding(nameof(Slider.Value), source: font));
            var card = Ui.Card(Ui.Stack(reference, text)); card.Margin = new Thickness(16, 4);
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => { if (card.BindingContext is PracticeVerseRow row) row.Hidden = !row.Hidden; }; card.GestureRecognizers.Add(tap); return card;
        });
#if ANDROID
        AlMithaq.Platforms.Android.CollectionPinch.Attach(list, scale => font.Value = Math.Clamp(font.Value * scale, font.Minimum, font.Maximum));
#endif
        var reveal = Ui.Button("إظهار الكل", () => { foreach (var row in _rows) row.Hidden = false; }, true);
        var hide = Ui.Button("إخفاء الكل", () => { foreach (var row in _rows) row.Hidden = true; }, true);
        var switches = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 }; switches.Add(reveal); switches.Add(hide, 1);
        var audio = Ui.AsyncButton("استماع وتكرار هذا المقطع", async () =>
        {
            MemorizationStore.RequestAudio(new(plan.Surah, plan.StartAyah, plan.EndAyah, new[] { 1, 3, 5, 10 }[Math.Max(0, _repeat.SelectedIndex)]));
            await CloseAsync(); await _navigator.GoToAsync("quran");
        }); audio.FontSize = 13;
        var header = Ui.Stack(Ui.Label($"{chapter.Name} · الآيات {plan.StartAyah}–{plan.EndAyah}", 15, true), Ui.Label("اضغط على بطاقة الآية لإخفائها أو كشفها. التقييم ذاتي بعد التدريب؛ لا يستخدم التطبيق تسجيل صوتك.", 12), switches, _repeat, audio); header.Padding = new Thickness(16, 8); header.Spacing = 5;
        var ratings = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 6 };
        var labels = new[] { "أحتاج تدريبًا", "جيد", "متقن" };
        for (var i = 0; i < 3; i++)
        {
            var rating = (ReviewRating)i;
            var button = Ui.Button(labels[i], () => Rate(rating), true); button.FontSize = 12; button.Padding = 4; _ratings.Add(button); ratings.Add(button, i);
        }
        _outcome.Text = "كيف كان حفظك في هذه الجلسة؟";
        var footer = Ui.Stack(_outcome, ratings); footer.Padding = new Thickness(16, 8, 16, 16); footer.Spacing = 6;
        var layout = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } }; layout.Add(header); layout.Add(list, 0, 1); layout.Add(footer, 0, 2); Content = layout;
    }
    private void Rate(ReviewRating rating)
    {
        if (_recorded) return;
        var current = MemorizationStore.Load().FirstOrDefault(p => p.Id == _plan.Id); if (current is null) return;
        var updated = current.Review(DateOnly.FromDateTime(DateTime.Today), rating); MemorizationStore.Save(updated); _recorded = true;
        foreach (var button in _ratings) button.IsEnabled = false;
        _outcome.Text = updated.NextReviewOn <= DateOnly.FromDateTime(DateTime.Today) ? "سُجل تقييمك · أعد التدريب من الخطة عندما تكون مستعدًا" : $"سُجل تقييمك · المراجعة التالية {updated.NextReviewOn:dd / MM / yyyy}";
    }
    protected override bool OnBackButtonPressed() { if (!PopupSheet.Dismiss(this)) _ = CloseAsync(); return true; }
    private async Task CloseAsync() { if (_closing) return; _closing = true; await Navigation.PopModalAsync(); }
}

internal sealed partial class PracticeVerseRow(QuranVerse verse, bool initiallyHidden) : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Display))] private bool hidden = initiallyHidden;
    public string Display => Hidden ? "اضغط لإظهار الآية" : verse.Text;
    public string Reference => $"آية {verse.Ayah}";
}
