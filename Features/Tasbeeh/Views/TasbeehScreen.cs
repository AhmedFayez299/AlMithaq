using AlMithaq.Shared.Controls;

namespace AlMithaq.Pages;

public partial class TasbeehPage : ContentPage
{
    private readonly ChoicePicker _dhikr = new() { Title = "اختر الذكر", ItemsSource = new[] { "سبحان الله", "الحمد لله", "الله أكبر", "لا إله إلا الله", "أستغفر الله" } };
    private readonly List<Button> _dhikrChoices = [];
    private readonly Label _selectedDhikr = Ui.Label("",13,true);
    private readonly Label _count = Ui.Label("0", 48, true);
    private readonly Label _today = Ui.Label("", 19, true);
    private readonly Label _total = Ui.Label("", 19, true);
    private readonly Label _goalText = Ui.Label("", 12);
    private readonly TasbeehDial _dial = new();
    private readonly GraphicsView _dialView;
    private readonly ProgressBar _progress = new() { ProgressColor = Ui.Green };
    private readonly Switch _haptics = new ReminderToggle() { IsToggled = Preferences.Get("tasbeeh.haptics", false) };
    private int _goal = Preferences.Get("tasbeeh.goal", 33);
    private int _value;
    private readonly Dictionary<int, Button> _goalChoices = [];
    private readonly Stack<string> _undoDays = new();
    private string CounterKey => $"tasbeeh.session.{_dhikr.SelectedIndex}";
    private string DayKey => $"tasbeeh.day.{DateTime.Today:yyyyMMdd}";
    public TasbeehPage()
    {
        InitializeComponent(); Ui.Page(this, "السبحة");
        _dhikr.SelectedIndex = Math.Clamp(Preferences.Get("tasbeeh.dhikr", 0), 0, 4);
        _count.FontSize = 54; _count.HorizontalTextAlignment = TextAlignment.Center; _count.TextColor = Colors.White;
        _selectedDhikr.TextColor = Color.FromArgb("#E8D8AC"); _selectedDhikr.HorizontalTextAlignment = TextAlignment.Center; _selectedDhikr.MaxLines = 2; _selectedDhikr.WidthRequest = 156;
        var counterContent = Ui.Stack(_selectedDhikr,_count); counterContent.Spacing = 0; counterContent.VerticalOptions = LayoutOptions.Center; counterContent.InputTransparent = true;
        var tap = Ui.Button("", Increment); tap.CornerRadius = 100; tap.Padding = 0;
        SemanticProperties.SetDescription(tap, "إضافة تسبيحة");
        var counter = new Grid { WidthRequest = 232, HeightRequest = 232, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8) }; _dialView = new GraphicsView { Drawable = _dial, InputTransparent = true };
#if IOS
        tap.WidthRequest = tap.HeightRequest = 182;
        AlMithaq.Platforms.iOS.CircularButton.Attach(tap);
#endif
        tap.Margin = 25; tap.BorderColor = Color.FromArgb("#68A893"); tap.BorderWidth = 1;
        counter.Add(_dialView); counter.Add(tap); counter.Add(counterContent);
        var goalRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
        foreach (var (goal,column) in new[] { (33,0),(100,1) })
        { var choice = Ui.Button(goal.ToString(),()=>SetGoal(goal),true); _goalChoices[goal] = choice; goalRow.Add(choice,column); }
        goalRow.Add(Ui.AsyncButton("هدف آخر",CustomGoalAsync,true),2);
        foreach (var button in goalRow.Children.OfType<Button>()) { button.FontSize = 12; button.Padding = new Thickness(6, 8); }
        var counterTools = new Grid { ColumnDefinitions = { new(48), new(GridLength.Star), new(48) }, ColumnSpacing = 8 };
        _goalText.HorizontalTextAlignment = TextAlignment.Center; _goalText.VerticalOptions = LayoutOptions.Center;
        counterTools.Add(Ui.Icon("\uf0e2", "تراجع عن ضغطة", () => { Undo(); return Task.CompletedTask; })); counterTools.Add(_goalText, 1); counterTools.Add(Ui.Icon("\uf01e", "تصفير الجلسة", ResetAsync), 2);
        _progress.HeightRequest = 4;
        var counterBody = Ui.Stack(counter, counterTools, _progress); counterBody.Spacing = 6;
        var stats = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10 };
        Border Stat(string caption, Label value) { value.HorizontalTextAlignment = TextAlignment.Center; var label = Ui.Label(caption, 11); label.HorizontalTextAlignment = TextAlignment.Center; Ui.ThemeProperty(label, Label.TextColorProperty, "AppMuted"); var card = ScreenDesign.Surface(Ui.Stack(value, label)); card.Padding = 10; return card; }
        stats.Add(Stat("تسبيحات اليوم", _today)); stats.Add(Stat("الإجمالي المسجل", _total), 1);
        var hapticsRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; var hapticsLabel = Ui.Label("اهتزاز مع التسبيح وعند إتمام الهدف", 12); hapticsLabel.VerticalOptions = LayoutOptions.Center; hapticsRow.Add(hapticsLabel); hapticsRow.Add(_haptics, 1);
        var note = Ui.Label("الهدف لتنظيم وردك، وليس عددًا مأثورًا لكل ذكر. يُحفظ العداد تلقائيًا على جهازك.", 10); Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
        var dhikrCards = new FlexLayout { Direction = Microsoft.Maui.Layouts.FlexDirection.Row,Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Center };
        for (var i=0;i<_dhikr.ItemsSource!.Count;i++)
        {
            var index = i; var choice = Ui.Button(_dhikr.ItemsSource[i]?.ToString() ?? "",()=>_dhikr.SelectedIndex=index,true);
            choice.FontSize = 11; choice.Padding = new Thickness(3,4); choice.Margin = new Thickness(3,3); choice.MinimumHeightRequest = choice.HeightRequest = 38;
            FlexLayout.SetBasis(choice,new Microsoft.Maui.Layouts.FlexBasis(.30f,true)); _dhikrChoices.Add(choice); dhikrCards.Children.Add(choice);
        }
        var stack = Ui.Stack(ScreenDesign.Intro("ذكرٌ يطمئن به قلبك","اختر ذكرك، وتابع وردك بهدوء", "\uf192"),dhikrCards,ScreenDesign.Surface(counterBody),ScreenDesign.Section("هدف الجلسة", "\uf140"),goalRow,stats,ScreenDesign.Surface(hapticsRow,new Thickness(12,4)),note);
        stack.Spacing = 6; stack.Padding = new Thickness(16, 10, 16, 24);
        var host = new Grid { ClassId = "choice-host" }; host.Add(new ScrollView { Content = stack }); Content = host;
        _dhikr.SelectedIndexChanged += (_, _) => { Preferences.Set("tasbeeh.dhikr", _dhikr.SelectedIndex); Load(); };
        _haptics.Toggled += (_, e) => Preferences.Set("tasbeeh.haptics", e.Value);
        Load();
    }
    protected override bool OnBackButtonPressed() => PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    protected override void OnAppearing() { base.OnAppearing(); AppAppearance.Changed += Render; Load(); }
    protected override void OnDisappearing() { AppAppearance.Changed -= Render; base.OnDisappearing(); }
    private void Load() { _undoDays.Clear(); _value = Preferences.Get(CounterKey, 0); Render(); }
    private void Render()
    {
        _selectedDhikr.Text = _dhikr.SelectedItem?.ToString();
        for (var i=0;i<_dhikrChoices.Count;i++)
        {
            if (i==_dhikr.SelectedIndex) { _dhikrChoices[i].BackgroundColor=Color.FromArgb("#124A3B"); _dhikrChoices[i].TextColor=Colors.White; }
            else { Ui.ThemeProperty(_dhikrChoices[i],Button.BackgroundColorProperty,"AppSoft"); Ui.ThemeProperty(_dhikrChoices[i],Button.TextColorProperty,"AppInk"); }
        }
        _dial.Value = _value; _dial.Goal = _goal; _dialView.Invalidate();
        foreach (var choice in _goalChoices)
        {
            if (choice.Key == _goal) { choice.Value.BackgroundColor = Ui.Green; choice.Value.TextColor = Colors.White; }
            else { Ui.ThemeProperty(choice.Value,Button.BackgroundColorProperty,"AppSoft"); Ui.ThemeProperty(choice.Value,Button.TextColorProperty,"AppInk"); }
        }
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
    private async Task ResetAsync() { if (await PopupSheet.AlertAsync(this, "تصفير الجلسة", "تصفير عداد الذكر الحالي؟ يبقى إجمالي اليوم محفوظًا.", "تصفير", "إلغاء")) { _value = 0; _undoDays.Clear(); Preferences.Set(CounterKey, 0); Render(); } }
    private async Task CustomGoalAsync()
    {
        var goals = Enumerable.Range(1,1000).Concat(new[] { 2000,5000,10000,50000,100000 }).ToArray();
        var selected = await PopupSheet.ShowAsync(this,"اختر هدف الجلسة",goals.Select(n=>n.ToString()).ToArray(),Array.IndexOf(goals,_goal),selection:true);
        if (selected is { } index) SetGoal(goals[index]);
    }
}
