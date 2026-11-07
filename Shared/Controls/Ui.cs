namespace AlMithaq.Shared.Controls;

public static class Ui
{
    private sealed record ThemeTarget(WeakReference<BindableObject> Target, BindableProperty Property, string Key, object Value);
    private static readonly List<ThemeTarget> ThemeTargets = [];
    public static void ThemeProperty(BindableObject target, BindableProperty property, string key)
    {
        ThemeTargets.RemoveAll(t => !t.Target.TryGetTarget(out var existing) || ReferenceEquals(existing, target) && t.Property == property);
        if (Application.Current?.Resources.TryGetValue(key, out var value) != true) return;
        target.SetValue(property, value);
        ThemeTargets.Add(new(new(target), property, key, value));
    }
    public static void RefreshThemeProperties()
    {
        for (var i = ThemeTargets.Count - 1; i >= 0; i--)
        {
            var entry = ThemeTargets[i];
            if (!entry.Target.TryGetTarget(out var target)) { ThemeTargets.RemoveAt(i); continue; }
            // A local override (hero text, active menu, reader palette) takes precedence.
            if (!Equals(target.GetValue(entry.Property), entry.Value)) { ThemeTargets.RemoveAt(i); continue; }
            if (Application.Current?.Resources.TryGetValue(entry.Key, out var value) != true) continue;
            target.SetValue(entry.Property, value); ThemeTargets[i] = entry with { Value = value };
        }
    }
    public static readonly Color Green = Color.FromArgb("#176B57");
    public static readonly Color Gold = Color.FromArgb("#B28A43");
    public static Color Ink => Application.Current?.Resources.TryGetValue("AppInk", out var value) == true ? (Color)value : Color.FromArgb("#173C34");
    public static Color Paper => Application.Current?.Resources.TryGetValue("AppPaper", out var value) == true ? (Color)value : Color.FromArgb("#F8F5ED");
    public static Label Label(string text, double size = 16, bool bold = false)
    {
        var label = new Label {
        Text = text, FontSize = size < 12 ? 12 : size == 12 ? 13 : size, FontFamily = bold ? "Almarai-Bold" : "Almarai-Regular",
        TextColor = Ink, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
        LineBreakMode = LineBreakMode.WordWrap
        };
        ThemeProperty(label, Microsoft.Maui.Controls.Label.TextColorProperty, "AppInk"); return label;
    }
    public static Button Button(string text, Action action, bool secondary = false)
    {
        var button = new Button { Text = text, FontFamily = "Almarai-Regular", FontSize = 14,
            BackgroundColor = secondary ? Color.FromArgb("#E7EDE6") : Green,
            TextColor = secondary ? Ink : Colors.White, CornerRadius = 14, Padding = new Thickness(14, 10), MinimumHeightRequest = 48 };
        button.Clicked += (_, _) => action();
        if (secondary) { ThemeProperty(button, Microsoft.Maui.Controls.Button.BackgroundColorProperty, "AppSoft"); ThemeProperty(button, Microsoft.Maui.Controls.Button.TextColorProperty, "AppInk"); }
        return button;
    }
    public static Button AsyncButton(string text, Func<Task> action, bool secondary = false)
    {
        Button? button = null;
        button = Button(text, async () =>
        {
            button!.IsEnabled = false;
            try { await action(); }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                if (Application.Current?.Windows.FirstOrDefault()?.Page is { } root) await root.DisplayAlertAsync("تعذر إتمام العملية", "تحقق من الاتصال والأذونات ثم أعد المحاولة.", "حسنًا");
            }
            finally { button.IsEnabled = true; }
        }, secondary);
        return button;
    }
    public static Entry Input(string placeholder, bool numeric = false)
    {
        var entry = new Entry { Placeholder = placeholder, FontFamily = "Almarai-Regular", FontSize = 14, HeightRequest = 50, Keyboard = numeric ? Keyboard.Numeric : Keyboard.Default };
        ThemeProperty(entry, Entry.TextColorProperty, "AppInk"); ThemeProperty(entry, Entry.PlaceholderColorProperty, "AppMuted"); ThemeProperty(entry, Entry.BackgroundColorProperty, "AppSoft");
        return entry;
    }
    public static Border Card(View content)
    {
        var card = new Border {
        Content = content, Padding = 18, Margin = new Thickness(0, 5), BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#E4E8DE"), StrokeThickness = 1,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 }
        };
        ThemeProperty(card, Border.BackgroundColorProperty, "AppCard"); ThemeProperty(card, Border.StrokeProperty, "AppStrokeBrush"); return card;
    }
    public static Button Icon(string glyph, string description, Func<Task> action, bool primary = false)
    {
        var button = AsyncButton(glyph, action, !primary);
        button.FontFamily = "FontIconSolid";
        button.FontSize = primary ? 22 : 18;
        button.WidthRequest = button.HeightRequest = primary ? 56 : 48;
        button.MinimumWidthRequest = button.MinimumHeightRequest = 48;
        button.CornerRadius = primary ? 28 : 16;
        button.Padding = 0;
#if IOS
        if (primary) AlMithaq.Platforms.iOS.CircularButton.Attach(button);
#endif
        if (!primary) button.BackgroundColor = Colors.Transparent;
        SemanticProperties.SetDescription(button, description);
        ToolTipProperties.SetText(button, description);
        return button;
    }
    public static VerticalStackLayout Stack(params View[] views)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        foreach (var view in views) stack.Add(view);
        return stack;
    }
    public static HorizontalStackLayout Row(params View[] views)
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        foreach (var view in views) row.Add(view);
        return row;
    }
    public static Button ThemeToggle()
    {
        var button = Icon("\uf186", "تغيير المظهر", () => { AppAppearance.Apply(AppAppearance.IsDark ? 1 : 2); return Task.CompletedTask; });
        button.TextColor = Colors.White; button.VerticalOptions = LayoutOptions.Center;
        var target = new WeakReference<Button>(button);
        Action? update = null;
        update = () =>
        {
            if (!target.TryGetTarget(out var view)) { AppAppearance.Changed -= update; return; }
            view.Text = AppAppearance.IsDark ? "\uf185" : "\uf186";
            var description = AppAppearance.IsDark ? "التبديل إلى المظهر الفاتح" : "التبديل إلى المظهر الداكن";
            SemanticProperties.SetDescription(view, description); ToolTipProperties.SetText(view, description);
        };
        AppAppearance.Changed += update; update(); return button;
    }
    public static void Page(ContentPage page, string title, bool nativeHeader = true)
    {
        page.Loaded += (_, _) => PageContentZoom.Attach(page);
        page.Title = title; page.BackgroundColor = Paper; page.FlowDirection = FlowDirection.RightToLeft;
        ThemeProperty(page, ContentPage.BackgroundColorProperty, "AppPaper");
        if (!nativeHeader) return;
#if IOS
        NavigationPage.SetHasBackButton(page, false);
        var heading = Label(title, 17, true); heading.TextColor = Colors.White;
        heading.HorizontalTextAlignment = TextAlignment.Center; heading.VerticalTextAlignment = TextAlignment.Center;
        heading.MaxLines = 1; heading.LineBreakMode = LineBreakMode.TailTruncation;
        heading.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, new Binding(nameof(ContentPage.Title), source: page));
        var bar = new Grid { FlowDirection = FlowDirection.LeftToRight, HeightRequest = 48,
            ColumnDefinitions = { new(44), new(GridLength.Star), new(44), new(44) } };
        var back = Icon("\uf053", "رجوع", () => BackAsync(page)); back.TextColor = Colors.White;
        var menu = Icon("\uf0c9", "القائمة الرئيسية", () => page.Window?.Page is AppShell shell ? shell.ShowMenuAsync() : Task.CompletedTask);
        menu.TextColor = Colors.White;
        bar.Add(back); bar.Add(heading, 1); bar.Add(ThemeToggle(), 2); bar.Add(menu, 3);
        void SizeHeader() { if (page.Width > 32) bar.WidthRequest = page.Width - 32; }
        page.SizeChanged += (_, _) => SizeHeader(); SizeHeader();
        NavigationPage.SetTitleView(page, bar);
        return;
#else
        var heading = Label(title, 18, true);
        heading.TextColor = Colors.White; heading.HeightRequest = 48;
        heading.HorizontalOptions = LayoutOptions.Fill; heading.VerticalTextAlignment = TextAlignment.Center;
        heading.HorizontalTextAlignment = TextAlignment.Center; heading.LineBreakMode = LineBreakMode.TailTruncation; heading.MaxLines = 1;
        heading.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, new Binding(nameof(ContentPage.Title), source: page));
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        var bar = new Grid { ColumnDefinitions = { new(48), new(GridLength.Star), new(48) }, FlowDirection = FlowDirection.LeftToRight, HeightRequest = 48, HorizontalOptions = LayoutOptions.Fill };
#if ANDROID
        AlMithaq.Platforms.Android.HeaderToolbarBackground.Attach(bar);
#else
        var backdrop = new HeaderBackdrop(); Grid.SetColumnSpan(backdrop, 3); bar.Add(backdrop);
#endif
        bar.Add(ThemeToggle(), 2); bar.Add(heading, 1);
        var back = Icon("\uf053", "رجوع", () => BackAsync(page)); back.TextColor = Colors.White; bar.Add(back, 0);
        NavigationPage.SetTitleView(page, bar);
#endif
    }
    private static Task BackAsync(ContentPage page)
    {
        if (PopupSheet.Dismiss(page)) return Task.CompletedTask;
        if (page.SendBackButtonPressed()) return Task.CompletedTask;
        if (page.Navigation.ModalStack.Count > 0) return page.Navigation.PopModalAsync();
        return page.Window?.Page is AppShell shell ? shell.GoBackAsync() : Task.CompletedTask;
    }
}
