using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Models;
using AlMithaq.Features.PrayerTimes.Services;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;
using System.Globalization;

namespace AlMithaq.Pages;

public sealed class SettingsPage : ContentPage
{
    private readonly LocationService _locations;
    private readonly QuranMediaService _media;
    private readonly IQuranRepository _quran;
    private readonly IReadingStore _reading;
    private readonly AlMithaq.Features.PrayerTimes.Views.AdhanSettingsView _adhan;
    private readonly AlMithaq.Features.Salawat.Views.SalawatSettingsView _salawat;
    private readonly List<SettingsSection> _sections = [];
    private readonly Label _storage = Ui.Label("", 12);
    private readonly Label _location = Ui.Label("", 12);
    public SettingsPage(LocationService locations, QuranMediaService media, IQuranRepository quran, IReadingStore reading)
    {
        Ui.Page(this, "الإعدادات"); _locations = locations; _media = media; _quran = quran; _reading = reading;
        var method = new ChoicePicker { Title = "طريقة حساب الصلاة", ItemsSource = PrayerSettings.Methods.Select(m => m.Name).ToArray(), SelectedIndex = PrayerSettings.MethodIndex };
        method.SelectedIndexChanged += (_, _) => { PrayerSettings.MethodIndex = Math.Max(0, method.SelectedIndex); AdhanCoordinator.Refresh(); };
        var hanafi = new Switch { IsToggled = Preferences.Get("prayer.hanafi", false) };
        hanafi.Toggled += (_, e) => { Preferences.Set("prayer.hanafi", e.Value); AdhanCoordinator.Refresh(); };
        var ramadan = new Switch { IsToggled = PrayerSettings.RamadanIsha };
        ramadan.Toggled += (_, e) => { Preferences.Set("prayer.ramadan", e.Value); AdhanCoordinator.Refresh(); };
        var adjustment = new Stepper { Minimum = -30, Maximum = 30, Increment = 1, Value = PrayerSettings.Adjustment };
        var adjustmentLabel = Ui.Label($"تصحيح عام للمواقيت: {PrayerSettings.Adjustment} دقيقة", 12);
        adjustment.ValueChanged += (_, e) => { Preferences.Set("prayer.adjustment", (int)e.NewValue); adjustmentLabel.Text = $"تصحيح عام للمواقيت: {e.NewValue:F0} دقيقة"; AdhanCoordinator.Refresh(); };
        var appearance = new ChoicePicker { Title = "مظهر التطبيق", ItemsSource = AppAppearance.Names, SelectedIndex = AppAppearance.Selection };
        var syncingAppearance = false;
        appearance.SelectedIndexChanged += (_, _) => { if (!syncingAppearance) AppAppearance.Apply(appearance.SelectedIndex); };
        AppAppearance.Changed += () => { syncingAppearance = true; try { appearance.SelectedIndex = AppAppearance.Selection; } finally { syncingAppearance = false; } };
        _salawat = new AlMithaq.Features.Salawat.Views.SalawatSettingsView();
        _adhan = new AlMithaq.Features.PrayerTimes.Views.AdhanSettingsView();
        var heading = Ui.Label("إعدادات تناسب يومك", 17, true);
        var caption = Ui.Label("المظهر والصلاة والصوت، في مكان واحد", 11); Ui.ThemeProperty(caption, Label.TextColorProperty, "AppMuted");
        SettingsSection Section(string title, string caption, string glyph, params View[] views)
        {
            var section = new SettingsSection(title, caption, glyph, views);
            section.Opening += selected => { foreach (var other in _sections) if (other != selected) other.SetExpanded(false); };
            _sections.Add(section); return section;
        }
        var salawatSection = Section("الصلاة على النبي", "وقت التذكير والصوت ومستواه", "\uf0f3", _salawat);
        salawatSection.Closed += AlMithaq.Features.Salawat.Services.SalawatCoordinator.StopPreview;
        var adhanSection = Section("الأذان والتنبيهات", "صوت الأذان وتنبيهات الصلاة", "\uf028", _adhan);
        adhanSection.Closed += AdhanCoordinator.StopPreview;
        var stack = Ui.Stack(heading, caption,
            Section("المظهر", "ألوان التطبيق والنمط المفضل", "\uf042", appearance), salawatSection, adhanSection,
            Section("حساب مواقيت الصلاة", "الموقع وطريقة الحساب وتصحيح الوقت", "\uf017", method,
                Ui.Row(Ui.Label("العصر وفق المذهب الحنفي", 12), hanafi),
                Ui.Row(Ui.Label("زيادة عشاء أم القرى ٣٠ دقيقة لرمضان", 11), ramadan),
                adjustmentLabel, adjustment, _location,
                Ui.AsyncButton("إدخال إحداثيات يدويًا", ManualLocationAsync, true),
                Ui.Label("يُحفظ موقعك على الجهاز. حدّثه مع توقيت الهاتف عند السفر، وراجع طريقة الحساب مع مسجدك المحلي.", 11)),
            Section("التلاوات والتنزيلات", "إدارة مساحة الصوت المحفوظ", "\uf019", _storage,
                Ui.AsyncButton("حذف التلاوات المنزّلة", ClearAudioAsync, true),
                Ui.Label("النص والعلامات لا تتأثر بالحذف. تحتاج التلاوات والتفسير الإنترنت أول مرة فقط.", 11)),
            Section("العلامات والملاحظات", "حفظ نسخة من علاماتك أو استعادتها", "\uf02e", Ui.AsyncButton("تصدير نسخة", ExportAsync, true), Ui.AsyncButton("استيراد نسخة", ImportAsync, true)),
            Section("شارك الميثاق", "أرسل رابط البرنامج لمن تحب", "\uf1e0", Ui.Label("لحظات تقربك... وتملأ يومك طمأنينة", 12), Ui.AsyncButton("مشاركة رابط البرنامج", ShareAppAsync, true)));
        stack.Padding = new Thickness(16, 12, 16, 24); stack.Spacing = 6;
        Content = new ScrollView { Content = stack };
    }
    protected override bool OnBackButtonPressed() => ChoicePicker.Dismiss(this) || base.OnBackButtonPressed();
    protected override void OnAppearing() { base.OnAppearing(); foreach (var section in _sections) section.SetExpanded(false); UpdateLabels(); _adhan.Refresh(); _salawat.Refresh(); }
    protected override void OnDisappearing() { AdhanCoordinator.StopPreview(); AlMithaq.Features.Salawat.Services.SalawatCoordinator.StopPreview(); base.OnDisappearing(); }
    private void UpdateLabels() { _storage.Text = $"المساحة المستخدمة للصوت: {_media.AudioBytes() / 1048576d:F1} MB"; _location.Text = _locations.Saved()?.Description ?? "لا يوجد موقع محفوظ"; }
    private static Task ShareAppAsync() => Share.Default.RequestAsync(new ShareTextRequest
    {
        Title = "الميثاق",
        Text = "الميثاق — رفيقك للقرآن والذكر\nلحظات تقربك... وتملأ يومك طمأنينة\n\nhttps://play.google.com/store/apps/details?id=com.companyname.almithaq"
    });
    private async Task ManualLocationAsync()
    {
        var lat = await DisplayPromptAsync("خط العرض", "قيمة من ‎-90 إلى 90. مثال القاهرة: 30.0444", "التالي", "إلغاء"); if (lat is null) return;
        var lon = await DisplayPromptAsync("خط الطول", "قيمة من ‎-180 إلى 180. مثال القاهرة: 31.2357", "حفظ", "إلغاء"); if (lon is null) return;
        if (!double.TryParse(AlMithaq.Core.Logic.NumberInput.Normalize(lat), NumberStyles.Float, CultureInfo.InvariantCulture, out var a) || !double.TryParse(AlMithaq.Core.Logic.NumberInput.Normalize(lon), NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
            || !double.IsFinite(a) || !double.IsFinite(b) || Math.Abs(a) > 90 || Math.Abs(b) > 180)
        { await DisplayAlertAsync("إحداثيات غير صحيحة", "اكتب أرقامًا صحيحة ضمن الحدود، واستخدم النقطة للفاصل العشري.", "حسنًا"); return; }
        _locations.SaveManual(a, b); UpdateLabels();
    }
    private async Task ClearAudioAsync()
    {
        if (!await DisplayAlertAsync("حذف التلاوات", "حذف ملفات الصوت المنزّلة فقط؟", "حذف", "إلغاء")) return;
        await _media.ClearAudioAsync(); UpdateLabels();
    }
    private async Task ExportAsync()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, "almithaq-bookmarks.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(_reading.GetBookmarks()));
        await Share.Default.RequestAsync(new ShareFileRequest("نسخة من علامات الميثاق", new ShareFile(path, "application/json")));
    }
    private async Task ImportAsync()
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "اختر ملف علامات الميثاق JSON" }); if (file is null) return;
        await using var stream = await file.OpenReadAsync();
        using var limited = new MemoryStream(); var buffer = new byte[8192];
        while (true) { var read = await stream.ReadAsync(buffer); if (read == 0) break; if (limited.Length + read > 64 * 1024 * 1024) throw new InvalidDataException("الملف كبير جدًا"); limited.Write(buffer, 0, read); }
        var marks = JsonSerializer.Deserialize<List<VerseBookmark>>(limited.ToArray()); var document = await _quran.LoadAsync();
        if (marks is null || marks.Count > 6236 || marks.Any(b => b is null || b.Surah is < 1 or > 114 || b.Ayah < 1 || b.Ayah > document.Chapters[b.Surah - 1].Verses.Count || b.Note is null || b.Note.Length > 1000))
        { await DisplayAlertAsync("ملف غير صالح", "تأكد من أن الملف نسخة علامات صحيحة من الميثاق.", "حسنًا"); return; }
        if (!await DisplayAlertAsync("استيراد العلامات", $"دمج {marks.Count} علامة؟ ملاحظة الملف ستحل محل الملاحظة الحالية للآية نفسها.", "دمج", "إلغاء")) return;
        _reading.MergeBookmarks(marks);
        await DisplayAlertAsync("تم الاستيراد", "العلامات متاحة الآن في قارئ القرآن.", "حسنًا");
    }
    private async Task ShowQuranLicenseAsync()
    {
        var document = await _quran.LoadAsync();
        await DisplayAlertAsync("Tanzil Quran Text License", document.CopyrightNotice, "إغلاق");
    }
}

