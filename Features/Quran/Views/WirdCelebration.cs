using AlMithaq.Shared.Controls;
namespace AlMithaq.Features.Quran.Views;

public sealed class WirdCelebration : Grid, IDrawable
{
    private sealed record Particle(double X, double Speed, double Drift, double Size, int Color, bool Balloon);
    private readonly Particle[] _particles;
    private readonly GraphicsView _canvas;
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private readonly IDispatcherTimer _timer;
    private readonly TaskCompletionSource _closed = new();
    private readonly ContentPage _owner;
    private readonly Grid _host;
    private static readonly Color[] Colors = [Color.FromArgb("#DFC175"), Color.FromArgb("#8FD8B3"), Color.FromArgb("#81B6CA"), Color.FromArgb("#DDA0A0"), Color.FromArgb("#BDA3D3")];
    private WirdCelebration(ContentPage owner, Grid host)
    {
        _owner = owner; _host = host; ZIndex = 1000; BackgroundColor = Color.FromArgb("#B010251F");
        _particles = Enumerable.Range(0, 42).Select(i => new Particle(Random.Shared.NextDouble(), .15 + Random.Shared.NextDouble() * .2, Random.Shared.NextDouble() * 80 - 40, 5 + Random.Shared.NextDouble() * 6, i % Colors.Length, i % 7 == 0)).ToArray();
        _canvas = new GraphicsView { Drawable = this, InputTransparent = true }; Add(_canvas);
        var title = Ui.Label("أتممت ورد اليوم!", 21, true); title.HorizontalTextAlignment = TextAlignment.Center;
        var note = Ui.Label("خطوة جميلة... واصل رحلتك مع القرآن", 13); note.HorizontalTextAlignment = TextAlignment.Center;
        var sound = new Switch { IsToggled = Preferences.Get("wird.celebration-sound", true) }; sound.Toggled += (_, e) => Preferences.Set("wird.celebration-sound", e.Value);
        var soundLabel = Ui.Label("نغمة الاحتفال", 11); soundLabel.VerticalOptions = LayoutOptions.Center;
        var soundRow = Ui.Row(soundLabel, sound); soundRow.HorizontalOptions = LayoutOptions.Center;
        var close = Ui.Button("الحمد لله · متابعة", Close); close.FontSize = 13;
        var body = Ui.Stack(title, note, soundRow, close); body.Spacing = 12;
        var card = Ui.Card(body); card.Margin = 24; card.MaximumWidthRequest = 360; card.HorizontalOptions = LayoutOptions.Center; card.VerticalOptions = LayoutOptions.Center; Add(card);
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromMilliseconds(33); _timer.Tick += Tick;
        SemanticProperties.SetDescription(card, "أتممت ورد اليوم. يمكنك إغلاق رسالة الاحتفال للمتابعة.");
    }
    public static Task ShowAsync(ContentPage owner)
    {
        if (owner.Content is not Grid host || host.Children.OfType<WirdCelebration>().Any()) return Task.CompletedTask;
        var view = new WirdCelebration(owner, host);
        Grid.SetRowSpan(view, Math.Max(1, host.RowDefinitions.Count)); Grid.SetColumnSpan(view, Math.Max(1, host.ColumnDefinitions.Count));
        host.Add(view); owner.Disappearing += view.Disappearing;
        view._clock.Start(); view._timer.Start();
        if (Preferences.Get("wird.celebration-sound", true)) PlayChime();
        return view._closed.Task;
    }
    private void Tick(object? sender, EventArgs e) { _canvas.Invalidate(); if (_clock.Elapsed.TotalSeconds > 4) { _timer.Stop(); _canvas.IsVisible = false; } }
    private void Disappearing(object? sender, EventArgs e) => Close();
    private void Close()
    {
        _timer.Stop(); _timer.Tick -= Tick; _clock.Stop(); _owner.Disappearing -= Disappearing; _host.Remove(this); _closed.TrySetResult();
    }
    public static bool Dismiss(ContentPage owner)
    {
        if (owner.Content is not Grid host || host.Children.OfType<WirdCelebration>().FirstOrDefault() is not { } view) return false;
        view.Close(); return true;
    }
    public void Draw(ICanvas canvas, RectF rect)
    {
        var time = _clock.Elapsed.TotalSeconds;
        foreach (var p in _particles)
        {
            var x = (float)(p.X * rect.Width + Math.Sin(time * 2 + p.X * 8) * p.Drift);
            var fraction = p.Balloon ? 1.1 - time * p.Speed : -.15 + time * p.Speed;
            var y = (float)(fraction * rect.Height); var size = (float)p.Size;
            canvas.FillColor = Colors[p.Color]; canvas.StrokeColor = Colors[p.Color];
            if (p.Balloon) { canvas.FillEllipse(x, y, size * 2.2f, size * 2.8f); canvas.StrokeSize = 1; canvas.DrawLine(x + size, y + size * 2.8f, x + size + 3, y + size * 6); }
            else { canvas.SaveState(); canvas.Rotate((float)(time * 110 + p.X * 360), x, y); canvas.FillRectangle(x, y, size, size * .6f); canvas.RestoreState(); }
        }
    }
    private static void PlayChime()
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            var audio = (global::Android.Media.AudioManager?)context.GetSystemService(global::Android.Content.Context.AudioService);
            if (audio?.RingerMode != global::Android.Media.RingerMode.Normal) return;
            var player = global::Android.Media.MediaPlayer.Create(context, Resource.Raw.wird_chime);
            if (player is null) return; player.SetVolume(.22f, .22f);
            player.Completion += (_, _) => { player.Release(); player.Dispose(); };
            player.Error += (_, e) => { e.Handled = true; player.Release(); player.Dispose(); }; player.Start();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
#endif
    }
}
