using AlMithaq.Core.Logic;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;
namespace AlMithaq.Features.Quran.Views;

public sealed class DailyWirdView : VerticalStackLayout
{
    private readonly Label _summary = Ui.Label("", 12);
    private readonly Label _status = Ui.Label("", 10);
    private readonly Entry _pages = new() { Keyboard = Keyboard.Numeric, FontFamily = "Almarai-Regular", FontSize = 14, Placeholder = "مثل 5" };
    private readonly PageNumberDropdown _start = new();
    private readonly VerticalStackLayout _editor;
    private readonly Button _open, _done;
    public DailyWirdView(ContentPage owner, Func<Task> navigate, Func<Task> configurePlan)
    {
        Spacing = 8;
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        foreach (var entry in new[] { _pages }) { Ui.ThemeProperty(entry, Entry.TextColorProperty, "AppInk"); Ui.ThemeProperty(entry, Entry.PlaceholderColorProperty, "AppMuted"); }
        var title = Ui.Label("وردي من القرآن", 14, true); title.VerticalOptions = LayoutOptions.Center;
        var heading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48) } }; heading.Add(title);
        heading.Add(Ui.Icon("\uf013", "تحديد الورد وتعديله", () => { if (KhatmahStore.Linked) return configurePlan(); _editor!.IsVisible = !_editor.IsVisible; return Task.CompletedTask; }), 1);
        var inputs = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10 };
        inputs.Add(Ui.Stack(Ui.Label("صفحات يوميًا", 11), _pages)); inputs.Add(Ui.Stack(Ui.Label("صفحة البداية", 11), _start), 1);
        var save = Ui.AsyncButton("حفظ وردي", async () =>
        {
            if (!int.TryParse(NumberInput.Normalize(_pages.Text ?? ""), out var pages) || pages is < 1 or > 604)
            { _status.Text = "أدخل عدد الصفحات اليومي من 1 إلى 604، واختر صفحة البداية من القائمة"; return; }
            var start = _start.SelectedPage;
            var old = DailyWirdStore.Load();
            DailyWirdStore.Save(old is not null && old.StartPage == start && old.PagesPerDay == pages ? old : new(start, pages));
            if (WirdReminders.Enabled) await WirdReminders.RequestPermissionAsync();
            WirdReminders.Refresh(); _editor!.IsVisible = false; Refresh();
        }, true); save.FontSize = 12;
        var note = Ui.Label("بعد إتمام ورد اليوم، يبدأ ورد اليوم التالي من الصفحة التالية. إن فاتك يوم، يبقى موضعك دون تخطّي. أرقام الصفحات وفق المصحف الكامل.", 10); Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
        _editor = Ui.Stack(inputs, save, note); _editor.Spacing = 6; _editor.IsVisible = false;
        _open = Ui.AsyncButton("فتح ورد اليوم", async () => { if (DailyWirdStore.Load() is not { } plan) return; Preferences.Set("quran.wird-page.request", plan.StartPage); await navigate(); }); _open.FontSize = 12;
        _done = Ui.Button("أتممت الورد", async () =>
        {
            if (DailyWirdStore.Load() is not { } plan || plan.IsComplete(DateOnly.FromDateTime(DateTime.Today))) return;
            _done!.IsEnabled = false;
            try
            {
                var answer = await PopupSheet.ChooseAsync(owner, $"هل أتممت قراءة الصفحات {plan.StartPage}–{plan.EndPage}؟", "نعم، أتممت الورد", "لاحقًا");
                if (answer == "نعم، أتممت الورد") { var celebrate = DailyWirdStore.Complete(); Refresh(); if (celebrate) await WirdCelebration.ShowAsync(owner); }
            }
            finally { Refresh(); }
        }, true); _done.FontSize = 12;
        var actions = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 }; actions.Add(_open); actions.Add(_done, 1);
        var reminders = new Switch { IsToggled = WirdReminders.Enabled };
        reminders.Toggled += async (_, e) => { WirdReminders.Enabled = e.Value; if (e.Value) await WirdReminders.RequestPermissionAsync(); Refresh(); };
        var reminderRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        var reminderLabel = Ui.Label("تذكيري بالورد مساءً", 12); reminderLabel.VerticalOptions = LayoutOptions.Center;
        reminderRow.Add(reminderLabel); reminderRow.Add(reminders, 1);
        Add(heading); Add(_summary); Add(_editor); Add(actions); Add(reminderRow); Add(_status); Refresh();
    }
    public void Refresh()
    {
        var plan = DailyWirdStore.Load(); var complete = plan?.IsComplete(DateOnly.FromDateTime(DateTime.Today)) == true;
        _summary.Text = plan is null ? "اختر عدد الصفحات وصفحة البداية لتحديد وردك" : $"ورد اليوم: صفحة {plan.StartPage} إلى {plan.EndPage}\nعدد الصفحات: {plan.EndPage - plan.StartPage + 1}";
        _pages.Text = (plan?.PagesPerDay ?? 5).ToString(); _start.SelectedPage = plan?.StartPage ?? 1;
        _open.IsEnabled = plan is not null; _done.IsEnabled = plan is not null && !complete;
        _status.Text = complete ? "✓ أتممت ورد اليوم · أحسنت الاستمرار" : WirdReminders.Status;
        if (KhatmahStore.Linked)
        {
            _editor.IsVisible = false;
            _summary.Text = KhatmahStore.Load()?.IsFinished == true ? "اكتملت خطة الختمة · أحسنت الاستمرار" : "وردك مرتبط بخطة الختمة\n" + _summary.Text;
            if (plan is null) _status.Text = "ابدأ خطة جديدة من إعدادات الختمة";
        }
        else if (plan is null) _editor.IsVisible = true;
    }
}
