using System.Collections;
using AlMithaq.Core.Models;
namespace AlMithaq.Shared.Controls;
public sealed class ChoicePicker : ContentView
{
    public static bool Dismiss(ContentPage page) => PopupSheet.Dismiss(page);
    private readonly Button _button;
    private IList? _items;
    private int _selected = -1;
    private bool _open;
    public Func<Task<int?>>? SelectionProvider { get; set; }
    public string Title { get; set; } = "اختيار";
    public IList? ItemsSource { get => _items; set { _items = value; Refresh(); } }
    public object? SelectedItem => _items is not null && _selected >= 0 && _selected < _items.Count ? _items[_selected] : null;
    public int SelectedIndex { get => _selected; set { if (_selected == value) return; _selected = value; Refresh(); SelectedIndexChanged?.Invoke(this, EventArgs.Empty); } }
    public event EventHandler? SelectedIndexChanged;
    public ChoicePicker()
    {
        _button = Ui.AsyncButton("اختيار  ﹀", OpenAsync, true); _button.HorizontalOptions = LayoutOptions.Fill;
        Content = _button; MinimumHeightRequest = 48;
    }
    public void UseCompactLayout()
    {
        MinimumHeightRequest = HeightRequest = 44;
        _button.MinimumHeightRequest = _button.HeightRequest = 44;
        _button.Padding = new Thickness(10, 2); _button.FontSize = 13;
        _button.LineBreakMode = LineBreakMode.TailTruncation;
    }
    private static string Display(object? item) => item is QuranChapter chapter ? chapter.Display : item?.ToString() ?? "";
    private void Refresh() { if (_button is not null) _button.Text = $"{(SelectedItem is { } item ? Display(item) : Title)}  ﹀"; }
    private async Task OpenAsync()
    {
        if (_open || _items is null || _items.Count == 0) return;
        Element? parent = this; while (parent is not null && parent is not ContentPage) parent = parent.Parent;
        if (parent is not ContentPage page) return;
        _open = true;
        try
        {
            if (SelectionProvider is not null) { var chosen = await SelectionProvider(); if (chosen is { } selected) SelectedIndex = selected; return; }
            var options = _items.Cast<object>().Select(Display).ToArray();
            var result = await PopupSheet.ShowAsync(page, Title, options, _selected, selection: true);
            if (result is { } index) SelectedIndex = index;
        }
        finally { _open = false; }
    }
}
