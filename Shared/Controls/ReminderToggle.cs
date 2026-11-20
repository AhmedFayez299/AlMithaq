namespace AlMithaq.Shared.Controls;

// Keep the off-state track visible against the ivory card on both mobile platforms.
public sealed class ReminderToggle : Switch
{
    private bool _subscribed;
    public ReminderToggle()
    {
        Loaded += (_,_) => { Subscribe(); ApplyPalette(); };
        Unloaded += (_,_) => Unsubscribe();
    }
    private void Subscribe() { if (_subscribed) return; AppAppearance.Changed += ApplyPalette; _subscribed = true; }
    private void Unsubscribe() { if (!_subscribed) return; AppAppearance.Changed -= ApplyPalette; _subscribed = false; }
    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null) Unsubscribe();
        base.OnHandlerChanging(args);
    }
    protected override void OnHandlerChanged() { base.OnHandlerChanged(); ApplyPalette(); }
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsToggled) && Handler is not null) Dispatcher.Dispatch(ApplyPalette);
    }
    private void ApplyPalette()
    {
        if (!IsLoaded) return;
#if ANDROID
        if (Handler?.PlatformView is global::AndroidX.AppCompat.Widget.SwitchCompat native && native.Handle != IntPtr.Zero)
        {
            var off = AppAppearance.IsDark ? global::Android.Graphics.Color.Rgb(103, 130, 115) : global::Android.Graphics.Color.Rgb(145, 166, 151);
            native.TrackTintList = new global::Android.Content.Res.ColorStateList(
                new[] { new[] { global::Android.Resource.Attribute.StateChecked }, Array.Empty<int>() },
                new[] { global::Android.Graphics.Color.Rgb(23, 107, 87).ToArgb(), off.ToArgb() });
            native.ThumbTintList = global::Android.Content.Res.ColorStateList.ValueOf(global::Android.Graphics.Color.Rgb(255, 248, 236));
        }
#elif IOS
        if (Handler?.PlatformView is UIKit.UISwitch native)
        {
            var off = AppAppearance.IsDark ? UIKit.UIColor.FromRGB(103,130,115) : UIKit.UIColor.FromRGB(145,166,151);
            native.OnTintColor = UIKit.UIColor.FromRGB(23,107,87);
            native.ThumbTintColor = UIKit.UIColor.FromRGB(255,248,236);
            native.TintColor = off; native.BackgroundColor = IsToggled ? UIKit.UIColor.Clear : off;
            native.Layer.CornerRadius = 16; native.ClipsToBounds = true;
        }
#endif
    }
}
