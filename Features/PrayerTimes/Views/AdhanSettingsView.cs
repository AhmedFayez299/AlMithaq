using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Shared.Controls;

namespace AlMithaq.Features.PrayerTimes.Views;

public sealed class AdhanSettingsView : VerticalStackLayout
{
    private readonly Switch _enabled = new();
    private readonly Label _status = Ui.Label("", 11);
    private readonly ChoicePicker _voice = new() { Title = "صوت الأذان", ItemsSource = AdhanSettings.Voices, SelectedIndex = AdhanSettings.Voice };
    private bool _sync;
    public AdhanSettingsView()
    {
        Spacing = 10;
        var title = Ui.Label("الأذان والتنبيهات", 16, true);
        var enableRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        enableRow.Add(title); enableRow.Add(_enabled, 1);
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        var voices = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48), new(48) }, ColumnSpacing = 6 };
        voices.Add(_voice); voices.Add(Ui.Icon("\uf04b", "معاينة صوت الأذان", () => { AdhanCoordinator.Preview(); return Task.CompletedTask; }), 1);
        voices.Add(Ui.Icon("\uf04d", "إيقاف المعاينة", () => { AdhanCoordinator.StopPreview(); return Task.CompletedTask; }), 2);
        var prayers = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto) }, ColumnSpacing = 6, RowSpacing = 2 };
        for (var i = 0; i < 5; i++)
        {
            var index = i; var toggle = new Switch { IsToggled = AdhanSettings.ForPrayer(i), HorizontalOptions = LayoutOptions.Center };
            toggle.Toggled += (_, e) => { AdhanSettings.SetPrayer(index, e.Value); AdhanCoordinator.Refresh(); Refresh(); };
            var label = Ui.Label(AdhanSettings.Prayers[i], 11); label.HorizontalTextAlignment = TextAlignment.Center;
            var cell = Ui.Stack(label, toggle); cell.Spacing = 0; prayers.Add(cell, i % 3, i / 3);
        }
        var volume = new Slider { Minimum = 0, Maximum = 1, Value = AdhanSettings.Volume };
        var volumeCaption = Ui.Label($"مستوى الأذان · {AdhanSettings.Volume:P0}", 11);
        volume.ValueChanged += (_, e) => { AdhanSettings.Volume = e.NewValue; AdhanCoordinator.UpdateVolume(); volumeCaption.Text = $"مستوى الأذان · {e.NewValue:P0}"; };
        var note = Ui.Label("علي بن أحمد ملا وأحمد الحداد · الصوتان مرفقان ويعملان دون إنترنت. يتأثر الصوت بمستوى المنبّه ووضع عدم الإزعاج في الهاتف.", 10); Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
        var permissions = Ui.AsyncButton("مراجعة الأذونات وتفعيل الأذان", async () => { await AdhanCoordinator.EnableAsync(); Refresh(); }, true); permissions.FontSize = 12;
        Add(enableRow); Add(_status); Add(voices); Add(prayers); Add(volumeCaption); Add(volume); Add(note); Add(permissions);
        var test = Ui.AsyncButton("اختبار تنبيه بعد ١٠ ثوانٍ", async () =>
        {
            if (!AdhanSettings.Enabled) { _status.Text = "فعّل الأذان والأذونات أولًا"; return; }
            try { AdhanCoordinator.TestAlarm(); _status.Text = "جُدول الاختبار؛ يمكنك إغلاق الشاشة الآن"; }
            catch (Exception ex) { _status.Text = ex.Message; }
            await Task.CompletedTask;
        }, true); test.FontSize = 12; Add(test);
        _enabled.Toggled += async (_, e) =>
        {
            if (_sync) return;
            if (e.Value) await AdhanCoordinator.EnableAsync();
            else { AdhanSettings.Enabled = false; AdhanCoordinator.Stop(); AdhanCoordinator.Refresh(); }
            Refresh();
        };
        _voice.SelectedIndexChanged += (_, _) => { AdhanCoordinator.StopPreview(); AdhanSettings.Voice = _voice.SelectedIndex; AdhanCoordinator.Refresh(); Refresh(); };
        Refresh();
    }
    public void Refresh() { _sync = true; try { _enabled.IsToggled = AdhanSettings.Enabled; _status.Text = AdhanCoordinator.Status; } finally { _sync = false; } }
}
