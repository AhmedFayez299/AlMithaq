using AlMithaq.Shared.Controls;

namespace AlMithaq.Pages;

public partial class TasbeehPage : ContentPage
{
    private readonly ChoicePicker _dhikr = new() { Title = "اختر الذكر", ItemsSource = new[] { "سبحان الله", "الحمد لله", "الله أكبر", "لا إله إلا الله", "أستغفر الله", "اللهم صل وسلم على نبينا محمد" } };
    private readonly Label _count = Ui.Label("0", 48, true);
    private readonly Label _today = Ui.Label("", 19, true);
    private readonly Label _total = Ui.Label("", 19, true);
    private readonly Label _goalText = Ui.Label("", 12);
    private readonly TasbeehDial _dial = new();
    private readonly GraphicsView _dialView;
    private readonly ProgressBar _progress = new() { ProgressColor = Ui.Green };
    private readonly Switch _haptics = new() { IsToggled = Preferences.Get("tasbeeh.haptics", false) };
    private int _goal = Preferences.Get("tasbeeh.goal", 33);
    private int _value;
    private readonly Stack<string> _undoDays = new();
    private string CounterKey => $"tasbeeh.session.{_dhikr.SelectedIndex}";
    private string DayKey => $"tasbeeh.day.{DateTime.Today:yyyyMMdd}";
    public TasbeehPage()
    {
        InitializeComponent(); Ui.Page(this, "السبحة");
        _dhikr.SelectedIndex = Math.Clamp(Preferences.Get("tasbeeh.dhikr", 0), 0, 5);
        _count.HorizontalTextAlignment = TextAlignment.Center; _count.TextColor = Colors.White;
        var tapHint = Ui.Label("اضغط للتسبيح", 12); tapHint.TextColor = Color.FromArgb("#E8D8AC"); tapHint.HorizontalTextAlignment = TextAlignment.Center;
        var counterContent = Ui.Stack(_count, tapHint); counterContent.Spacing = 0; counterContent.VerticalOptions = LayoutOptions.Center; counterContent.InputTransparent = true;
        var tap = Ui.Button("", Increment); tap.CornerRadius = 100; tap.Padding = 0;
        SemanticProperties.SetDescription(tap, "إضافة تسبيحة");
        var counter = new Grid { WidthRequest = 232, HeightRequest = 232, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8) }; _dialView = new GraphicsView { Drawable = _dial, InputTransparent = true };
        tap.Margin = 25; tap.BorderColor = Color.FromArgb("#68A893"); tap.BorderWidth = 1;
        counter.Add(_dialView); counter.Add(tap); counter.Add(counterContent);
        var goalRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
        goalRow.Add(Ui.Button("هدف ٣٣", () => SetGoal(33), true)); goalRow.Add(Ui.Button("هدف ١٠٠", () => SetGoal(100), true), 1); goalRow.Add(Ui.AsyncButton("مخصص", CustomGoalAsync, true), 2);
        foreach (var button in goalRow.Children.OfType<Button>()) { button.FontSize = 12; button.Padding = new Thickness(6, 8); }
        var counterTools = new Grid { ColumnDefinitions = { new(48), new(GridLength.Star), new(48) }, ColumnSpacing = 8 };
        _goalText.HorizontalTextAlignment = TextAlignment.Center; _goalText.VerticalOptions = LayoutOptions.Center;
        counterTools.Add(Ui.Icon("\uf0e2", "تراجع عن ضغطة", () => { Undo(); return Task.CompletedTask; })); counterTools.Add(_goalText, 1); counterTools.Add(Ui.Icon("\uf01e", "تصفير الجلسة", ResetAsync), 2);
        _progress.HeightRequest = 4;
        var counterBody = Ui.Stack(counter, counterTools, _progress); counterBody.Spacing = 6;
        var stats = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10 };
        Border Stat(string caption, Label value) { value.HorizontalTextAlignment = TextAlignment.Center; var label = Ui.Label(caption, 11); label.HorizontalTextAlignment = TextAlignment.Center; Ui.ThemeProperty(label, Label.TextColorProperty, "AppMuted"); var card = Ui.Card(Ui.Stack(value, label)); card.Padding = 10; return card; }
        stats.Add(Stat("تسبيحات اليوم", _today)); stats.Add(Stat("الإجمالي المسجل", _total), 1);
        var hapticsRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; var hapticsLabel = Ui.Label("اهتزاز مع التسبيح وعند إتمام الهدف", 12); hapticsLabel.VerticalOptions = LayoutOptions.Center; hapticsRow.Add(hapticsLabel); hapticsRow.Add(_haptics, 1);
        var note = Ui.Label("الهدف لتنظيم وردك، وليس عددًا مأثورًا لكل ذكر. يُحفظ العداد تلقائيًا على جهازك.", 10); Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
        var stack = Ui.Stack(_dhikr, Ui.Card(counterBody), Ui.Label("هدف الجلسة", 13, true), goalRow, stats, hapticsRow, note);
        stack.Spacing = 6; stack.Padding = new Thickness(16, 10, 16, 24);
        var host = new Grid { ClassId = "choice-host" }; host.Add(new ScrollView { Content = stack }); Content = host;
        _dhikr.SelectedIndexChanged += (_, _) => { Preferences.Set("tasbeeh.dhikr", _dhikr.SelectedIndex); Load(); };
        _haptics.Toggled += (_, e) => Preferences.Set("tasbeeh.haptics", e.Value);
        Load();
    }
    protected override bool OnBackButtonPressed() => PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    protected override void OnAppearing() { base.OnAppearing(); Load(); }
    private void Load() { _undoDays.Clear(); _value = Preferences.Get(CounterKey, 0); Render(); }
    private void Render()
    {
        _dial.Value = _value; _dial.Goal = _goal; _dialView.Invalidate();
        _count.Text = _value.ToString(); _progress.Progress = Math.Min(1, _value / (double)Math.Max(1, _goal));
        _goalText.Text = _value >= _goal ? $"اكتمل هدفك ({_goal}) — يمكنك الاستمرار" : $"الهدف {_goal} · المتبقي {_goal - _value}";
        _today.Text = Preferences.Get(DayKey, 0).ToString(); _total.Text = Preferences.Get("tasbeeh.total", 0).ToString();
        SemanticProperties.SetDescription(_count, $"عدد التسبيحات {_value}");
    }
    private void Increment()
    {
        if (_value == int.MaxValue) return; _value++; _undoDays.Push(DayKey); Preferences.Set(CounterKey, _value);
        Preferences.Set(DayKey, Preferences.Get(DayKey, 0) + 1); Preferences.Set("tasbeeh.total", Preferences.Get("tasbeeh.total", 0) + 1);
        if (_haptics.IsToggled) TasbeehFeedback.Pulse(_value % Math.Max(1, _goal) == 0);
        _dial.Value = _value; _dial.Goal = _goal; _dialView.Invalidate();
        Render();
    }
    private void Undo()
    {
        if (_value <= 0 || _undoDays.Count == 0) return; _value--; Preferences.Set(CounterKey, _value);
        var day = _undoDays.Pop(); Preferences.Set(day, Math.Max(0, Preferences.Get(day, 0) - 1)); Preferences.Set("tasbeeh.total", Math.Max(0, Preferences.Get("tasbeeh.total", 0) - 1)); Render();
    }
    private void SetGoal(int goal) { _goal = goal; Preferences.Set("tasbeeh.goal", goal); Render(); }
    private async Task ResetAsync() { if (await DisplayAlertAsync("تصفير الجلسة", "تصفير عداد الذكر الحالي؟ يبقى إجمالي اليوم محفوظًا.", "تصفير", "إلغاء")) { _value = 0; _undoDays.Clear(); Preferences.Set(CounterKey, 0); Render(); } }
    private async Task CustomGoalAsync()
    {
        var input = await DisplayPromptAsync("هدف شخصي", "عدد من 1 إلى 100000", "حفظ", "إلغاء", keyboard: Keyboard.Numeric);
        if (int.TryParse(AlMithaq.Core.Logic.NumberInput.Normalize(input ?? ""), out var n) && n is > 0 and <= 100000) SetGoal(n);
    }
}
