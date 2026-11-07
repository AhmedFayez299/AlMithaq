using AlMithaq.Features.Salawat.Services;
using AlMithaq.Shared.Controls;

namespace AlMithaq.Features.Salawat.Views;

public sealed class SalawatSettingsView : VerticalStackLayout
{
    private readonly Switch _enabled = new();
    private readonly Label _status = Ui.Label("", 12);
    private bool _sync;
    public SalawatSettingsView()
    {
        Spacing = 8;
        var title = Ui.Label("الصلاة على النبي ﷺ", 15, true);
        var heading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; heading.Add(title); heading.Add(_enabled, 1); Add(heading);
        var interval = new ChoicePicker { Title = "تكرار التذكير", ItemsSource = SalawatSettings.IntervalNames, SelectedIndex = SalawatSettings.IntervalIndex };
        interval.SelectedIndexChanged += (_, _) => { SalawatSettings.IntervalIndex = interval.SelectedIndex; SalawatCoordinator.Refresh(); Refresh(); };
        Add(interval);
        var start = new TimePicker { Time = TimeSpan.FromMinutes(SalawatSettings.Start), Format = "hh:mm tt", FontFamily = "Almarai-Regular", FontSize = 14 };
        var end = new TimePicker { Time = TimeSpan.FromMinutes(SalawatSettings.End), Format = "hh:mm tt", FontFamily = "Almarai-Regular", FontSize = 14 };
        foreach (var picker in new[] { start, end }) Ui.ThemeProperty(picker, TimePicker.TextColorProperty, "AppInk");
        var times = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 12 }; times.Add(Ui.Stack(Ui.Label("من", 12), start)); times.Add(Ui.Stack(Ui.Label("حتى", 12), end), 1); Add(times);
        start.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time) && start.Time is { } time) { SalawatSettings.Start = (int)time.TotalMinutes; SalawatCoordinator.Refresh(); Refresh(); } };
        end.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(TimePicker.Time) && end.Time is { } time) { SalawatSettings.End = (int)time.TotalMinutes; SalawatCoordinator.Refresh(); Refresh(); } };
        var sound = new Switch { IsToggled = SalawatSettings.Sound };
        sound.Toggled += (_, e) => { SalawatSettings.Sound = e.Value; SalawatCoordinator.Refresh(); Refresh(); };
        var soundRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(44), new(44) } }; soundRow.Add(Ui.Label("التذكير بصوت", 13)); soundRow.Add(sound, 1);
        soundRow.Add(Ui.Icon("\uf04b", "معاينة صوت الصلاة على النبي", () => { SalawatCoordinator.Preview(); return Task.CompletedTask; }), 2);
        soundRow.Add(Ui.Icon("\uf04d", "إيقاف معاينة التذكير", () => { SalawatCoordinator.StopPreview(); return Task.CompletedTask; }), 3); Add(soundRow);
        var volume = new Slider { Minimum = 0, Maximum = 1, Value = SalawatSettings.Volume };
        var volumeText = Ui.Label($"مستوى صوت التذكير · {SalawatSettings.Volume:P0}", 12);
        volume.ValueChanged += (_, e) => { SalawatSettings.Volume = e.NewValue; SalawatCoordinator.UpdateVolume(); volumeText.Text = $"مستوى صوت التذكير · {e.NewValue:P0}"; };
        Add(volumeText); Add(volume);
#if IOS
        Add(Ui.Label("صوت بشري قصير يعمل دون إنترنت. على الآيفون يتبع صوت الإشعار مستوى الرنين والتنبيهات؛ ألغِ الوضع الصامت وفعّل Sounds من إعدادات إشعارات الميثاق. قد يكتمه وضع التركيز. شريط الصوت هنا يضبط المعاينة فقط.", 12));
#else
        Add(Ui.Label("صوت بشري قصير يعمل دون إنترنت، مستقل عن صوت الوسائط والرنين الصامت. يُضبط صوت المنبّه مؤقتًا أثناء التذكير ثم يُعاد لمستواه السابق. أثناء «عدم الإزعاج» اسمح بالمنبّهات من إعدادات الهاتف.", 12));
#endif
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted"); Add(_status);
#if IOS
        var test = Ui.AsyncButton("اختبار التذكير بعد ١٠ ثوانٍ", async () =>
        {
            var scheduled = await AlMithaq.Platforms.iOS.LocalReminders.TestAsync("salawat", "اختبار تذكير الصلاة على النبي", SalawatSettings.Sound);
            _status.Text = scheduled ? "جُدول الاختبار؛ يمكنك إغلاق الشاشة الآن" : "لم يُجدوَل الاختبار؛ راجع أذونات الإشعارات والصوت";
        }, true); test.FontSize = 13; Add(test);
#else
        var test = Ui.AsyncButton("اختبار التذكير بعد ١٠ ثوانٍ", () => { SalawatCoordinator.Test(); _status.Text = "جُدول الاختبار؛ يمكنك إغلاق الشاشة الآن"; return Task.CompletedTask; }, true); test.FontSize = 13; Add(test);
#endif
        _enabled.Toggled += async (_, e) =>
        {
            if (_sync) return;
            if (e.Value) await SalawatCoordinator.EnableAsync();
            else { SalawatSettings.Enabled = false; SalawatCoordinator.Refresh(); }
            Refresh();
        };
        Refresh();
    }
    public void Refresh() { _sync = true; try { _enabled.IsToggled = SalawatSettings.Enabled; _status.Text = SalawatCoordinator.Status; } finally { _sync = false; } }
}
