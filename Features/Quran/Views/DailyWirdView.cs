using AlMithaq.Core.Logic;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;
namespace AlMithaq.Features.Quran.Views;

public sealed class DailyWirdView : VerticalStackLayout
{
    private readonly Label _summary = Ui.Label("",15,true), _status = Ui.Label("",12), _quantity = Ui.Label("",12);
    private readonly PageNumberDropdown _start = new(), _end = new();
    private readonly VerticalStackLayout _editor;
    private readonly Button _open, _done;
    private bool _sync;
    public DailyWirdView(ContentPage owner, Func<Task> navigate, Func<Task> configurePlan)
    {
        Spacing = 12; _start.OwnerPage = _end.OwnerPage = owner;
        Ui.ThemeProperty(_status,Label.TextColorProperty,"AppMuted"); Ui.ThemeProperty(_quantity,Label.TextColorProperty,"AppMuted");
        var heading = _summary;
        var inputs = new Grid { FlowDirection = FlowDirection.RightToLeft,ColumnDefinitions = { new(GridLength.Star),new(GridLength.Star) },ColumnSpacing = 10 };
        inputs.Add(Ui.Stack(Ui.Label("صفحة البداية",13,true),_start)); inputs.Add(Ui.Stack(Ui.Label("صفحة النهاية",13,true),_end),1);
        void Count() { if (_sync) return; _end.MinimumPage = _start.SelectedPage; if (_end.SelectedPage < _start.SelectedPage) _end.SelectedPage = _start.SelectedPage; _quantity.Text = $"عدد صفحات الورد: {_end.SelectedPage-_start.SelectedPage+1}"; }
        _start.SelectedPageChanged += (_,_)=>Count(); _end.SelectedPageChanged += (_,_)=>Count();
        var save = Ui.Button("اعتماد الورد",()=>
        {
            var first = _start.SelectedPage; var pages = _end.SelectedPage-first+1; var old = DailyWirdStore.Load();
            DailyWirdStore.Save(old is not null && old.StartPage == first && old.PagesPerDay == pages ? old : new(first,pages,AssignedOn:DateOnly.FromDateTime(DateTime.Today)));
            Refresh();
        });
        var note = Ui.Label("نحافظ على عدد الصفحات الذي تختاره. إذا فاتك ورد، ننتظر إتمامه ثم نتيح ورد اليوم في اليوم نفسه.",12); Ui.ThemeProperty(note,Label.TextColorProperty,"AppMuted");
        save.IsEnabled = !KhatmahStore.Linked;
        _editor = Ui.Stack(inputs,_quantity,save,note);
        if (KhatmahStore.Linked) _editor.Add(Ui.AsyncButton("تعديل خطة الختمة",configurePlan,true));
        _open = Ui.AsyncButton("قراءة الورد",async()=> { if (DailyWirdStore.Load() is not { } plan) return; Preferences.Set("quran.wird-page.request",plan.StartPage); await navigate(); });
        _done = Ui.AsyncButton("أتممت الورد",async()=>
        {
            if (DailyWirdStore.Load() is not { } plan || plan.IsComplete(DateOnly.FromDateTime(DateTime.Today))) return;
            if (!await PopupSheet.AlertAsync(owner,"إتمام وردي",$"هل أتممت قراءة الصفحات {plan.StartPage} إلى {plan.EndPage}؟","أتممت القراءة","لاحقًا")) return;
            var overdue = plan.IsOverdue(DateOnly.FromDateTime(DateTime.Today));
            var celebrate = DailyWirdStore.Complete(); Refresh();
            if (overdue && !KhatmahStore.Linked) await PopupSheet.AlertAsync(owner,"ورد اليوم جاهز","أحسنت إتمام وردك السابق. جهّزنا لك ورد اليوم بنفس عدد الصفحات.","متابعة");
            else if (celebrate) await WirdCelebration.ShowAsync(owner);
        },true);
        var actions = new Grid { ColumnDefinitions = { new(GridLength.Star),new(GridLength.Star) },ColumnSpacing = 8 }; actions.Add(_open); actions.Add(_done,1);
        var reminders = new ReminderToggle { IsToggled = WirdReminders.Enabled,VerticalOptions = LayoutOptions.Center };
        reminders.Toggled += async (_,e)=> { WirdReminders.Enabled = e.Value; if (e.Value) await WirdReminders.RequestPermissionAsync(); Refresh(); };
        var reminderRow = new Grid { ColumnDefinitions = { new(GridLength.Star),new(GridLength.Auto) } };
        var caption = Ui.Label("تذكيري بالورد مساءً",13); caption.VerticalOptions = LayoutOptions.Center; reminderRow.Add(caption); reminderRow.Add(reminders,1);
        Add(heading); Add(_status); Add(_editor); Add(actions); Add(reminderRow); Refresh();
    }
    public void Refresh()
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var plan = DailyWirdStore.Load(); var complete = plan?.IsComplete(today) == true;
        _summary.Text = plan is null ? "ورد يناسب يومك" : $"الصفحات {plan.StartPage} — {plan.EndPage}";
        _sync = true; _start.SelectedPage = plan?.StartPage ?? 1; _end.MinimumPage = _start.SelectedPage; _end.SelectedPage = plan?.EndPage ?? 5; _sync = false;
        _quantity.Text = $"عدد صفحات الورد: {_end.SelectedPage-_start.SelectedPage+1}";
        _editor.IsVisible = true; _open.IsEnabled = plan is not null; _done.IsEnabled = plan is not null && !complete;
        _status.Text = complete ? "✓ أتممت ورد اليوم · الورد التالي متاح غدًا" : plan?.IsOverdue(today) == true ? "وردك السابق ينتظرك. أتممه لنجهّز ورد اليوم فورًا." : plan is null ? "اختر البداية والنهاية من القائمتين" : $"عدد الصفحات: {plan.EndPage-plan.StartPage+1} · تقدمك محفوظ تلقائيًا";
        if (KhatmahStore.Linked) { _editor.IsVisible = true; _start.IsEnabled = _end.IsEnabled = false; _status.Text = "وردك مرتبط بخطة الختمة"; }
    }
}
