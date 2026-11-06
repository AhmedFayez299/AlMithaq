using AlMithaq.Shared.Controls;
namespace AlMithaq.Pages;

public sealed record DailyCard(string Text, string Source);
public sealed class DailyCardsView : VerticalStackLayout
{
    private readonly CarouselView _carousel;
    private readonly bool _quran;
    public DailyCardsView(string title, bool quran)
    {
        _quran = quran; Spacing = 2;
        var hint = Ui.Label("اسحب للاطلاع على بطاقات اليوم", 12); Ui.ThemeProperty(hint, Label.TextColorProperty, "AppMuted");
        Add(Ui.Label(title, 15, true)); Add(hint);
        _carousel = new CarouselView { HeightRequest = 280, Loop = false, PeekAreaInsets = 0, HorizontalOptions = LayoutOptions.Fill };
        _carousel.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal) { SnapPointsAlignment = SnapPointsAlignment.Center, SnapPointsType = SnapPointsType.MandatorySingle };
        _carousel.ItemTemplate = new DataTemplate(() =>
        {
            var text = Ui.Label("", quran ? 20 : 16); if (quran) text.FontFamily = "AmiriQuran";
            text.LineHeight = 1.25; text.SetBinding(Label.TextProperty, nameof(DailyCard.Text));
            var source = Ui.Label("", 12); source.SetBinding(Label.TextProperty, nameof(DailyCard.Source)); Ui.ThemeProperty(source, Label.TextColorProperty, "AppMuted");
            var content = Ui.Stack(text, source); content.Spacing = 12;
            var card = Ui.Card(new ScrollView { Content = content }); card.Margin = new Thickness(1, 5); card.Padding = 16; return card;
        });
        var indicators = new IndicatorView { IndicatorColor = Color.FromArgb("#AEC2B5"), SelectedIndicatorColor = Ui.Gold, IndicatorSize = 6, HorizontalOptions = LayoutOptions.Center };
        _carousel.IndicatorView = indicators;
        Add(_carousel); Add(indicators);
    }
    public void SetItems(IEnumerable<DailyCard> items)
    {
        var data = items.ToArray(); _carousel.ItemsSource = data; _carousel.Position = 0;
        _carousel.HeightRequest = Math.Clamp((_quran ? 120 : 100) + data.Select(d => d.Text.Length).DefaultIfEmpty(80).Max() * (_quran ? .65 : .55), _quran ? 210 : 190, _quran ? 310 : 280);
    }
}
