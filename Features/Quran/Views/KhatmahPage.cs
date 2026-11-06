using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Logic;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Features.Quran.Views;
using AlMithaq.Shared.Controls;
using System.Globalization;
namespace AlMithaq.Pages;

public sealed class KhatmahPage : ContentPage
{
    private readonly IAppNavigator _navigator;
    private readonly Label _summary = Ui.Label("", 17, true), _range = Ui.Label("", 15), _status = Ui.Label("", 12), _numbers = Ui.Label("", 13);
    private readonly ProgressBar _progress = new() { ProgressColor = Ui.Gold, HeightRequest = 5 };
    private readonly PageNumberDropdown _start = new();
    private readonly ChoicePicker _duration = new() { Title = "مدة الخطة", ItemsSource = new[] { "30 يومًا", "60 يومًا", "90 يومًا", "تاريخ أحدده" }, SelectedIndex = 0 };
    private readonly DatePicker _target = new() { Date = DateTime.Today.AddDays(29), MinimumDate = DateTime.Today, MaximumDate = DateTime.Today.AddYears(10), Format = "dd / MM / yyyy", FontFamily = "Almarai-Regular", FontSize = 14 };
    private readonly DatePicker _extend = new() { MinimumDate = DateTime.Today, MaximumDate = DateTime.Today.AddYears(10), Format = "dd / MM / yyyy", FontFamily = "Almarai-Regular", FontSize = 14 };
    private readonly Switch _link = new();
    private readonly VerticalStackLayout _editor, _deadlineEditor;
    private readonly Button _open, _done, _changeDeadline;
    private bool _rendering;
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    public KhatmahPage(IAppNavigator navigator)
    {
        _navigator = navigator; Ui.Page(this, "خطة الختمة");
        foreach (var picker in new[] { _target, _extend }) Ui.ThemeProperty(picker, DatePicker.TextColorProperty, "AppInk");
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        _start.SelectedPage = 1;
        _duration.SelectedIndexChanged += (_, _) => { _target.IsEnabled = _duration.SelectedIndex == 3; if (_duration.SelectedIndex < 3) _target.Date = DateTime.Today.AddDays(new[] { 30, 60, 90 }[Math.Max(0, _duration.SelectedIndex)] - 1); };
        _target.IsEnabled = false;
        var create = Ui.AsyncButton("حفظ خطة الختمة", CreateAsync);
        _editor = Ui.Stack(Ui.Label("خطة تناسب وقتك", 15, true), Ui.Label("صفحة البداية", 13), _start, Ui.Label("مدة الخطة", 13), _duration, Ui.Label("موعد إتمام الخطة", 13), _target,
            Ui.Label("يُحسب اليوم ضمن المدة. إن فاتك يوم، تبقى الصفحات غير المقروءة وتُوزّع على الأيام المتبقية.", 12), create); _editor.Spacing = 7; _editor.IsVisible = KhatmahStore.Load() is null;
        _open = Ui.AsyncButton("قراءة ورد الختمة", OpenAsync);
        _done = Ui.AsyncButton("أتممت ورد اليوم", CompleteAsync, true);
        var actions = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 }; actions.Add(_open); actions.Add(_done, 1);
        _changeDeadline = Ui.Button("تعديل الموعد مع حفظ التقدم", () => { _extend.Date = (KhatmahStore.Load()?.TargetDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today) < DateTime.Today ? DateTime.Today : KhatmahStore.Load()!.TargetDate.ToDateTime(TimeOnly.MinValue); _deadlineEditor!.IsVisible = !_deadlineEditor.IsVisible; }, true);
        _deadlineEditor = Ui.Stack(_extend, Ui.Button("حفظ الموعد الجديد", () =>
        {
            if (KhatmahStore.Load() is not { } plan) return;
            KhatmahStore.Save(plan with { TargetDate = DateOnly.FromDateTime(_extend.Date ?? DateTime.Today) }); _deadlineEditor!.IsVisible = false; Render();
        })); _deadlineEditor.IsVisible = false;
        var linkRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        var linkLabel = Ui.Label("ربط ورد الرئيسية بخطة الختمة", 13); linkLabel.VerticalOptions = LayoutOptions.Center; linkRow.Add(linkLabel); linkRow.Add(_link, 1);
        _link.Toggled += async (_, e) =>
        {
            if (_rendering || KhatmahStore.Load() is null) return;
            KhatmahStore.SetLinked(e.Value); if (e.Value && WirdReminders.Enabled) await WirdReminders.RequestPermissionAsync(); Render();
        };
        var newPlan = Ui.Button("إنشاء خطة جديدة", () => { _editor.IsVisible = !_editor.IsVisible; }, true);
        var body = Ui.Stack(Ui.Label("خطوات صغيرة حتى الإتمام", 18, true), Ui.Label("موعد تختاره، وورد مرن يُحسب من الصفحات المتبقية. تقدمك خاص ومحفوظ على هذا الجهاز.", 13),
            Ui.Card(Ui.Stack(_summary, _progress, _numbers, _range, actions, _status)), Ui.Card(Ui.Stack(linkRow, Ui.Label("الربط اختياري؛ يحتفظ التطبيق بإعداد وردك المستقل عند فك الربط. تذكيرات المساء تتبع الورد المرتبط.", 12))),
            _changeDeadline, _deadlineEditor, newPlan, Ui.Card(_editor));
        body.Padding = new Thickness(16, 12, 16, 24); body.Spacing = 8;
        var host = new Grid { ClassId = "choice-host" }; host.Add(new ScrollView { Content = body }); Content = host; Render();
    }
    protected override void OnAppearing() { base.OnAppearing(); Render(); }
    protected override bool OnBackButtonPressed() => PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    private void Render()
    {
        var plan = KhatmahStore.Load(); var range = plan?.Assignment(Today);
        _rendering = true; _link.IsToggled = KhatmahStore.Linked; _rendering = false;
        _summary.Text = plan is null ? "ابدأ خطة ختمتك" : plan.IsFinished ? "✓ أتممت خطة القراءة" : $"الموعد: {plan.TargetDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("ar-EG"))}";
        _progress.Progress = plan?.Progress ?? 0;
        _numbers.Text = plan is null ? "اختر مدة جاهزة أو تاريخًا يناسبك" : $"قُرئ {plan.ReadPages} · المتبقي {plan.RemainingPages} صفحة · الإنجاز {plan.Progress:P0}";
        _range.Text = range is null ? (plan?.IsFinished == true ? "بارك الله في استمرارك. يمكنك بدء خطة جديدة." : "حدد صفحة البداية وموعد الإتمام أدناه") : $"ورد اليوم: الصفحات {range.StartPage}–{range.EndPage} · {range.Pages} صفحة";
        _status.Text = range?.Completed == true ? "✓ إتمام اليوم مسجل بتأكيدك" : plan is not null && plan.TargetDate < Today ? "الموعد السابق انتهى. عدّل الموعد لتوزيع المتبقي دون تخطي صفحات." : "لا تُسجل الصفحات مكتملة بمجرد فتح القارئ؛ أكّد الإتمام بعد القراءة.";
        _open.IsEnabled = range is not null; _done.IsEnabled = range is { Completed: false };
        _changeDeadline.IsVisible = plan is { IsFinished: false }; if (plan is null) _editor.IsVisible = true;
    }
    private async Task CreateAsync()
    {
        var start = _start.SelectedPage;
        var target = DateOnly.FromDateTime(_target.Date ?? DateTime.Today);
        if (target < Today) { _status.Text = "اختر موعدًا من اليوم فما بعد"; return; }
        if (KhatmahStore.Load() is not null && await PopupSheet.ChooseAsync(this, "استبدال الخطة الحالية وإعادة تقدم الخطة؟", "إنشاء الخطة الجديدة", "إلغاء") != "إنشاء الخطة الجديدة") return;
        var linked = _link.IsToggled;
        KhatmahStore.Save(new(start, start, Today, target)); KhatmahStore.SetLinked(linked);
        if (linked && WirdReminders.Enabled) await WirdReminders.RequestPermissionAsync();
        _editor.IsVisible = false; Render();
    }
    private async Task OpenAsync()
    {
        if (KhatmahStore.Load()?.Assignment(Today) is not { } range) return;
        Preferences.Set("quran.wird-page.request", range.StartPage); await _navigator.GoToAsync("quran");
    }
    private async Task CompleteAsync()
    {
        if (KhatmahStore.Load()?.Assignment(Today) is not { Completed: false } range) return;
        if (await PopupSheet.ChooseAsync(this, $"هل أتممت الصفحات {range.StartPage}–{range.EndPage}؟", "نعم، أتممت القراءة", "لاحقًا") != "نعم، أتممت القراءة") return;
        var celebrate = KhatmahStore.Linked ? DailyWirdStore.Complete() : KhatmahStore.CompleteToday(); Render();
        if (celebrate) await WirdCelebration.ShowAsync(this);
    }
}
