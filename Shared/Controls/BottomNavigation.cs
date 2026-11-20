using AlMithaq.Core.Abstractions;
using System.Runtime.CompilerServices;

namespace AlMithaq.Shared.Controls;

public static class BottomNavigation
{
    private sealed record Entry(View Bar);
    private static readonly ConditionalWeakTable<ContentPage, Entry> Entries = new();

    public static void Attach(ContentPage page, IAppNavigator navigator, string route)
    {
        if (Entries.TryGetValue(page, out _)) return;
        var bar = new Grid
        {
            FlowDirection = FlowDirection.RightToLeft, ColumnSpacing = 3, Padding = new Thickness(8, 5),
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }
        };
        var items = new[] { ("الرئيسية", "\uf015", "home"), ("المصحف", "\uf518", "quran"), ("وردي", "\uf5fd", "wird"), ("الأذكار", "\uf1ce", "azkar"), ("الإعدادات", "\uf013", "settings") };
        for (var i = 0; i < items.Length; i++)
        {
            var (title, glyph, destination) = items[i];
            var icon = Ui.Label(glyph, 19); icon.FontFamily = "FontIconSolid";
            icon.HorizontalTextAlignment = TextAlignment.Center;
            var label = Ui.Label(title, 11, destination == route); label.HorizontalTextAlignment = TextAlignment.Center;
            var kind = destination switch { "quran" => "book", "azkar" => "lantern", "more" => "squares", _ => "" };
            View iconView = kind.Length == 0 ? icon : new HomeNavigationIcon(kind) { WidthRequest = 23, HeightRequest = 23, HorizontalOptions = LayoutOptions.Center };
            var body = Ui.Stack(iconView, label); body.Spacing = 1;
            var cell = new Border { Content = body, StrokeThickness = 0, Padding = new Thickness(3, 5), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 } };
            if (destination == route) Ui.ThemeProperty(cell, Border.BackgroundColorProperty, "HomeSelected");
            else cell.BackgroundColor = Colors.Transparent;
            Ui.ThemeProperty(icon, Label.TextColorProperty, destination == route ? "AppAccent" : "AppInk");
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) =>
            {
                if (destination == "more")
                {
#if IOS
                    if (page.Window?.Page is AppShell shell) await shell.ShowMenuAsync();
#else
                    if (page.Window?.Page is FlyoutPage shell) shell.IsPresented = true;
#endif
                }
                else
                {
                    if (destination == "wird" && page is not AlMithaq.Pages.HomePage) Preferences.Set("home.wird.open", true);
                    await navigator.GoToAsync(destination == "wird" ? "home" : destination);
                    if (destination == "wird" && page is AlMithaq.Pages.HomePage home) await home.OpenWirdEditorAsync();
                }
            };
            cell.GestureRecognizers.Add(tap); SemanticProperties.SetDescription(cell, title);
            bar.Add(cell, i);
        }
        var card = Ui.Card(bar); card.Margin = 0; card.Padding = 0;
        Ui.ThemeProperty(card, Border.BackgroundColorProperty, "HomeCard");
        Ui.ThemeProperty(card, Border.StrokeProperty, "HomeStrokeBrush");
        Ui.ThemeProperty(card, Border.StrokeThicknessProperty, "HomeStrokeWidth");
        Entries.Add(page, new(card));
        var wrapping = false;
        void Wrap()
        {
            if (wrapping || page.Content is null || Contains(page.Content, card)) return;
            wrapping = true;
            try
            {
                var content = page.Content; page.Content = null;
                if (content.Parent is Layout previous) previous.Remove(content);
                // The same bar can be reused when the Quran index and reader swap content.
                if (card.Parent is Layout old) old.Remove(card);
                var host = new Grid { RowDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
                host.Add(content); host.Add(card, 0, 1); page.Content = host;
            }
            finally { wrapping = false; }
        }
        page.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(ContentPage.Content)) Wrap(); };
        Wrap();
        page.Appearing += (_, _) =>
        {
            Wrap();
            if (page is not AlMithaq.Pages.QuranPage)
            {
                card.IsVisible = true;
                page.SafeAreaEdges = SafeAreaEdges.All;
#if ANDROID
                AlMithaq.Platforms.Android.ReaderPresentation.SetImmersive(false);
#endif
            }
            page.Dispatcher.Dispatch(() =>
            {
                if (page.Content is Layout layout) layout.InvalidateMeasure();
#if ANDROID
                if (page.Handler?.PlatformView is global::Android.Views.View native)
                { native.RequestApplyInsets(); native.RequestLayout(); }
#endif
            });
        };
    }
    private static bool Contains(IVisualTreeElement root, IVisualTreeElement target)
    {
        if (ReferenceEquals(root, target)) return true;
        // Logical content stays reliable while cached native handlers detach and reattach.
        if (root is Layout layout) return layout.Children.OfType<IVisualTreeElement>().Any(child => Contains(child, target));
        if (root is ContentView content && content.Content is { } childView) return Contains(childView, target);
        if (root is ScrollView scroll && scroll.Content is { } scrollContent) return Contains(scrollContent, target);
        if (root is Border border && border.Content is { } borderContent) return Contains(borderContent, target);
        return root.GetVisualChildren().Any(child => Contains(child, target));
    }

    public static void SetReaderTools(ContentPage page, bool visible)
    {
        if (Entries.TryGetValue(page, out var entry)) entry.Bar.IsVisible = visible;
    }
}
