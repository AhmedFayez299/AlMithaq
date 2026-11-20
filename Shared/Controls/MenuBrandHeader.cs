using Microsoft.Maui.Controls.Shapes;
namespace AlMithaq.Shared.Controls;

public sealed class MenuBrandHeader : Border
{
    public MenuBrandHeader(Func<Task>? close = null)
    {
        Padding = 0; StrokeThickness = .6; Stroke = Ui.Gold.WithAlpha(.45f); StrokeShape = new RoundRectangle { CornerRadius = 23 };
        Background = new LinearGradientBrush(new GradientStopCollection { new(Ui.Green,0),new(Color.FromArgb("#103E30"),1) },new Point(0,0),new Point(1,1));
        var title = Ui.Label("الميثاق",23,true); title.TextColor = Colors.White;
        var subtitle = Ui.Label("رفيقك للقرآن والذكر",12); subtitle.TextColor = Color.FromArgb("#F0DDB3");
        var words = Ui.Stack(title,subtitle); words.Spacing = 1; words.VerticalOptions = LayoutOptions.Center; words.FlowDirection = FlowDirection.RightToLeft;
        var theme = Ui.ThemeToggle(); theme.TextColor = Colors.White; theme.WidthRequest = theme.HeightRequest = theme.MinimumWidthRequest = theme.MinimumHeightRequest = 36; theme.FontSize = 17;
        var grid = new Grid { FlowDirection = FlowDirection.LeftToRight,Padding = 12,HeightRequest = 128,ColumnSpacing = 9,
            ColumnDefinitions = { new(36),new(GridLength.Star),new(64) },RowDefinitions = { new(GridLength.Star),new(GridLength.Auto) } };
        if (close is null) grid.Add(theme);
        else { var exit = Ui.Icon("\uf00d","إغلاق القائمة",close); exit.TextColor = Colors.White; exit.WidthRequest = exit.HeightRequest = exit.MinimumWidthRequest = exit.MinimumHeightRequest = 36; grid.Add(exit); grid.Add(theme,0,1); }
        grid.Add(words,1);
        var logo = new Image { Source = "mithaqlogo.png",WidthRequest = 64,HeightRequest = 84,VerticalOptions = LayoutOptions.Center }; Grid.SetRowSpan(logo,2); grid.Add(logo,2);
        var caption = Ui.Label("لحظات تقرّبك وتملأ يومك طمأنينة",10); caption.TextColor = Color.FromArgb("#C9DED1"); caption.MaxLines = 2; caption.FlowDirection = FlowDirection.RightToLeft; grid.Add(caption,1,1);
        var layers = new Grid(); layers.Add(new HeaderBackdrop { Opacity = .18 }); layers.Add(grid); Content = layers;
    }
    public static View Group(string title)
    {
        var label = Ui.Label(title,10,true); Ui.ThemeProperty(label,Label.TextColorProperty,"AppMuted"); label.Margin = new Thickness(10,10,10,3); return label;
    }
}
