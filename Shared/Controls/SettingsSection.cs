namespace AlMithaq.Shared.Controls;

public sealed class SettingsSection : ContentView
{
    private readonly View _body;
    private readonly Label _chevron;
    private readonly Button _hit;
    private readonly string _title;
    public bool IsExpanded { get; private set; }
    public event Action<SettingsSection>? Opening;
    public event Action? Closed;
    public SettingsSection(string title, string caption, string glyph, params View[] views)
    {
        _title = title;
        var icon = Ui.Label(glyph, 18); icon.FontFamily = "FontIconSolid";
        icon.HorizontalTextAlignment = TextAlignment.Center; icon.VerticalTextAlignment = TextAlignment.Center;
        var iconBox = Ui.Card(icon); iconBox.WidthRequest = iconBox.HeightRequest = 40;
        iconBox.Padding = 0; iconBox.Margin = 0; iconBox.StrokeThickness = 0;
        Ui.ThemeProperty(iconBox, Border.BackgroundColorProperty, "AppSoft");
        var hint = Ui.Label(caption, 12); Ui.ThemeProperty(hint, Label.TextColorProperty, "AppMuted");
        var words = Ui.Stack(Ui.Label(title, 15, true), hint); words.Spacing = 3; words.VerticalOptions = LayoutOptions.Center;
        _chevron = Ui.Label("\uf104", 15); _chevron.FontFamily = "FontIconSolid"; _chevron.VerticalTextAlignment = TextAlignment.Center;
        var row = new Grid { ColumnDefinitions = { new(40), new(GridLength.Star), new(20) }, ColumnSpacing = 11, Padding = new Thickness(14, 12), MinimumHeightRequest = 72 };
        row.Add(iconBox); row.Add(words, 1); row.Add(_chevron, 2);
        _hit = Ui.AsyncButton("", () => { SetExpanded(!IsExpanded); return Task.CompletedTask; });
        _hit.BackgroundColor = Colors.Transparent; _hit.Padding = 0;
        var header = new Grid(); header.Add(row); header.Add(_hit);
        var body = Ui.Stack(views); body.Spacing = 10; body.Padding = new Thickness(14, 0, 14, 16);
        _body = body; body.IsVisible = false;
        var stack = Ui.Stack(header, body); stack.Spacing = 0;
        var card = Ui.Card(stack); card.Padding = 0; Content = card; UpdateDescription();
    }
    public void SetExpanded(bool expanded)
    {
        if (expanded == IsExpanded) return;
        if (expanded) Opening?.Invoke(this);
        IsExpanded = expanded; _body.IsVisible = expanded; _chevron.Text = expanded ? "\uf107" : "\uf104";
        UpdateDescription(); if (!expanded) Closed?.Invoke();
    }
    private void UpdateDescription() => SemanticProperties.SetDescription(_hit, _title + (IsExpanded ? "، إغلاق القسم" : "، فتح القسم"));
}
