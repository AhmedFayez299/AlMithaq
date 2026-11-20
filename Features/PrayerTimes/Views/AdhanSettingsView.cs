using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Shared.Controls;

namespace AlMithaq.Features.PrayerTimes.Views;

public sealed class AdhanSettingsView : VerticalStackLayout
{
    private readonly Switch _enabled = new();
    private readonly Label _status = Ui.Label("", 11);
    private readonly List<(int Index, Border Card, Label Check, Border Circle)> _voices = [];
    private bool _sync;
    public AdhanSettingsView()
    {
        Spacing = 10;
        var title = Ui.Label("الأذان والتنبيهات", 16, true);
        var enableRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        enableRow.Add(title); enableRow.Add(_enabled, 1);
        Ui.ThemeProperty(_status, Label.TextColorProperty, "AppMuted");
        var voices = new VerticalStackLayout { Spacing = 6 };
        voices.Add(Ui.Label("اختر صوت الأذان", 13, true));
        foreach (var index in new[] { 2, 0, 1 })
        {
            var name = Ui.Label(AdhanSettings.Voices[index], 13, true); name.VerticalTextAlignment = TextAlignment.Center;
            var check = Ui.Label("", 17); check.FontFamily = "FontIconSolid"; check.VerticalTextAlignment = TextAlignment.Center;
            var row = new Grid { FlowDirection = FlowDirection.RightToLeft, ColumnDefinitions = { new(28), new(GridLength.Star) }, ColumnSpacing = 8, MinimumHeightRequest = 44 };
            check.HorizontalTextAlignment = TextAlignment.Center;
            var circle = new Border { WidthRequest = 19, HeightRequest = 19, Padding = 0,
                Stroke = new SolidColorBrush(Ui.Gold.WithAlpha(.75f)), StrokeThickness = 1,
                BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, InputTransparent = true };
            var indicator = new Grid { InputTransparent = true }; indicator.Add(circle); indicator.Add(check);
            row.Add(indicator); row.Add(name, 1);
            var card = Ui.Card(row); card.Padding = new Thickness(12, 4); card.Margin = 0;
            var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => { AdhanCoordinator.StopPreview(); AdhanSettings.Voice = index; AdhanCoordinator.Refresh(); Refresh(); };
            card.GestureRecognizers.Add(tap); SemanticProperties.SetDescription(card, AdhanSettings.Voices[index]);
            _voices.Add((index, card, check, circle)); voices.Add(card);
        }
        var preview = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48), new(48) }, ColumnSpacing = 6 };
        preview.Add(Ui.Label("معاينة الصوت المختار", 12));
        preview.Add(Ui.Icon("\uf04b", "معاينة صوت الأذان", () => { AdhanCoordinator.Preview(); return Task.CompletedTask; }), 1);
        preview.Add(Ui.Icon("\uf04d", "إيقاف المعاينة", () => { AdhanCoordinator.StopPreview(); return Task.CompletedTask; }), 2);
        voices.Add(preview);
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
#if IOS
        var note = Ui.Label("صوت الأذان مرفق دون إنترنت. إشعار iOS يستخدم مقطعًا قصيرًا؛ اضغط عليه لاستكمال الأذان داخل التطبيق، ولإيقافه استخدم زر إيقاف المعاينة. صوت الإشعار يتبع مستوى الرنين والتنبيهات. ألغِ الصامت وفعّل Sounds لإشعارات الميثاق. إذا انقطع الصوت عند اختفاء البانر، اختر نمط البانر «دائم» من إعدادات إشعارات الميثاق في الآيفون. شريط الصوت يضبط المعاينة فقط.", 10); Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
#else
        var note = Ui.Label("التسجيلات الثلاثة مرفقة وتعمل دون إنترنت. يتأثر الصوت بمستوى المنبّه ووضع عدم الإزعاج في الهاتف.", 10); Ui.ThemeProperty(note, Label.TextColorProperty, "AppMuted");
#endif
        var permissions = Ui.AsyncButton("مراجعة الأذونات وتفعيل الأذان", async () => { await AdhanCoordinator.EnableAsync(); Refresh(); }, true); permissions.FontSize = 12;
        Add(enableRow); Add(_status);
#if ANDROID
        var lastAt = Preferences.Get("adhan.last.scheduled", 0L); var startedAt = Preferences.Get("adhan.last.started", 0L);
        if (lastAt > 0 && startedAt >= lastAt)
        {
            var timing = Ui.Label($"آخر اختبار/أذان: بدأ الصوت بعد الموعد بـ{(startedAt-lastAt)/1000d:0.0} ثانية", 10);
            Ui.ThemeProperty(timing, Label.TextColorProperty, "AppMuted"); Add(timing);
        }
#endif
         Add(voices); Add(prayers); Add(volumeCaption); Add(volume); Add(note); Add(permissions);
        var test = Ui.AsyncButton("اختبار تنبيه بعد ١٠ ثوانٍ", async () =>
        {
            if (!AdhanSettings.Enabled) { _status.Text = "فعّل الأذان والأذونات أولًا"; return; }
            try
            {
#if IOS
                var scheduled = await AlMithaq.Platforms.iOS.LocalReminders.TestAsync("adhan", "اختبار تنبيه الصلاة");
                _status.Text = scheduled ? "جُدول الاختبار؛ يمكنك إغلاق الشاشة الآن" : "لم يُجدوَل الاختبار؛ راجع أذونات الإشعارات والصوت";
#else
                AdhanCoordinator.TestAlarm(); _status.Text = "جُدول الاختبار؛ يمكنك إغلاق الشاشة الآن";
#endif
            }
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

        Refresh();
    }
    public void Refresh() { _sync = true; try { _enabled.IsToggled = AdhanSettings.Enabled; _status.Text = AdhanCoordinator.Status;
        foreach (var voice in _voices)
        {
            var selected = voice.Index == AdhanSettings.Voice;
            voice.Check.Text = "\uf058";
            voice.Check.IsVisible = selected;
            voice.Circle.IsVisible = !selected;
            voice.Check.TextColor = selected ? Ui.Green : Ui.Gold;
            Ui.ThemeProperty(voice.Card, Border.BackgroundColorProperty, selected ? "AppSoft" : "AppCard");
            voice.Card.Stroke = new SolidColorBrush(selected ? Ui.Green : Ui.Gold.WithAlpha(.45f));
            voice.Card.StrokeThickness = .7;
        } } finally { _sync = false; } }
}

