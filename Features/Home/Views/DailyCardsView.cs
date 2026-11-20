using AlMithaq.Shared.Controls;
namespace AlMithaq.Pages;

public sealed record DailyCard(string Text, string Source, bool IsQuran = false, int Surah = 0, int Ayah = 0, int DhikrId = 0);
public sealed class DailyCardsView : VerticalStackLayout
{
    private readonly CarouselView _carousel;
    public Func<DailyCard, Task>? SaveFavoriteAsync { get; set; }
    public DailyCardsView(string title, bool quran)
    {
        Spacing = 3;
        var heading = Ui.Label(title, 14, true);
        var previous = Ui.Icon("\uf054", "البطاقة السابقة", () => { Move(-1); return Task.CompletedTask; });
        var next = Ui.Icon("\uf053", "البطاقة التالية", () => { Move(1); return Task.CompletedTask; });
        _carousel = new CarouselView { HeightRequest = 136, Loop = false, PeekAreaInsets = 0 };
        _carousel.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal) { SnapPointsAlignment = SnapPointsAlignment.Center, SnapPointsType = SnapPointsType.MandatorySingle };
        _carousel.ItemTemplate = new DataTemplate(() =>
        {
            var text = Ui.Label("", 18); text.LineHeight = 1.25;
            text.SetBinding(Label.TextProperty, nameof(DailyCard.Text));
            text.Triggers.Add(new DataTrigger(typeof(Label)) { Binding = new Binding(nameof(DailyCard.IsQuran)), Value = true, Setters = { new Setter { Property = Label.FontFamilyProperty, Value = "AmiriQuran" } } });
            var source = Ui.Label("", 10); source.SetBinding(Label.TextProperty, nameof(DailyCard.Source));
            Ui.ThemeProperty(source, Label.TextColorProperty, "AppMuted");
            var content = Ui.Stack(text, source); content.Spacing = 5; content.Padding = new Thickness(4, 0);
            return new ScrollView { Content = content };
        });
        var indicators = new IndicatorView { IndicatorColor = Color.FromArgb("#B8B69B"), SelectedIndicatorColor = Ui.Gold, IndicatorSize = 5, HorizontalOptions = LayoutOptions.Center };
        _carousel.IndicatorView = indicators;
        var controls = new Grid { ColumnDefinitions = { new(36), new(GridLength.Star), new(36) } };
        controls.Add(previous); controls.Add(_carousel, 1); controls.Add(next, 2);
        var share = OutlineIcon("share", "مشاركة الآية أو الذكر", async () =>
        {
            if (_carousel.CurrentItem is DailyCard item) await Share.RequestAsync(new ShareTextRequest { Text = item.Text + "\n" + item.Source, Title = "الميثاق" });
        });
        var copy = OutlineIcon("bookmark", "حفظ الآية أو الذكر في المفضلة", async () =>
        {
            if (_carousel.CurrentItem is DailyCard item && SaveFavoriteAsync is not null) await SaveFavoriteAsync(item);
        });
        var footer = new Grid { ColumnDefinitions = { new(GridLength.Star), new(36), new(36) } };
        footer.Add(indicators); footer.Add(share, 1); footer.Add(copy, 2);
        var quote = Ui.Label("\uf10e", 17); quote.FontFamily = "FontIconSolid"; quote.TextColor = Ui.Gold; quote.VerticalTextAlignment = TextAlignment.Center;
        var header = new Grid { FlowDirection = FlowDirection.RightToLeft, ColumnSpacing = 6, ColumnDefinitions = { new(22), new(GridLength.Star) } }; header.Add(quote); header.Add(heading, 1);
        var body = Ui.Stack(header, controls, footer); body.Spacing = 2;
        var decorated = new Grid(); decorated.Add(new HomeCardOrnament()); decorated.Add(body);
        var card = Ui.Card(decorated);
        card.Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#06160F")), Opacity = .24f, Offset = new Point(0, 4), Radius = 9 }; card.Padding = 10; card.Margin = 0;
        Ui.ThemeProperty(card, Border.BackgroundColorProperty, "HomeCard");
        Ui.ThemeProperty(card, Border.StrokeProperty, "HomeStrokeBrush");
        Ui.ThemeProperty(card, Border.StrokeThicknessProperty, "HomeStrokeWidth");
        Add(card);
    }
    private static View OutlineIcon(string kind, string description, Func<Task> action)
    {
        var art = new HomeNavigationIcon(kind) { WidthRequest = 21, HeightRequest = 21, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var target = new Grid { WidthRequest = 36, HeightRequest = 36 }; target.Add(art);
        var tap = new TapGestureRecognizer(); tap.Tapped += async (_, _) => await action(); target.GestureRecognizers.Add(tap);
        SemanticProperties.SetDescription(target, description); return target;
    }
    private void Move(int delta)
    {
        var count = (_carousel.ItemsSource as DailyCard[])?.Length ?? 0;
        if (count > 0) _carousel.Position = Math.Clamp(_carousel.Position + delta, 0, count - 1);
    }
    public void SetItems(IEnumerable<DailyCard> items) { _carousel.ItemsSource = items.ToArray(); _carousel.Position = 0; }
}
