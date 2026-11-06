using AlMithaq.Core.Abstractions;
using AlMithaq.Core.Models;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Shared.Controls;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;

namespace AlMithaq.Pages;

public partial class QuranPage : ContentPage
{
    private readonly IQuranRepository _repository;
    private readonly IReadingStore _store;
    private readonly QuranMediaService _media;
    private QuranDocument? _document;
    private Button? _downloadSurah;
    private readonly Grid _readingArea = new();
    private Grid _readerLayout = null!;
    private Grid? _playbackSheet;
    private View _readerTop = null!, _readerBottom = null!, _volumePopup = null!;
    private AlMithaq.Features.Quran.Views.QuranIndexView? _index;
    private IReadOnlyList<AlMithaq.Core.Logic.QuarterStart> _quarters = [];
    private bool _readerOpen, _chromeVisible, _returningModal;
    private bool _sectionEntry = true;
    public void PrepareSectionEntry() => _sectionEntry = true;
    private Border? _verseBar;
    private double _verseAnchor = .5;
    private readonly ChoicePicker _chapter = new() { Title = "السورة", WidthRequest = 240 };
    private readonly ChoicePicker _reciter = new() { Title = "القارئ", WidthRequest = 180 };
    private readonly ChoicePicker _mode = new() { Title = "طريقة عرض القرآن", ItemsSource = new[] { "سورة", "صفحة نصية", "جزء", "مصحف كامل" }, SelectedIndex = 3, WidthRequest = 130 };
    private readonly ChoicePicker _theme = new() { Title = "خلفية القراءة", ItemsSource = new[] { "ورقي", "فاتح", "ليلي" }, WidthRequest = 95 };
    private readonly ChoicePicker _repeat = new() { Title = "عدد مرات التلاوة", ItemsSource = new[] { "مرة", "٣ مرات", "٥ مرات", "١٠ مرات" }, SelectedIndex = 0, WidthRequest = 95 };
    private readonly Switch _continuous = new() { IsToggled = true };
    private readonly Slider _font = new() { Minimum = 18, Maximum = 64, WidthRequest = 150 };
    private readonly Slider _zoom = new() { Minimum = 1, Maximum = 3.5, Value = 1, WidthRequest = 145 };
    private readonly SearchBar _search = new() { Placeholder = "ابحث في القرآن دون تشكيل — حرفان على الأقل" };
    private readonly Label _status = Ui.Label("جارٍ فتح المصحف…", 12);
    private readonly Label _playing = Ui.Label("اختر آية للاستماع", 12);
    private readonly CollectionView _list = new() { SelectionMode = SelectionMode.None };
    private readonly MediaElement _player = new() { HeightRequest = 1, Opacity = 0, InputTransparent = true, ShouldAutoPlay = true, ShouldShowPlaybackControls = false };
    private readonly Switch _follow = new() { IsToggled = true };
    private readonly AlMithaq.Features.Quran.Views.MushafPageView _mushaf = new();
    private readonly AlMithaq.Features.Quran.Views.ReaderSettingsView _settingsSheet;
    private Dictionary<string, int>? _printedPages;
    private int ReadingPage(QuranVerse verse) => _mode.SelectedIndex == 3 && _printedPages is not null ? _printedPages[verse.Key] : verse.Page;
    private readonly Slider _seek = new() { Minimum = 0, Maximum = 1, Value = 0, ThumbColor = Ui.Green, MinimumTrackColor = Ui.Green };
    private readonly Label _elapsed = Ui.Label("00:00", 11);
    private readonly Label _duration = Ui.Label("00:00", 11);
    private readonly AlMithaq.Core.Logic.PlaybackGate _playbackGate = new();
    private long _playbackToken;
    private bool _advancing, _draggingSeek, _pauseRequested;
    private bool _matchingAppTheme;
    private int _firstVisible = -1, _lastVisible = -1;
    private Button _playToggle = null!;
    private Button _cancelDownload = null!;
    private readonly ProgressBar _progress = new() { IsVisible = false, ProgressColor = Ui.Gold };
    private List<QuranVerse> _shown = [];
    private List<VerseItem> _rows = [];
    private CancellationTokenSource? _lifetime, _searchCancellation, _download;
    private bool _changing, _active, _sequence;
    private QuranVerse? _current, _rangeStart;
    private int _repeatIndex, _cycles;
    private int? _customRangeRepeats;
    private int _customVerseRepeats = 1;
    private bool _onlySelectedRange = true;
    private int? _rangeEnd;
    private string ReciterId => QuranMediaService.Reciters[Math.Max(0, _reciter.SelectedIndex)].Id;
    private IEnumerable<QuranVerse> All => _document!.Chapters.SelectMany(s => s.Verses);

    public QuranPage(IQuranRepository repository, IReadingStore store, QuranMediaService media)
    {
        InitializeComponent(); Ui.Page(this, "القرآن الكريم"); _repository = repository; _store = store; _media = media;
        _font.Value = Math.Clamp(Preferences.Get("reader.text-font", Math.Min(Preferences.Get("reader.font", 24d), 24d)), 18, 64);
        _player.Volume = 1;
        _reciter.ItemsSource = QuranMediaService.Reciters.Select(r => r.Name).ToArray();
        var oldReaders = new[] { "ar.alafasy", "ar.husary", "ar.minshawi" };
        var savedReader = Preferences.Get("reader.reciter.id", oldReaders[Math.Clamp(Preferences.Get("reader.reciter", 0), 0, 2)]);
        _reciter.SelectedIndex = Math.Max(0, Array.FindIndex(QuranMediaService.Reciters, r => r.Id == savedReader));
        _theme.SelectedIndex = Math.Clamp(Preferences.Get("reader.theme", 0), 0, 2); ApplyTheme();
        _list.ItemTemplate = new DataTemplate(() =>
        {
            var text = new Label { ClassId = "verse-text", FontFamily = "AmiriQuran", LineHeight = 1.2, Margin = new Thickness(0, 8), HorizontalTextAlignment = TextAlignment.Start };
            text.SetBinding(Label.TextColorProperty, nameof(VerseItem.Ink));
            text.SetBinding(Label.TextProperty, nameof(VerseItem.DisplayText));
            text.SetBinding(Label.FontSizeProperty, new Binding(nameof(Slider.Value), source: _font));
            var reference = Ui.Label("", 11); reference.SetBinding(Label.TextProperty, "Verse.Reference");
            reference.ClassId = "verse-reference"; reference.TextColor = (Color)Resources["ReaderMuted"];
            var marker = Ui.Label("", 12, true); marker.TextColor = Ui.Gold; marker.SetBinding(Label.TextProperty, nameof(VerseItem.Marker));
            marker.SetBinding(IsVisibleProperty, nameof(VerseItem.HasMarker));
            var action = Ui.Icon("\uf141", "خيارات الآية", async () => { if (text.BindingContext is VerseItem item) await ActionsAsync(item); });
            var listen = Ui.Icon("\uf04b", "تشغيل أو إيقاف الآية", async () =>
            {
                if (text.BindingContext is not VerseItem item) return;
                DismissKeyboard(_search);
                if (item.IsPlaying) { StopPlayback("verse stopped"); return; }
                _rangeEnd = null;
                await PlayAsync(item.Verse, true);
            });
            listen.SetBinding(Button.TextProperty, nameof(VerseItem.PlayGlyph));
            action.ClassId = "verse-action"; action.TextColor = (Color)Resources["ReaderMuted"];
            listen.ClassId = "verse-action"; listen.TextColor = (Color)Resources["ReaderMuted"];
            var heading = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
            heading.Add(reference); heading.Add(listen, 1); heading.Add(action, 2);
            var body = Ui.Stack(heading, text, marker); body.Spacing = 2;
            var card = Ui.Card(body); card.Padding = new Thickness(16, 4, 16, 12); card.Margin = new Thickness(14, 4);
            card.ClassId = "verse-card"; card.BackgroundColor = (Color)Resources["ReaderCard"];
            card.Stroke = (Brush)Resources["ReaderStroke"]; return card;
        });
        var frameStyle = new ChoicePicker { Title = "إطار المصحف", ItemsSource = new[] { "الأرابيسك الأصلي", "مصحفي كلاسيكي", "خط ذهبي رفيع", "إطار الصفحة المزخرف" }, SelectedIndex = Math.Clamp(Preferences.Get("reader.frame.style", 0), 0, 3) };
        _ = _mushaf.SetFrameStyleAsync(frameStyle.SelectedIndex);
        frameStyle.SelectedIndexChanged += async (_, _) => await GuardAsync(async () =>
        {
            Preferences.Set("reader.frame.style", frameStyle.SelectedIndex);
            await _mushaf.SetFrameStyleAsync(frameStyle.SelectedIndex);
        });
        _settingsSheet = new(_mode, _theme, _font, _zoom, _reciter, _repeat, _follow, _continuous, DownloadAsync, CanDownloadCurrent, frameStyle);
        _chapter.WidthRequest = -1; _chapter.HorizontalOptions = LayoutOptions.Fill;
        _search.IsVisible = false;
        var title = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) } };
        title.Add(_chapter);
        title.Add(Ui.Icon("\uf002", "البحث في القرآن", () =>
        {
            _search.IsVisible = !_search.IsVisible;
            if (_search.IsVisible) Dispatcher.Dispatch(() => { if (_search.IsVisible) _search.Focus(); });
            else if (_document is not null) { var anchor = _shown.FirstOrDefault() ?? All.First(); ClearSearch(); Show(anchor); }
            return Task.CompletedTask;
        }), 1);
        title.Add(Ui.Icon("\uf1de", "إعدادات القراءة والصوت", _settingsSheet.ShowAsync), 2);
        var toolbar = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto) } };
        toolbar.Add(Ui.Icon("\uf053", "القسم السابق", () => MoveAsync(-1)), 0);
        toolbar.Add(Ui.Icon("\uf054", "القسم التالي", () => MoveAsync(1)), 1);
        toolbar.Add(_status, 2); _status.VerticalOptions = LayoutOptions.Center; _status.HorizontalTextAlignment = TextAlignment.Center;
        toolbar.Add(Ui.Icon("\uf05b", "الانتقال إلى آية أو صفحة", JumpAsync), 3);
        toolbar.Add(Ui.Icon("\uf02e", "العلامات المحفوظة", BookmarksAsync), 4);
        toolbar.Add(Ui.Icon("\uf1da", "آخر قراءة", ResumeAsync), 5);
        var top = Ui.Stack(title, _search, toolbar); top.Padding = new Thickness(10, 0); top.Spacing = 0;
        _playToggle = Ui.Icon("\uf04b", "تشغيل أو إيقاف التلاوة مؤقتًا", TogglePlaybackAsync, true);
        var playback = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 6, FlowDirection = FlowDirection.LeftToRight };
        _downloadSurah = Ui.Icon("\uf019", "تنزيل تلاوة السورة", DownloadAsync); playback.Add(_downloadSurah, 0);
        playback.Add(Ui.Icon("\uf048", "الآية السابقة", () => StepAudioAsync(-1)), 1);
        playback.Add(_playToggle, 2);
        playback.Add(Ui.Icon("\uf051", "الآية التالية", () => StepAudioAsync(1)), 3);
        playback.Add(Ui.Icon("\uf04d", "إيقاف التلاوة", () => { StopPlayback("user stop"); return Task.CompletedTask; }), 4);
        playback.ColumnDefinitions.Add(new(GridLength.Auto));
        playback.Add(Ui.Icon("\uf013", "إعدادات تشغيل المقطع", PlaybackSettingsAsync), 5);
        var timeline = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, FlowDirection = FlowDirection.LeftToRight, ColumnSpacing = 6 };
        timeline.Add(_elapsed); timeline.Add(_seek, 1); timeline.Add(_duration, 2);
        _playing.HorizontalTextAlignment = TextAlignment.Center;
        _cancelDownload = Ui.Icon("\uf00d", "إلغاء التنزيل الجاري", () => { _download?.Cancel(); return Task.CompletedTask; });
        _cancelDownload.IsVisible = false;
        var downloadProgress = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        downloadProgress.Add(_progress); downloadProgress.Add(_cancelDownload, 1);
        _progress.VerticalOptions = LayoutOptions.Center;
        var volume = new Slider { Minimum = 0, Maximum = 1, Value = MediaVolume.Current, MinimumTrackColor = Ui.Green, ThumbColor = Ui.Green };
        var volumeValue = Ui.Label($"{volume.Value:P0}", 11);
        volume.Rotation = -90; volume.FlowDirection = FlowDirection.LeftToRight;
        var volumeTrack = new AbsoluteLayout { WidthRequest = 56, HeightRequest = 176 };
        AbsoluteLayout.SetLayoutBounds(volume, new Rect(-52, 66, 160, 44)); volumeTrack.Add(volume);
        volumeValue.HorizontalTextAlignment = TextAlignment.Center;
        var volumeBody = Ui.Stack(volumeValue, volumeTrack); volumeBody.Spacing = 0;
        var volumeRow = Ui.Card(volumeBody); volumeRow.Padding = 6; volumeRow.Margin = new Thickness(10, 0, 10, 8);
        volumeRow.IsVisible = false; volumeRow.HorizontalOptions = LayoutOptions.End; volumeRow.VerticalOptions = LayoutOptions.End; volumeRow.ZIndex = 25;
        volume.ValueChanged += (_, e) => { MediaVolume.Set(e.NewValue); volumeValue.Text = $"{e.NewValue:P0}";
#if !ANDROID
            _player.Volume = e.NewValue;
#endif
        };
        var nowPlaying = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        nowPlaying.Add(_playing); _playing.FontSize = 12; _playing.MaxLines = 1; _playing.VerticalOptions = LayoutOptions.Center;
        nowPlaying.Add(Ui.Icon("\uf028", "مستوى صوت التلاوة", () => { volume.Value = MediaVolume.Current; volumeRow.IsVisible = !volumeRow.IsVisible; return Task.CompletedTask; }), 1);
        nowPlaying.Remove(_playing); var info = Ui.Stack(_playing, timeline); info.Spacing = 0; nowPlaying.Add(info);
        var panelContent = Ui.Stack(nowPlaying, playback, downloadProgress); panelContent.Spacing = 0;
        timeline.HeightRequest = 22; _playToggle.WidthRequest = _playToggle.HeightRequest = 48;
        var playerPanel = Ui.Card(panelContent);
        playerPanel.Padding = new Thickness(10, 0); playerPanel.Margin = new Thickness(8, 0, 8, 3);
        var bottom = Ui.Stack(playerPanel); bottom.Spacing = 0;
        var grid = new Grid { ClassId = "choice-host", RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        var readingArea = _readingArea; readingArea.Add(_list); readingArea.Add(_mushaf); readingArea.Add(volumeRow);
        readingArea.SizeChanged += (_, _) => HideVerseBar();
        grid.Add(top); grid.Add(readingArea, 0, 1); grid.Add(bottom, 0, 2); Grid.SetRowSpan(_settingsSheet, 3); grid.Add(_settingsSheet);
        Grid.SetRowSpan(_player, 3); grid.Add(_player); _player.InputTransparent = true; _player.VerticalOptions = LayoutOptions.End;
        _readerLayout = grid; _readerTop = top; _readerBottom = bottom; _volumePopup = volumeRow; Content = grid;
        _chapter.SelectionProvider = () => { ShowLibrary(); return Task.FromResult<int?>(null); };
        _mushaf.ChromeTapped += (_, _) => { if (!_active || !_readerOpen) return; SetChrome(!_chromeVisible); };
        _mushaf.VerseTapped += async (_, reference) => await GuardAsync(async () =>
        {
            if (_document is null || !_active || !_readerOpen) return;
            var verse = All.FirstOrDefault(v => v.Surah == reference.Surah && v.Ayah == reference.Ayah);
            if (verse is not null) await ActionsAsync(_rows.FirstOrDefault(r => r.Verse.Number == verse.Number) ?? new VerseItem(verse), reference.Y);
        });
        _mushaf.PageSwiped += async (_, delta) => { if (_active && _readerOpen) await GuardAsync(() => MoveAsync(delta)); };
        _mushaf.ZoomChanged += (_, zoom) => _zoom.Value = zoom;
#if ANDROID
        AlMithaq.Platforms.Android.CollectionPinch.Attach(_list,
            scale => _font.Value = Math.Clamp(_font.Value * scale, _font.Minimum, _font.Maximum),
            async direction => { if (string.IsNullOrWhiteSpace(_search.Text)) await GuardAsync(() => MoveAsync(direction)); });
#else
        foreach (var direction in new[] { SwipeDirection.Left, SwipeDirection.Right })
        {
            var swipe = new SwipeGestureRecognizer { Direction = direction, Threshold = 75 };
            swipe.Swiped += async (_, e) => await GuardAsync(() => MoveAsync(e.Direction == SwipeDirection.Right ? 1 : -1)); _list.GestureRecognizers.Add(swipe);
        }
        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += (_, e) => { if (e.Status == GestureStatus.Running) _font.Value = Math.Clamp(_font.Value * e.Scale, _font.Minimum, _font.Maximum); };
        _list.GestureRecognizers.Add(pinch);
#endif
        _zoom.ValueChanged += async (_, e) => await _mushaf.SetZoomAsync(e.NewValue);
        _chapter.SelectedIndexChanged += (_, _) =>
        {
            if (!_changing && _document is not null && _chapter.SelectedIndex >= 0) { ClearSearch(); Show(_document.Chapters[_chapter.SelectedIndex].Verses[0]); }
        };
        _mode.SelectedIndexChanged += (_, _) => { if (!_changing && _shown.Count > 0) { ClearSearch(); Show(_shown[0]); } };
        _theme.SelectedIndexChanged += (_, _) =>
        {
            Preferences.Set("reader.theme", _theme.SelectedIndex);
            var appearance = _theme.SelectedIndex == 2 ? 2 : _theme.SelectedIndex == 0 ? 3 : 1;
            if (!_matchingAppTheme && AppAppearance.Selection != appearance) { AppAppearance.Apply(appearance); return; }
            ApplyTheme();
        };
        _font.ValueChanged += (_, _) => Preferences.Set("reader.text-font", _font.Value);
        _reciter.SelectionProvider = async () =>
        {
            _settingsSheet.Hide(); HideVerseBar();
            _returningModal = true;
            var choice = await AlMithaq.Features.Quran.Views.ReciterSelectionPage.ShowAsync(this, ReciterId, _document?.Chapters[Math.Max(0, _chapter.SelectedIndex)].Verses ?? [], _media);
            if (choice is null) return null;
            var index = Array.FindIndex(QuranMediaService.Reciters, r => r.Id == choice.Id);
            if (index < 0) return null;
            _reciter.SelectedIndex = index;
            if (choice.Download) await DownloadAsync();
            return index;
        };
        _reciter.SelectedIndexChanged += (_, _) => { StopPlayback("reciter changed"); Preferences.Set("reader.reciter.id", ReciterId); UpdateDownloadButton(); };
        _search.TextChanged += async (_, e) => { if (!_changing) await SearchAsync(e.NewTextValue ?? ""); };
        _list.Scrolled += (_, e) =>
        {
            _firstVisible = e.FirstVisibleItemIndex; _lastVisible = e.LastVisibleItemIndex;
            if (e.FirstVisibleItemIndex >= 0 && e.FirstVisibleItemIndex < _shown.Count && string.IsNullOrWhiteSpace(_search.Text))
                _store.SavePosition(_shown[e.FirstVisibleItemIndex]);
        };
        _player.MediaEnded += (_, _) =>
        {
            // ExoPlayer updates its old source state after raising MediaEnded. Defer source replacement until that callback returns.
            var token = _playbackToken;
            Dispatcher.Dispatch(async () => await GuardAsync(() => AdvanceAsync(token)));
        };
        _player.StateChanged += (_, _) =>
        {
            StartupTrace.Write("Audio state: " + _player.CurrentState);
            if (_sequence && !_pauseRequested && _player.CurrentState == MediaElementState.Playing) _playbackGate.Playing(_playbackToken);
            _playToggle.Text = _sequence && !_pauseRequested && _player.CurrentState is MediaElementState.Playing or MediaElementState.Buffering ? "\uf04c" : "\uf04b";
            UpdateTimeline();
        };
        _player.PositionChanged += (_, _) => UpdateTimeline();
        _seek.DragStarted += (_, _) => _draggingSeek = true;
        _seek.DragCompleted += async (_, _) =>
        {
            try { if (_sequence) await GuardAsync(async () => await _player.SeekTo(TimeSpan.FromSeconds(_seek.Value))); }
            finally { _draggingSeek = false; UpdateTimeline(); }
        };
        _player.MediaFailed += (_, _) =>
        {
            if (!_sequence) return;
            StopPlayback("media failed");
            _playing.Text = "تعذر تشغيل التسجيل. تحقق من الاتصال أو جرّب قارئًا آخر.";
        };
        void MatchAppTheme()
        {
            _matchingAppTheme = true;
            try
            {
                var index = AppAppearance.IsDark ? 2 : AppAppearance.Selection == 3 ? 0 : 1;
                if (_theme.SelectedIndex != index) _theme.SelectedIndex = index; else ApplyTheme();
            }
            finally { _matchingAppTheme = false; }
        }
        AppAppearance.Changed += MatchAppTheme; MatchAppTheme();
        var loading = Ui.Label("جارٍ تجهيز فهرس القرآن…", 14); loading.HorizontalOptions = LayoutOptions.Center; loading.VerticalOptions = LayoutOptions.Center;
        Content = new Grid { Children = { loading } };
    }
    protected override bool OnBackButtonPressed()
    {
        if (_playbackSheet is not null)
        {
            if (!PopupSheet.Dismiss(this)) ClosePlaybackSheet();
            return true;
        }
        if (_verseBar is not null) { HideVerseBar(); return true; }
        if (ChoicePicker.Dismiss(this)) return true;
        if (_settingsSheet.IsVisible) { _settingsSheet.Hide(); return true; }
        if (_readerOpen) { ShowLibrary(); return true; }
        if (_index?.TryNavigateBack() == true) return true;
        return base.OnBackButtonPressed();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing(); var sectionEntry = _sectionEntry; _sectionEntry = false; _active = true; _lifetime?.Dispose(); _lifetime = new();
        await GuardAsync(async () =>
        {
            _document ??= await _repository.LoadAsync(); if (!_active) return;
            if (_printedPages is null)
            {
                await using var mapping = await FileSystem.OpenAppPackageFileAsync("mushaf/verse-pages.json");
                _printedPages = await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary<string, int>>(mapping);
            }
            if (_quarters.Count == 0)
            {
                await using var metadata = await FileSystem.OpenAppPackageFileAsync("quran-metadata.xml");
                _quarters = AlMithaq.Core.Logic.QuranPartitions.Read(metadata);
                var keys = All.Select(v => v.Key).ToHashSet();
                if (_quarters.Any(q => !keys.Contains($"{q.Surah}:{q.Ayah}"))) throw new InvalidDataException("Invalid quarter reference");
            }
            if (!_active) return;
            _mushaf.PageHeading ??= page =>
            {
                var verse = All.First(v => _printedPages![v.Key] == page);
                var quarter = _quarters.LastOrDefault(q => q.Surah < verse.Surah || q.Surah == verse.Surah && q.Ayah <= verse.Ayah);
                return (_document.Chapters[verse.Surah - 1].Name, $"الجزء {quarter?.Juz ?? verse.Juz} · الحزب {quarter?.Hizb ?? 1}");
            };
            _changing = true; _chapter.ItemsSource = _document.Chapters; _changing = false;
            var practiceJson = Preferences.Get("quran.practice.request", "");
            if (practiceJson.Length > 0)
            {
                Preferences.Remove("quran.practice.request");
                AlMithaq.Core.Logic.PracticeRequest? practice = null;
                try { practice = JsonSerializer.Deserialize<AlMithaq.Core.Logic.PracticeRequest>(practiceJson); } catch (JsonException) { }
                if (practice is { Surah: >= 1 and <= 114 } && practice.IsValid(_document.Chapters[practice.Surah - 1].Verses.Count))
                {
                    var chapter = _document.Chapters[practice.Surah - 1]; var start = chapter.Verses[practice.StartAyah - 1];
                    _changing = true; _mode.SelectedIndex = 3; _changing = false;
                    ClearSearch(); Show(start); _rangeStart = start; _rangeEnd = chapter.Verses[practice.EndAyah - 1].Number;
                    _repeat.SelectedIndex = Array.IndexOf(new[] { 1, 3, 5, 10 }, practice.Repeats);
                    Preferences.Remove("quran.wird-page.request"); Preferences.Set("quran.resume.request", false);
                    _status.Text = $"تدريب الحفظ · {chapter.Name} · الآيات {practice.StartAyah}–{practice.EndAyah}";
                    await PlayAsync(start, true); return;
                }
            }
            var wirdPage = Preferences.Get("quran.wird-page.request", 0);
            if (wirdPage is >= 1 and <= 604)
            {
                _changing = true; _mode.SelectedIndex = 3; _changing = false;
                var anchor = All.FirstOrDefault(v => ReadingPage(v) == wirdPage);
                if (anchor is not null)
                {
                    _changing = true; _mode.SelectedIndex = 3; _changing = false; ClearSearch(); Show(anchor);
                    Preferences.Remove("quran.wird-page.request"); Preferences.Set("quran.resume.request", false);
                }
            }
            else if (Preferences.Get("quran.resume.request", false))
            { Preferences.Set("quran.resume.request", false); await ResumeAsync(); }
            else if (_readerOpen && _shown.Count > 0 && (_returningModal || !sectionEntry))
            { _returningModal = false; Show(_shown[0]); SetChrome(_chromeVisible); }
            else if (!sectionEntry && !_readerOpen && _index is not null) SetChrome(true);
            else ShowLibrary();
        });
    }
    protected override void OnDisappearing()
    {
        ClosePlaybackSheet();
        HideVerseBar(); _settingsSheet.Hide();
        DeviceDisplay.Current.KeepScreenOn = false;
#if ANDROID
        AlMithaq.Platforms.Android.ReaderPresentation.SetImmersive(false);
#endif
        _active = false; StopPlayback("reader disappeared"); _mushaf.CancelPending(); _lifetime?.Cancel(); _searchCancellation?.Cancel(); _download?.Cancel(); base.OnDisappearing();
    }
    private async Task GuardAsync(Func<Task> action)
    {
        try { await action(); } catch (OperationCanceledException) { }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); if (_active) await DisplayAlertAsync("تعذر إتمام العملية", "تحقق من الاتصال أو أعد فتح الشاشة للمحاولة مجددًا.", "حسنًا"); }
    }
    private void ApplyTheme()
    {
        var dark = _theme.SelectedIndex == 2;
        Resources["ReaderInk"] = dark ? Color.FromArgb("#ECE5D4") : Ui.Ink;
        Resources["ReaderMuted"] = dark ? Color.FromArgb("#B6C8BF") : Color.FromArgb("#5A7067");
        Resources["ReaderCard"] = dark ? Color.FromArgb("#1F3430") : _theme.SelectedIndex == 0 ? Color.FromArgb("#FFFCF2") : Colors.White;
        Resources["ReaderStroke"] = new SolidColorBrush(dark ? Color.FromArgb("#42564F") : Color.FromArgb("#E4DFD0"));
        _list.BackgroundColor = dark ? Color.FromArgb("#132622") : Ui.Paper;
        foreach (var row in _rows) row.Night = dark;
        void Recolor(IVisualTreeElement parent)
        {
            foreach (var child in parent.GetVisualChildren())
            {
                if (child is Border { ClassId: "verse-card" } card) { card.BackgroundColor = (Color)Resources["ReaderCard"]; card.Stroke = (Brush)Resources["ReaderStroke"]; }
                if (child is Label { ClassId: "verse-reference" } reference) reference.TextColor = (Color)Resources["ReaderMuted"];
                if (child is Button { ClassId: "verse-action" } action) action.TextColor = (Color)Resources["ReaderMuted"];
                Recolor(child);
            }
        }
        Recolor(_list);
        _ = _mushaf.SetThemeAsync(_theme.SelectedIndex);
    }
    private void ClearSearch()
    {
        DismissKeyboard(_search);
        _changing = true; _search.Text = ""; _changing = false; _searchCancellation?.Cancel();
    }

    private static void DismissKeyboard(SearchBar search)
    {
#if ANDROID
        if (search.Handler?.PlatformView is global::Android.Views.View native &&
            native.Context?.GetSystemService(global::Android.Content.Context.InputMethodService) is global::Android.Views.InputMethods.InputMethodManager keyboard)
            keyboard.HideSoftInputFromWindow(native.WindowToken, global::Android.Views.InputMethods.HideSoftInputFlags.None);
#endif
        search.Unfocus();
    }
    private void Show(QuranVerse anchor, bool preserveAudio = false)
    {
        HideVerseBar();
        if (_document is null) return; if (!preserveAudio) StopPlayback("reading context changed");
        if (!_readerOpen)
        {
            _readerOpen = true; Title = "القرآن الكريم"; Content = _readerLayout; SetChrome(false);
        }
        DeviceDisplay.Current.KeepScreenOn = _active;
        _changing = true; _chapter.SelectedIndex = anchor.Surah - 1; _changing = false;
        IEnumerable<QuranVerse> source = _mode.SelectedIndex switch { 1 => All.Where(v => v.Page == anchor.Page), 2 => All.Where(v => v.Juz == anchor.Juz), _ => _document.Chapters[anchor.Surah - 1].Verses };
        if (_mode.SelectedIndex == 3) source = All.Where(v => ReadingPage(v) == ReadingPage(anchor));
        Populate(source); _status.Text = $"{_document.Chapters[anchor.Surah - 1].Name} · صفحة {anchor.Page} · الجزء {anchor.Juz} · {_shown.Count} آية";
        _mushaf.IsVisible = _mode.SelectedIndex == 3; _list.IsVisible = !_mushaf.IsVisible;
        if (_mushaf.IsVisible)
        {
            _status.Text = $"{_document.Chapters[anchor.Surah - 1].Name} · صفحة {ReadingPage(anchor)}";
            var highlighted = preserveAudio && _current is not null ? _current : anchor;
            _ = _mushaf.ShowAsync(ReadingPage(anchor), highlighted.Surah, highlighted.Ayah);
        }
        UpdateDownloadButton();
        _store.SavePosition(anchor);
        if (_list.IsVisible) Dispatcher.Dispatch(() => _list.ScrollTo(Math.Max(0, _shown.FindIndex(v => v.Number == anchor.Number)), position: ScrollToPosition.Start, animate: false));
    }
    private void Populate(IEnumerable<QuranVerse> source)
    {
        var marks = _store.GetBookmarks().Select(b => $"{b.Surah}:{b.Ayah}").ToHashSet(); _shown = source.ToList();
        _rows = _shown.Select(v => new VerseItem(v) { IsBookmarked = marks.Contains(v.Key), IsPlaying = _sequence && v.Number == _current?.Number, Night = _theme.SelectedIndex == 2 }).ToList(); _list.ItemsSource = _rows;
        _firstVisible = _lastVisible = -1;
    }
    private Task ResumeAsync()
    {
        if (_document is not null) { _changing = true; _mode.SelectedIndex = 3; _changing = false; var pos = _store.GetPosition(); ClearSearch(); Show(All.FirstOrDefault(v => v.Surah == pos?.Surah && v.Ayah == pos?.Ayah) ?? All.First()); }
        return Task.CompletedTask;
    }
    private Task MoveAsync(int delta)
    {
        if (_document is null || _shown.Count == 0) return Task.CompletedTask;
        var first = _shown[0]; var target = _mode.SelectedIndex switch { 1 or 3 => All.FirstOrDefault(v => ReadingPage(v) == ReadingPage(first) + delta), 2 => All.FirstOrDefault(v => v.Juz == first.Juz + delta), _ => All.FirstOrDefault(v => v.Surah == first.Surah + delta) };
        if (target is not null) { ClearSearch(); Show(target, preserveAudio: _sequence); } return Task.CompletedTask;
    }
    private Task JumpAsync()
    {
        ShowLibrary(); return Task.CompletedTask;
    }

    public void ShowLibrary()
    {
        if (_document is null) return;
        HideVerseBar(); _settingsSheet.Hide(); StopPlayback("Quran index opened"); _mushaf.CancelPending();
        _readerOpen = false; _returningModal = false; DeviceDisplay.Current.KeepScreenOn = false;
        SetChrome(true); Title = "فهرس القرآن الكريم";
        var position = _store.GetPosition();
        var resume = All.FirstOrDefault(v => v.Surah == position?.Surah && v.Ayah == position?.Ayah) ?? All.First();
        _index = new AlMithaq.Features.Quran.Views.QuranIndexView(_document,
            v => _printedPages is not null ? _printedPages[v.Key] : v.Page, _store.GetBookmarks(), _quarters,
            verse =>
            {
                if (_index?.Children.OfType<SearchBar>().FirstOrDefault() is { } search) DismissKeyboard(search);
                OpenVerseInMushaf(verse);
            }, resume);
        Content = _index;
    }
    private void OpenVerseInMushaf(QuranVerse verse)
    {
        _changing = true; _mode.SelectedIndex = 3; _changing = false;
        ClearSearch(); Show(verse);
    }
    private void SetChrome(bool visible)
    {
        _chromeVisible = visible;
        _readerTop.IsVisible = visible; _readerBottom.IsVisible = visible;
        _volumePopup.IsVisible = false;
        if (!visible) { _settingsSheet.Hide(); _search.Unfocus(); }
        NavigationPage.SetHasNavigationBar(this, visible || !_readerOpen);
#if ANDROID
        AlMithaq.Platforms.Android.ReaderPresentation.SetImmersive(_readerOpen && !visible);
#endif
    }

    private async Task SearchAsync(string query)
    {
        HideVerseBar();
        _searchCancellation?.Cancel(); _searchCancellation?.Dispose(); _searchCancellation = new(); var ct = _searchCancellation.Token;
        if (_document is null) return;
        if (query.Trim().Length < 2) { if (query.Length == 0) Show(_shown.FirstOrDefault() ?? All.First()); else _status.Text = "اكتب حرفين على الأقل للبحث"; return; }
        await GuardAsync(async () => { await Task.Delay(300, ct); var result = await _repository.SearchAsync(query, ct); ct.ThrowIfCancellationRequested(); if (!_active) return; StopPlayback("search results changed"); _mushaf.IsVisible = false; _list.IsVisible = true; Populate(result); _status.Text = $"نتائج البحث: {result.Count}";
            if (result.Count > 0) Dispatcher.Dispatch(() => { if (_active && !ct.IsCancellationRequested && _list.IsVisible) _list.ScrollTo(0, position: ScrollToPosition.Start, animate: false); }); });
    }
    private async Task BookmarksAsync()
    {
        if (_document is null) return; var marks = _store.GetBookmarks();
        if (marks.Count == 0) { await DisplayAlertAsync("العلامات", "احفظ علامة أو ملاحظة من خيارات أي آية.", "حسنًا"); return; }
        var labels = marks.Select((b, i) => $"{i + 1}. {_document.Chapters[b.Surah - 1].Name} · {b.Ayah} — {b.Note}").ToArray();
        var selected = await PopupSheet.ChooseAsync(this, "علاماتي", labels); var index = Array.IndexOf(labels, selected);
        if (index >= 0) { var b = marks[index]; ClearSearch(); Show(All.First(v => v.Surah == b.Surah && v.Ayah == b.Ayah)); }
    }
    private void HideVerseBar()
    {
        if (_verseBar is not null) _readingArea.Remove(_verseBar);
        _verseBar = null;
    }
    private Task ActionsAsync(VerseItem row, double anchor = .5)
    {
        DismissKeyboard(_search);
        HideVerseBar(); _store.SavePosition(row.Verse); _verseAnchor = anchor;
        var icons = new (string Glyph, string Action)[] {
            ("\uf04b", "استماع من هنا"), ("\uf01e", "تكرار مقطع للحفظ"),
            ("\uf02d", "التفسير الميسر"), ("\uf02e", "حفظ علامة / ملاحظة"),
            ("\uf0c5", "نسخ الآية"), ("\uf1e0", "مشاركة الآية"), ("\uf142", "المزيد") };
        var strip = new Grid { ColumnSpacing = 0, FlowDirection = FlowDirection.RightToLeft };
        for (var i = 0; i < icons.Length; i++)
        {
            var item = icons[i]; strip.ColumnDefinitions.Add(new(GridLength.Star));
            var button = Ui.Icon(item.Glyph, item.Action, async () => await GuardAsync(async () =>
            {
                HideVerseBar();
                var action = item.Action == "المزيد" ? await PopupSheet.ChooseAsync(this, "المزيد من خيارات الآية", "حذف العلامة", "إخفاء / إظهار للحفظ", "استكمال القراءة من المصحف") : item.Action;
                if (action is not null) await ExecuteVerseActionAsync(row, action);
            }));
            button.WidthRequest = 42; button.HeightRequest = 44; button.MinimumHeightRequest = 44; button.MinimumWidthRequest = 0;
            ToolTipProperties.SetText(button, item.Action); strip.Add(button, i);
        }
        _verseBar = Ui.Card(strip); _verseBar.Padding = 3; _verseBar.Margin = new Thickness(8, Math.Clamp(anchor * _readingArea.Height - 62, 4, Math.Max(4, _readingArea.Height - 58)), 8, 0);
        _verseBar.VerticalOptions = LayoutOptions.Start; _verseBar.HorizontalOptions = LayoutOptions.Center;
        _verseBar.WidthRequest = Math.Min(320, Math.Max(260, _readingArea.Width - 16)); _verseBar.ZIndex = 20;
        _readingArea.Add(_verseBar);
        return Task.CompletedTask;
    }
    private async Task ExecuteVerseActionAsync(VerseItem row, string action)
    {
        if (_document is null) return;
        var v = row.Verse;
        switch (action)
        {
            case "استماع من هنا": _rangeEnd = null; await PlayAsync(v, true); break;
            case "تكرار مقطع للحفظ":
                var input = await DisplayPromptAsync("تكرار مقطع", $"البداية {v.Ayah}. اكتب رقم آخر آية ثم اختر عدد التكرارات من الشريط.", "ابدأ", "إلغاء", initialValue: v.Ayah.ToString(), keyboard: Keyboard.Numeric);
                if (int.TryParse(Digits(input ?? ""), out var end) && end >= v.Ayah && end <= _document.Chapters[v.Surah - 1].Verses.Count)
                { _rangeStart = v; _rangeEnd = _document.Chapters[v.Surah - 1].Verses[end - 1].Number; await PlayAsync(v, true); }
                else if (input is not null) await DisplayAlertAsync("مقطع غير صحيح", "يجب أن تكون النهاية بعد البداية أو مساوية لها، وداخل السورة نفسها.", "حسنًا");
                break;
            case "التفسير الميسر":
                _status.Text = "جارٍ تحميل التفسير الميسر…";
                try
                {
                    var text = await _media.GetTafsirAsync(v, _lifetime?.Token ?? default);
                    _status.Text = "التفسير محفوظ على الجهاز";
                    if (_active) { _returningModal = true; await Navigation.PushModalAsync(new NavigationPage(new TafsirPage(v, text)) { BarBackgroundColor = Ui.Green, BarTextColor = Colors.White, FlowDirection = FlowDirection.RightToLeft }); }
                }
                catch { _status.Text = "تعذر تحميل التفسير؛ يمكنك إعادة المحاولة"; throw; }
                break;
            case "حفظ علامة / ملاحظة":
                var old = _store.GetBookmarks().FirstOrDefault(b => b.Surah == v.Surah && b.Ayah == v.Ayah);
                var note = await DisplayPromptAsync("علامة وملاحظة", "ملاحظة اختيارية تُحفظ منفصلة عن القرآن", "حفظ", "إلغاء", initialValue: old?.Note ?? "", maxLength: 1000);
                if (note is not null) { _store.SaveBookmark(v, note); row.IsBookmarked = true; } break;
            case "حذف العلامة": _store.RemoveBookmark(v); row.IsBookmarked = false; break;
            case "نسخ الآية": await Clipboard.Default.SetTextAsync(ShareText(v)); _status.Text = "تم نسخ الآية ومرجعها"; break;
            case "مشاركة الآية": await Share.Default.RequestAsync(new ShareTextRequest { Text = ShareText(v), Title = "آية من القرآن الكريم" }); break;
            case "إخفاء / إظهار للحفظ": row.Hidden = !row.Hidden; break;
            case "استكمال القراءة من المصحف": OpenVerseInMushaf(v); break;
        }
    }
    private string ShareText(QuranVerse v) => $"{v.Text}\n[{_document!.Chapters[v.Surah - 1].Name}: {v.Ayah}]\nTanzil.net — https://tanzil.net";
    private Task TogglePlaybackAsync() => _sequence && !_pauseRequested && _player.CurrentState is MediaElementState.Playing or MediaElementState.Buffering
        ? PauseAsync() : PlayCurrentAsync();
    private Task StepAudioAsync(int direction)
    {
        if (_document is null || _shown.Count == 0) return Task.CompletedTask;
        var anchor = _current ?? _shown[0];
        var target = All.FirstOrDefault(v => v.Number == anchor.Number + direction);
        if (target is null) return Task.CompletedTask;
        _rangeStart = null; _rangeEnd = null;
        return PlayAsync(target, true);
    }
    private void UpdateTimeline()
    {
        if (_draggingSeek) return;
        var duration = Math.Max(0, _player.Duration.TotalSeconds);
        var position = _sequence ? Math.Clamp(_player.Position.TotalSeconds, 0, duration) : 0;
        _seek.Maximum = Math.Max(1, duration); _seek.Value = position;
        _seek.IsEnabled = _sequence && duration > 0;
        _elapsed.Text = FormatTime(position); _duration.Text = FormatTime(duration);
    }
    private static string FormatTime(double seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
    private async Task PlayCurrentAsync()
    {
        if (_document is null || _shown.Count == 0) return;
        if (_current is not null && _sequence && (_pauseRequested || _player.CurrentState == MediaElementState.Paused))
        {
            _pauseRequested = false; _playbackGate.Resume();
            _player.Play();
            _playing.Text = $"▶ {_document.Chapters[_current.Surah - 1].Name} · آية {_current.Ayah} — {_reciter.SelectedItem}";
            return;
        }
        if (_sequence && _player.CurrentState == MediaElementState.Playing) return;
        _rangeStart = null; _rangeEnd = null;
        var start = _shown.FirstOrDefault(v => v.Number == _current?.Number) ?? _shown[0];
        if (_mushaf.IsVisible)
        {
            var beginnings = _shown.Where(v => v.Ayah == 1 && v.Number > _shown[0].Number).ToArray();
            if (beginnings.Length > 0)
            {
                var options = new[] { "بداية الصفحة" }.Concat(beginnings.Select(v => _document.Chapters[v.Surah - 1].Name)).ToArray();
                var selection = await PopupSheet.ShowAsync(this, "ابدأ التلاوة من", options);
                if (selection is not int index) return;
                start = index == 0 ? _shown[0] : beginnings[index - 1];
            }
        }
        await PlayAsync(start, true);
    }

    private async Task PlaybackSettingsAsync()
    {
        if (_document is null || _shown.Count == 0 || _playbackSheet is not null) return;
        var anchor = _shown.FirstOrDefault(v => v.Number == _current?.Number) ?? _shown[0];
        var chapters = _document.Chapters;
        var fromSurah = new ChoicePicker { Title = "سورة البداية", ItemsSource = chapters.ToArray(), SelectedIndex = anchor.Surah - 1 };
        var toSurah = new ChoicePicker { Title = "سورة النهاية", ItemsSource = chapters.ToArray(), SelectedIndex = anchor.Surah - 1 };
        var fromAyah = new ChoicePicker { Title = "آية البداية" };
        var toAyah = new ChoicePicker { Title = "آية النهاية" };
        void SetVerses(ChoicePicker surah, ChoicePicker ayah, int index)
        {
            ayah.ItemsSource = Enumerable.Range(1, chapters[surah.SelectedIndex].Verses.Count).ToArray();
            ayah.SelectedIndex = index;
        }
        SetVerses(fromSurah, fromAyah, anchor.Ayah - 1);
        SetVerses(toSurah, toAyah, chapters[anchor.Surah - 1].Verses.Count - 1);
        fromSurah.SelectedIndexChanged += (_, _) => SetVerses(fromSurah, fromAyah, 0);
        toSurah.SelectedIndexChanged += (_, _) => SetVerses(toSurah, toAyah, chapters[toSurah.SelectedIndex].Verses.Count - 1);
        var counts = new[] { 1, 2, 3, 4, 5, 10, int.MaxValue };
        var labels = counts.Select(n => n == int.MaxValue ? "بلا نهاية" : n.ToString()).ToArray();
        var groupRepeats = new ChoicePicker { ItemsSource = labels, SelectedIndex = 0 };
        var verseRepeats = new ChoicePicker { ItemsSource = labels, SelectedIndex = 0 };
        var speeds = new[] { .75, 1, 1.25, 1.5, 2 };
        var speed = new ChoicePicker { ItemsSource = speeds.Select(n => $"{n:0.##}×").ToArray(), SelectedIndex = 1 };
        var onlyRange = new Switch { IsToggled = true };
        var apply = Ui.AsyncButton("تطبيق وتشغيل", async () =>
        {
            var first = chapters[fromSurah.SelectedIndex].Verses[fromAyah.SelectedIndex];
            var last = chapters[toSurah.SelectedIndex].Verses[toAyah.SelectedIndex];
            if (last.Number < first.Number)
            {
                await DisplayAlertAsync("نطاق التلاوة", "اختر نهاية تأتي بعد البداية أو تساويها.", "حسنًا");
                return;
            }
            ClosePlaybackSheet();
            OpenVerseInMushaf(first);
            _rangeStart = first; _rangeEnd = last.Number;
            _customRangeRepeats = counts[groupRepeats.SelectedIndex];
            _customVerseRepeats = counts[verseRepeats.SelectedIndex];
            _onlySelectedRange = onlyRange.IsToggled;
            _player.Speed = speeds[speed.SelectedIndex];
            await PlayAsync(first, true);
        });
        foreach (var choice in new[] { fromSurah, fromAyah, toSurah, toAyah, groupRepeats, verseRepeats, speed }) choice.UseCompactLayout();
        Grid Pair(View first, View second, GridLength firstWidth, GridLength secondWidth)
        {
            var row = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(firstWidth), new(secondWidth) } };
            row.Add(first); row.Add(second, 1);
            return row;
        }
        View Field(string label, View control)
        {
            var field = Ui.Stack(Ui.Label(label, 12, true), control);
            field.Spacing = 3;
            return field;
        }
        var range = Ui.Stack(Field("من · السورة والآية", Pair(fromSurah, fromAyah, GridLength.Star, new GridLength(78))),
            Field("إلى · السورة والآية", Pair(toSurah, toAyah, GridLength.Star, new GridLength(78))));
        range.Spacing = 8;
        var onlyRangeLabel = Ui.Label("شغّل الآيات المختارة فقط", 13);
        onlyRangeLabel.VerticalOptions = LayoutOptions.Center;
        onlyRange.VerticalOptions = LayoutOptions.Center;
        var playbackOptions = Ui.Stack(Pair(Field("تكرار المقطع", groupRepeats), Field("تكرار كل آية", verseRepeats), GridLength.Star, GridLength.Star),
            Field("سرعة التشغيل", speed), Pair(onlyRangeLabel, onlyRange, GridLength.Star, GridLength.Auto));
        playbackOptions.Spacing = 8;
        var controls = Ui.Stack(Ui.Card(range), Ui.Card(playbackOptions));
        controls.Spacing = 10;
        var title = Ui.Label("إعدادات تشغيل المقطع", 17, true);
        title.VerticalOptions = LayoutOptions.Center;
        var heading = Pair(title, Ui.Icon("\uf00d", "إغلاق إعدادات التشغيل", () =>
        {
            ClosePlaybackSheet();
            return Task.CompletedTask;
        }), GridLength.Star, GridLength.Auto);
        var layout = new Grid { RowSpacing = 8, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        layout.Add(heading);
        layout.Add(new ScrollView { Content = controls }, 0, 1);
        layout.Add(apply, 0, 2);
        var card = Ui.Card(layout);
        card.Padding = 12;
        card.HeightRequest = Math.Min(430, Math.Max(240, Height - 40));
        card.VerticalOptions = LayoutOptions.End;
        card.TranslationY = card.HeightRequest;
        var shade = new Grid { BackgroundColor = Color.FromArgb("#99061C15"), ZIndex = 90, FlowDirection = FlowDirection.RightToLeft };
        var backdrop = new Button { BackgroundColor = Colors.Transparent };
        SemanticProperties.SetDescription(backdrop, "إغلاق إعدادات التشغيل");
        backdrop.Clicked += (_, _) => ClosePlaybackSheet();
        shade.Add(backdrop); shade.Add(card);
        Grid.SetRowSpan(shade, Math.Max(1, _readerLayout.RowDefinitions.Count));
        Grid.SetColumnSpan(shade, Math.Max(1, _readerLayout.ColumnDefinitions.Count));
        _playbackSheet = shade;
        _readerLayout.Add(shade);
        await card.TranslateToAsync(0, 0, 190, Easing.CubicOut);
    }

    private void ClosePlaybackSheet()
    {
        if (_playbackSheet is not { } sheet) return;
        _playbackSheet = null;
        _readerLayout.Remove(sheet);
    }
    private Task PauseAsync()
    {
        StartupTrace.Write("Audio pause: user request");
        if (_sequence && _current is not null)
        {
            _pauseRequested = true; _playbackGate.Pause(); _playToggle.Text = "\uf04b";
            _player.Pause(); _playing.Text = "التلاوة متوقفة مؤقتًا — اضغط تشغيل للمتابعة";
        }
        return Task.CompletedTask;
    }
    private Task PlayAsync(QuranVerse v, bool reset)
    {
        StartupTrace.Write("Audio play: " + v.Key);
        if (reset)
        {
            _repeatIndex = 0; _cycles = 0;
            if (_rangeEnd is null) { _customRangeRepeats = null; _onlySelectedRange = true; }
        }
        _playbackToken = _playbackGate.Begin(); _pauseRequested = false; _current = v; _sequence = true;
        var source = _media.GetAudioSource(ReciterId, v);
        if (!Path.IsPathRooted(source) && Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        { _playing.Text = "هذه التلاوة غير منزّلة. اتصل بالإنترنت لتنزيلها."; _sequence = false; _playbackGate.Stop(); return Task.CompletedTask; }
        _player.Source = Path.IsPathRooted(source) ? MediaSource.FromFile(source) : MediaSource.FromUri(new Uri(source));
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet && string.IsNullOrWhiteSpace(_search.Text) && (_continuous.IsToggled || _rangeEnd is not null))
        {
            var next = _rangeEnd == v.Number ? _rangeStart : All.FirstOrDefault(verse => verse.Number == v.Number + 1);
            if (next is not null && (_rangeEnd is not null || next.Surah == v.Surah) && _lifetime is { IsCancellationRequested: false } lifetime)
                _ = _media.PrefetchAsync(ReciterId, next, lifetime.Token);
        }
        // ShouldAutoPlay starts the new source once prepared. Calling Play here prepares
        // the previous/new source a second time while the source update is in progress.
        _playing.Text = $"▶ {_document!.Chapters[v.Surah - 1].Name} · آية {v.Ayah} — {_reciter.SelectedItem}";
        foreach (var row in _rows) row.IsPlaying = row.Verse.Number == v.Number;
        if (_mushaf.IsVisible)
        {
            if (_follow.IsToggled && _mushaf.PageNumber != ReadingPage(v))
            {
                Populate(All.Where(verse => ReadingPage(verse) == ReadingPage(v)));
                _changing = true; _chapter.SelectedIndex = v.Surah - 1; _changing = false;
                _status.Text = $"{_document.Chapters[v.Surah - 1].Name} · صفحة {ReadingPage(v)} · الجزء {v.Juz}";
                _ = _mushaf.ShowAsync(ReadingPage(v), v.Surah, v.Ayah);
            }
            else _ = _mushaf.HighlightAsync(v.Surah, v.Ayah);
        }
        var index = _rows.FindIndex(r => r.Verse.Number == v.Number);
        if (_list.IsVisible && _follow.IsToggled && index >= 0 && (index < _firstVisible || index > _lastVisible))
        {
            var token = _playbackToken;
            Dispatcher.Dispatch(() => { if (_active && _sequence && _follow.IsToggled && _playbackGate.IsCurrent(token)) _list.ScrollTo(index, position: ScrollToPosition.Start, animate: false); });
        }
        _store.SavePosition(v); return Task.CompletedTask;
    }
    private async Task AdvanceAsync(long token)
    {
        StartupTrace.Write("Audio ended: " + _current?.Key);
        if (!_active || !_sequence || _current is null || _advancing || !_playbackGate.TryComplete(token)) return;
        _advancing = true;
        try
        {
        if (_rangeEnd is null && !string.IsNullOrWhiteSpace(_search.Text)) { StopPlayback("search verse finished"); return; }
        var repeats = new[] { 1, 3, 5, 10 }[Math.Max(0, _repeat.SelectedIndex)];
        var verseRepeats = _customRangeRepeats.HasValue ? _customVerseRepeats : _rangeEnd is null ? repeats : 1;
        if (verseRepeats == int.MaxValue || ++_repeatIndex < verseRepeats)
        {
            _playbackToken = _playbackGate.Begin(); var repeatToken = _playbackToken;
            await _player.SeekTo(TimeSpan.Zero);
            if (_active && _sequence && !_pauseRequested && _playbackGate.IsCurrent(repeatToken)) _player.Play();
            return;
        }
        _repeatIndex = 0;
        if (_rangeEnd is not null && _current.Number >= _rangeEnd)
        {
            var rangeRepeats = _customRangeRepeats ?? repeats;
            if ((rangeRepeats == int.MaxValue || ++_cycles < rangeRepeats) && _rangeStart is not null) { await PlayAsync(_rangeStart, false); return; }
            if (_onlySelectedRange) { StopPlayback("range finished"); return; }
            _rangeEnd = null; _customRangeRepeats = null;
        }
        if (!_continuous.IsToggled && _onlySelectedRange && _rangeEnd is null) { StopPlayback("single verse finished"); return; }
        var next = All.FirstOrDefault(v => v.Number == _current.Number + 1);
        if (next is null || (_rangeEnd is null && next.Surah != _current.Surah)) { StopPlayback("surah finished"); return; } await PlayAsync(next, false);
        }
        finally { _advancing = false; }
    }
    private void StopPlayback(string reason)
    {
        StartupTrace.Write("Audio stop: " + reason);
        _sequence = false; _pauseRequested = false; _draggingSeek = false; _playbackGate.Stop();
        _customRangeRepeats = null; _onlySelectedRange = true;
        if (_player.Handler is not null) { _player.Pause(); _player.Stop(); } _player.Source = null;
        _playToggle.Text = "\uf04b";
        foreach (var row in _rows) row.IsPlaying = false;
        _ = _mushaf.HighlightAsync(0, 0);
        UpdateTimeline(); _playing.Text = "التلاوة متوقفة";
    }
    private bool CanDownloadCurrent() => _document is not null && _chapter.SelectedIndex >= 0 && !_document.Chapters[_chapter.SelectedIndex].Verses.All(v => _media.IsDownloaded(ReciterId, v.Number));
    private void UpdateDownloadButton() { if (_downloadSurah is not null) _downloadSurah.IsVisible = CanDownloadCurrent(); }
    private async Task DownloadAsync()
    {
        if (_document is null || _chapter.SelectedIndex < 0 || _download is not null) return;
        if (!CanDownloadCurrent()) { UpdateDownloadButton(); return; }
        var chapter = _document.Chapters[_chapter.SelectedIndex]; var reciter = ReciterId;
        if (await PopupSheet.ChooseAsync(this, $"تنزيل {chapter.Name} بصوت {_reciter.SelectedItem}؟", "تنزيل السورة الحالية", "إلغاء") != "تنزيل السورة الحالية") return;
        _download = CancellationTokenSource.CreateLinkedTokenSource(_lifetime?.Token ?? default); _progress.Progress = 0; _progress.IsVisible = true; _cancelDownload.IsVisible = true;
        try { await _media.DownloadAsync(reciter, chapter.Verses, new Progress<double>(p => { _progress.Progress = p; _status.Text = $"تنزيل التلاوة {p:P0}"; }), _download.Token); _status.Text = "اكتمل التنزيل — التلاوة متاحة دون إنترنت"; }
        catch (OperationCanceledException) { _status.Text = "توقف التنزيل؛ الملفات المكتملة محفوظة لاستكماله لاحقًا"; }
        finally { _download.Dispose(); _download = null; _progress.IsVisible = false; _cancelDownload.IsVisible = false; UpdateDownloadButton(); }
    }
    private static string Digits(string s) => AlMithaq.Core.Logic.NumberInput.Normalize(s);
}

public sealed partial class VerseItem(QuranVerse verse) : ObservableObject
{
    public QuranVerse Verse { get; } = verse;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Marker))] [NotifyPropertyChangedFor(nameof(HasMarker))] [NotifyPropertyChangedFor(nameof(Ink))] [NotifyPropertyChangedFor(nameof(PlayGlyph))] private bool isPlaying;
    public string PlayGlyph => IsPlaying ? "\uf04d" : "\uf04b";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Ink))] private bool night;
    public Color Ink => IsPlaying ? Color.FromArgb(Night ? "#8FD8B3" : "#176B57") : Night ? Color.FromArgb("#ECE5D4") : Ui.Ink;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(Marker))] [NotifyPropertyChangedFor(nameof(HasMarker))] private bool isBookmarked;
    public string Marker => IsPlaying ? "● الآية الحالية" : IsBookmarked ? "★ علامة محفوظة" : "";
    public bool HasMarker => IsPlaying || IsBookmarked;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(DisplayText))] private bool hidden;
    public string DisplayText => Hidden ? "••• راجع الآية من حفظك ثم أظهرها من الخيارات •••" : Verse.Text;
}

public sealed class TafsirPage : ContentPage
{
    public TafsirPage(QuranVerse verse, string text)
    {
        Ui.Page(this, "التفسير الميسر");
        var verseText = new Label { Text = verse.Text, FontFamily = "AmiriQuran", FontSize = 28, LineHeight = 1.6 }; Ui.ThemeProperty(verseText, Label.TextColorProperty, "AppInk");
        var stack = Ui.Stack(Ui.Label(verse.Reference, 13), verseText,
            new BoxView { HeightRequest = 1, Color = Ui.Gold }, Ui.Label(text, 19),
            Ui.Label("التفسير الميسر — مجمع الملك فهد عبر AlQuran.cloud. نص التفسير منفصل عن القرآن، ويُحفظ التفسير المفتوح على الجهاز.", 12),
            Ui.AsyncButton("إغلاق", async () => await Navigation.PopModalAsync()));
        stack.Padding = 24; Content = new ScrollView { Content = stack };
    }
}



