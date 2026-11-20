using Microsoft.Maui.Controls.Shapes;
namespace AlMithaq.Shared.Controls;

public static class ScreenDesign
{
    public static Border Surface(View content, Thickness? padding = null)
    {
        var card = Ui.Card(content); card.Padding = padding ?? new Thickness(16); card.Margin = new Thickness(0,3);
        card.StrokeShape = new RoundRectangle { CornerRadius = 22 };
        card.Shadow = new Shadow { Brush = Color.FromArgb("#071F17"),Opacity = .08f,Radius = 9,Offset = new Point(0,3) };
        return card;
    }
    public static View Intro(string title,string subtitle,string glyph)
    {
        var heading = Ui.Label(title,20,true); heading.MaxLines = 2;
        var caption = Ui.Label(subtitle,12); Ui.ThemeProperty(caption,Label.TextColorProperty,"AppMuted");
        var text = Ui.Stack(heading,caption); text.Spacing = 3;
        var icon = Ui.Label(glyph,21); icon.FontFamily = "FontIconSolid"; icon.TextColor = Ui.Gold; icon.HorizontalTextAlignment = TextAlignment.Center; icon.VerticalTextAlignment = TextAlignment.Center;
        var box = new Border { Content = icon,WidthRequest = 50,HeightRequest = 50,Padding = 0,StrokeThickness = 0,StrokeShape = new RoundRectangle { CornerRadius = 17 },VerticalOptions = LayoutOptions.Center };
        Ui.ThemeProperty(box,Border.BackgroundColorProperty,"AppSoft");
        var row = new Grid { ColumnDefinitions = { new(GridLength.Star),new(50) },ColumnSpacing = 10,Margin = new Thickness(0,2,0,6) };
        row.Add(text); row.Add(box,1); return row;
    }
    public static View Section(string title,string glyph)
    {
        var icon = Ui.Label(glyph,14); icon.FontFamily = "FontIconSolid"; icon.TextColor = Ui.Gold; icon.VerticalOptions = LayoutOptions.Center;
        var row = Ui.Row(icon,Ui.Label(title,14,true)); row.Spacing = 7; row.Margin = new Thickness(0,5,0,2); return row;
    }
    public static Border Pill(Label label)
    {
        label.VerticalOptions = LayoutOptions.Center; label.HorizontalTextAlignment = TextAlignment.Center;
        var pill = new Border { Content = label,Padding = new Thickness(10,7),StrokeThickness = 0,StrokeShape = new RoundRectangle { CornerRadius = 13 } };
        Ui.ThemeProperty(pill,Border.BackgroundColorProperty,"AppSoft"); return pill;
    }
}
