#if IOS
using AlMithaq.Shared.Controls;

namespace AlMithaq.Platforms.iOS;

internal static class AppStoreUpdatePrompt
{
    // null means another sheet is open; false means the user chose to continue later.
    internal static async Task<bool?> ShowAsync(ContentPage page, string version)
    {
        if (page.Content is not { } original) return null;
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Close() => result.TrySetResult(false);
        if (!PopupSheet.Register(page, Close)) return null;
        Grid? host = null;
        Grid? shade = null;
        void Disappearing(object? sender, EventArgs e) => Close();
        try
        {
            if (original is Grid existing && existing.ClassId == "choice-host") host = existing;
            else
            {
                page.Content = null;
                host = new Grid { ClassId = "choice-host" };
                host.Add(original); page.Content = host;
            }
            shade = new Grid { BackgroundColor = Color.FromArgb("#AA061C15"), Padding = 20, ZIndex = 100,
                FlowDirection = FlowDirection.RightToLeft };
            var backdrop = new Button { BackgroundColor = Colors.Transparent };
            backdrop.Clicked += (_, _) => Close();
            SemanticProperties.SetDescription(backdrop, "إغلاق تنبيه التحديث");
            shade.Add(backdrop);

            var badge = Ui.Card(new Image { Source = "mithaqbrand.png", WidthRequest = 46, HeightRequest = 46 });
            badge.HorizontalOptions = LayoutOptions.Center; badge.Padding = 8; badge.BackgroundColor = Ui.Green;
            var title = Ui.Label("تحديث الميثاق متاح", 21, true);
            title.HorizontalTextAlignment = TextAlignment.Center;
            SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
            var description = Ui.Label($"يتوفر الإصدار {version} على App Store. يمكنك تثبيته الآن أو المتابعة لاحقًا.", 14);
            description.HorizontalTextAlignment = TextAlignment.Center;
            Ui.ThemeProperty(description, Label.TextColorProperty, "AppMuted");
            var update = Ui.Button("تحديث الآن", () => result.TrySetResult(true));
            var later = Ui.Button("لاحقًا", Close, secondary: true);
            var actions = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
            actions.Add(update); actions.Add(later, 1);
            var card = Ui.Card(Ui.Stack(badge, title, description, actions));
            card.FlowDirection = FlowDirection.RightToLeft;
            card.Margin = 0; card.Padding = 20;
            card.HorizontalOptions = card.VerticalOptions = LayoutOptions.Center;
            void SizeCard(object? sender, EventArgs e) => card.WidthRequest = Math.Min(400, Math.Max(240, page.Width - 40));
            SizeCard(null, EventArgs.Empty); page.SizeChanged += SizeCard;
            shade.Add(card);
            Grid.SetRowSpan(shade, Math.Max(1, host.RowDefinitions.Count));
            Grid.SetColumnSpan(shade, Math.Max(1, host.ColumnDefinitions.Count));
            page.Disappearing += Disappearing;
            shade.Unloaded += Disappearing;
            host.Add(shade);
            try { return await result.Task; }
            finally { page.SizeChanged -= SizeCard; }
        }
        finally
        {
            page.Disappearing -= Disappearing;
            if (shade is not null)
            {
                shade.Unloaded -= Disappearing;
                host?.Remove(shade);
            }
            PopupSheet.Unregister(page, Close);
        }
    }
}
#endif
