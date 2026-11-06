using AlMithaq.Core.Logic;
using AlMithaq.Core.Models;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;

namespace AlMithaq.Features.Quran.Views;

public sealed record ReciterSelection(string Id, bool Download);

public sealed class ReciterSelectionPage : ContentPage
{
    private const string FavoritesKey = "reader.favorites";
    private readonly TaskCompletionSource<ReciterSelection?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly HashSet<string> _favorites;
    private readonly string _current;
    private readonly CollectionView _pinned = new() { SelectionMode = SelectionMode.None };
    private readonly CollectionView _all = new() { SelectionMode = SelectionMode.None };
    private readonly SearchBar _search = new() { Placeholder = "ابحث عن قارئ…", FontFamily = "Almarai-Regular", FontSize = 14 };
    private readonly Label _pinnedTitle = Ui.Label("قرّاؤك المفضّلون", 13, true);
    private readonly Label _count = Ui.Label("", 12);
    private readonly IReadOnlyList<QuranVerse> _verses;
    private readonly QuranMediaService _media;
    private bool _closing;
    private readonly Dictionary<string, string> _details = [];
    private readonly Dictionary<string, int> _saved = [];
    private sealed record Row(Reciter Reader, bool Favorite, bool Current, string DownloadText, bool CanDownload);

    public static async Task<ReciterSelection?> ShowAsync(ContentPage owner, string current, IReadOnlyList<QuranVerse> verses, QuranMediaService media)
    {
        var page = new ReciterSelectionPage(current, verses, media);
        await owner.Navigation.PushModalAsync(new NavigationPage(page) { BarBackgroundColor = Ui.Green, BarTextColor = Colors.White });
        return await page._result.Task;
    }
    private ReciterSelectionPage(string current, IReadOnlyList<QuranVerse> verses, QuranMediaService media)
    {
        _current = current; _verses = verses; _media = media;
        _favorites = Preferences.Get(FavoritesKey, "").Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        Ui.Page(this, "اختر القارئ"); NavigationPage.SetHasBackButton(this, false);
        Ui.ThemeProperty(_search, SearchBar.TextColorProperty, "AppInk");
        Ui.ThemeProperty(_search, SearchBar.PlaceholderColorProperty, "AppMuted");
        DataTemplate Template() => new(() =>
        {
            var name = Ui.Label("", 15, true); name.SetBinding(Label.TextProperty, "Reader.Name");
            var detail = Ui.Label("", 12); detail.SetBinding(Label.TextProperty, nameof(Row.DownloadText));
            var labels = Ui.Stack(name, detail); labels.Spacing = 3; labels.VerticalOptions = LayoutOptions.Center;
            var choose = new TapGestureRecognizer(); choose.Tapped += async (_, _) => { if (labels.BindingContext is Row row) await CloseAsync(new(row.Reader.Id, false)); }; labels.GestureRecognizers.Add(choose);
            var selected = Ui.Label("\uf058", 17); selected.FontFamily = "FontIconSolid"; selected.TextColor = Ui.Green; selected.SetBinding(IsVisibleProperty, nameof(Row.Current)); selected.VerticalOptions = LayoutOptions.Center; selected.InputTransparent = true;
            var star = Ui.Icon("\uf005", "تثبيت القارئ أو إزالته من المفضلة", () => Task.CompletedTask);
            star.BindingContextChanged += (_, _) => { if (star.BindingContext is Row row) star.TextColor = row.Favorite ? Ui.Gold : Color.FromArgb("#81968E"); };
            star.Clicked += (_, _) => { if (star.BindingContext is not Row row) return; if (!_favorites.Add(row.Reader.Id)) _favorites.Remove(row.Reader.Id); Preferences.Set(FavoritesKey, string.Join('|', _favorites)); Render(); };
            var download = Ui.Icon("\uf019", "تنزيل السورة الحالية بهذا الصوت", () => Task.CompletedTask);
            download.SetBinding(IsVisibleProperty, nameof(Row.CanDownload));
            download.Clicked += async (_, _) => { if (download.BindingContext is Row row) await CloseAsync(new(row.Reader.Id, true)); };
            var grid = new Grid { ColumnDefinitions = { new(GridLength.Star), new(24), new(44), new(44) }, ColumnSpacing = 2, Padding = new Thickness(10, 7), MinimumHeightRequest = 68 };
            star.WidthRequest = download.WidthRequest = 44; star.MinimumWidthRequest = download.MinimumWidthRequest = 44;
            grid.Add(labels); grid.Add(selected, 1); grid.Add(star, 2); grid.Add(download, 3);
            var card = Ui.Card(grid); card.Padding = 0; card.Margin = new Thickness(0, 0, 0, 5); return card;
        });
        _pinned.ItemTemplate = Template(); _all.ItemTemplate = Template();
        _all.EmptyView = Ui.Label("لا يوجد قارئ يطابق البحث", 14);
        var tip = Ui.Label("ثبّت قرّاءك بالنجمة ليبقوا في الأعلى. التنزيل يحفظ السورة الحالية فقط، والتلاوة عبر الإنترنت متاحة دون تنزيل.", 12);
        var layout = new Grid { Padding = new Thickness(12, 8), RowSpacing = 5, RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) } };
        layout.Add(_search); layout.Add(tip, 0, 1); layout.Add(_pinnedTitle, 0, 2); layout.Add(_pinned, 0, 3); layout.Add(_count, 0, 4); layout.Add(_all, 0, 5);
        Content = layout; _search.TextChanged += (_, _) => Render(); Render();
    }
    private void Render()
    {
        var query = ArabicSearch.Normalize(_search.Text ?? "");
        var rows = QuranMediaService.Reciters.Where(r => ArabicSearch.Normalize(r.Name).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(r => new Row(r, _favorites.Contains(r.Id), r.Id == _current, Detail(r), _verses.Count > 0 && Saved(r) < _verses.Count)).ToArray();
        var pinned = rows.Where(r => r.Favorite || r.Current).OrderByDescending(r => r.Current).ToArray();
        _pinned.ItemsSource = pinned; _pinned.HeightRequest = Math.Min(210, pinned.Length * 78); _pinned.IsVisible = _pinnedTitle.IsVisible = pinned.Length > 0;
        _all.ItemsSource = rows.Where(r => !r.Favorite && !r.Current).ToArray(); _count.Text = $"مكتبة التلاوات · {rows.Length} تسجيلًا";
    }
    private int Saved(Reciter r) => _saved.TryGetValue(r.Id, out var count) ? count : _saved[r.Id] = _verses.Count(v => _media.IsDownloaded(r.Id, v.Number));
    private string Detail(Reciter r)
    {
        if (_details.TryGetValue(r.Id, out var cached)) return cached;
        var saved = Saved(r);
        return _details[r.Id] = saved == _verses.Count && saved > 0 ? "السورة الحالية محفوظة دون إنترنت" : saved > 0 ? $"منزّل {saved} من {_verses.Count} آية من السورة" : "تلاوة آية بآية · استماع أو تنزيل السورة";
    }
    protected override bool OnBackButtonPressed() { _ = CloseAsync(null); return true; }
    protected override void OnDisappearing() { base.OnDisappearing(); if (!_closing) _result.TrySetResult(null); }
    private async Task CloseAsync(ReciterSelection? selection)
    {
        if (_closing) return; _closing = true;
        if (selection is not null) { _favorites.Add(selection.Id); Preferences.Set(FavoritesKey, string.Join('|', _favorites)); }
        try { await Navigation.PopModalAsync(); _result.TrySetResult(selection); }
        catch (Exception ex) { _result.TrySetException(ex); }
    }
}
