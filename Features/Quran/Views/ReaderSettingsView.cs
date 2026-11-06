using AlMithaq.Shared.Controls;

namespace AlMithaq.Features.Quran.Views;

public sealed class ReaderSettingsView : Grid
{
    private readonly Border _card;
    private readonly Action _refresh;
    public ReaderSettingsView(ChoicePicker mode, ChoicePicker theme, Slider font, Slider zoom, ChoicePicker reciter, ChoicePicker repeat, Switch follow, Switch continuous, Func<Task> download, Func<bool> canDownload, ChoicePicker frameStyle)
    {
        IsVisible = false; ZIndex = 50; BackgroundColor = Color.FromArgb("#99061C15"); Padding = new Thickness(12, 18); FlowDirection = FlowDirection.RightToLeft;
        var backdrop = new Button { BackgroundColor = Colors.Transparent, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
        backdrop.Clicked += (_, _) => Hide(); SemanticProperties.SetDescription(backdrop, "إغلاق إعدادات القراءة"); Add(backdrop);
        foreach (var picker in new[] { mode, theme, reciter, repeat }) { picker.WidthRequest = -1; picker.HorizontalOptions = LayoutOptions.Fill; }
        font.WidthRequest = zoom.WidthRequest = -1;
        var heading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        var title = Ui.Label("إعدادات القرآن", 17, true); title.VerticalOptions = LayoutOptions.Center;
        heading.Add(title); heading.Add(Ui.Icon("\uf00d", "إغلاق الإعدادات", () => { Hide(); return Task.CompletedTask; }), 1);
        View Field(string label, View control) { var stack = Ui.Stack(Ui.Label(label, 12), control); stack.Spacing = 2; return stack; }
        Grid Pair(View first, View second)
        {
            var grid = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10 }; grid.Add(first); grid.Add(second, 1); return grid;
        }
        Grid Toggle(string label, Switch control)
        {
            var row = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, MinimumHeightRequest = 48 };
            var text = Ui.Label(label, 14); text.VerticalOptions = LayoutOptions.Center; row.Add(text); row.Add(control, 1); return row;
        }
        var sizeTitle = Ui.Label("", 13, true); var sizeValue = Ui.Label("", 13); sizeValue.TextColor = Ui.Gold;
        var sizeHeading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Margin = new Thickness(0, 8, 0, 0) }; sizeHeading.Add(sizeTitle); sizeHeading.Add(sizeValue, 1);
        void Adjust(double sign)
        {
            var slider = mode.SelectedIndex == 3 ? zoom : font;
            slider.Value = Math.Clamp(slider.Value + sign * (mode.SelectedIndex == 3 ? .25 : 2), slider.Minimum, slider.Maximum);
        }
        var size = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, FlowDirection = FlowDirection.LeftToRight };
        size.Add(Ui.Icon("\uf010", "تصغير", () => { Adjust(-1); return Task.CompletedTask; })); size.Add(font, 1); size.Add(zoom, 1);
        size.Add(Ui.Icon("\uf00e", "تكبير", () => { Adjust(1); return Task.CompletedTask; }), 2);
        var modes = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto) }, ColumnSpacing = 8, RowSpacing = 8 };
        var modeButtons = new List<Button>();
        var modeNames = new[] { "سورة كاملة", "صفحة نصية", "جزء كامل", "صفحة المصحف" };
        for (var i = 0; i < 4; i++)
        {
            var index = i; var button = Ui.Button(modeNames[i], () => mode.SelectedIndex = index, true);
            button.FontSize = 13; button.MinimumHeightRequest = 50; modeButtons.Add(button); modes.Add(button, i % 2, i / 2);
        }
        var reading = Ui.Stack(Ui.Label("طريقة العرض", 13, true), modes, Field("لون الصفحة", theme), Field("إطار السورة", frameStyle), sizeHeading, size, Toggle("متابعة الآية تلقائيًا", follow)); reading.Spacing = 8;
        var reciterRow = new Grid { ColumnDefinitions = { new(GridLength.Star), new(48) }, ColumnSpacing = 8 };
        var downloadButton = Ui.Icon("\uf019", "تنزيل السورة الحالية بصوت القارئ المختار", async () => { Hide(); await download(); });
        reciterRow.Add(reciter); reciterRow.Add(downloadButton, 1);
        var audio = Ui.Stack(Field("القارئ", reciterRow), Pair(Field("التكرار", repeat), Field("تتابع الآيات", continuous)), Ui.Label("التلاوة تعمل بالإنترنت. رمز التنزيل يحفظ السورة الحالية للقارئ المختار للاستماع دون إنترنت؛ الصوت المحمّل يشغل مساحة بيانات على جهازك. اضبط مستوى الصوت من رمز السماعة.", 12)); audio.Spacing = 12;
        var readingTab = Ui.Button("القراءة", () => { }, true); var audioTab = Ui.Button("التلاوة", () => { }, true);
        var tabs = Pair(readingTab, audioTab);
        var content = Ui.Stack(reading, audio); content.Spacing = 0;
        var scroll = new ScrollView { Content = content, MaximumHeightRequest = 340 };
        var panel = Ui.Stack(heading, tabs, scroll); panel.Spacing = 10;
        _card = Ui.Card(panel); _card.Padding = 16; _card.Margin = 0; _card.VerticalOptions = LayoutOptions.End; _card.HorizontalOptions = LayoutOptions.Fill; _card.MaximumWidthRequest = 480; Add(_card);
        var audioSelected = false;
        void PaintTab(Button button, bool selected)
        {
            if (selected) { button.BackgroundColor = Ui.Green; button.TextColor = Colors.White; }
            else { Ui.ThemeProperty(button, Button.BackgroundColorProperty, "AppSoft"); Ui.ThemeProperty(button, Button.TextColorProperty, "AppInk"); }
        }
        void Refresh()
        {
            downloadButton.IsVisible = canDownload();
            for (var i = 0; i < modeButtons.Count; i++) PaintTab(modeButtons[i], i == mode.SelectedIndex);
            reading.IsVisible = !audioSelected; audio.IsVisible = audioSelected;
            font.IsVisible = mode.SelectedIndex != 3; zoom.IsVisible = mode.SelectedIndex == 3;
            sizeTitle.Text = mode.SelectedIndex == 3 ? "تكبير صفحة المصحف" : "حجم خط الآيات";
            sizeValue.Text = mode.SelectedIndex == 3 ? $"{zoom.Value * 100:F0}٪" : $"{font.Value:F0}";
            PaintTab(readingTab, !audioSelected); PaintTab(audioTab, audioSelected);
        }
        readingTab.Clicked += (_, _) => { audioSelected = false; Refresh(); };
        audioTab.Clicked += (_, _) => { audioSelected = true; Refresh(); };
        mode.SelectedIndexChanged += (_, _) => Refresh(); font.ValueChanged += (_, _) => Refresh(); zoom.ValueChanged += (_, _) => Refresh();
        AppAppearance.Changed += Refresh;
        SizeChanged += (_, _) => scroll.MaximumHeightRequest = Math.Clamp(Height - 180, 120, 340);
        _refresh = Refresh; Refresh();
    }
    public void Hide() => IsVisible = false;
    public async Task ShowAsync()
    {
        _refresh(); IsVisible = true; _card.Opacity = 0; _card.TranslationY = 24;
        await Task.WhenAll(_card.FadeToAsync(1, 180), _card.TranslateToAsync(0, 0, 200, Easing.CubicOut));
    }
}
