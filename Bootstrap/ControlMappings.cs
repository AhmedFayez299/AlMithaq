using Microsoft.Maui.Handlers;
namespace AlMithaq;
public static class ControlMappings
{
        public static void ControlsBackground()
        {

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(Entry), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                handler.PlatformView.Layer.BorderWidth = 0;
#endif
            });

            Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping(nameof(SearchBarHandler), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                if (view is SearchBar managed)
                    AlMithaq.Shared.Controls.Ui.ThemeProperty(managed, SearchBar.BackgroundColorProperty, "AppPaper");
                var search = handler.PlatformView;
                search.SearchBarStyle = UIKit.UISearchBarStyle.Minimal;
                search.BackgroundImage = new UIKit.UIImage();
                search.BackgroundColor = UIKit.UIColor.Clear;
                search.SearchTextField.BackgroundColor = UIKit.UIColor.Clear;
                search.SearchTextField.BorderStyle = UIKit.UITextBorderStyle.None;
                search.SearchTextField.Layer.BorderWidth = 0;
#endif
            });


            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping(nameof(PickerHandler), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#endif
            });



            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping(nameof(EditorHandler), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#endif
            });


            Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(nameof(DatePickerHandler), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#endif
            });

            Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping(nameof(TimePickerHandler), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#endif
            });

#if IOS
            foreach (var key in new[] { nameof(Slider.ThumbColor), nameof(SliderHandler) })
                SliderHandler.Mapper.AppendToMapping(key, (handler, view) =>
                {
                    using var renderer = new UIKit.UIGraphicsImageRenderer(new CoreGraphics.CGSize(14, 14));
                    var thumb = renderer.CreateImage(context =>
                    {
                        var color = view.ThumbColor;
                        (color is null ? UIKit.UIColor.FromRGB(178, 138, 67) :
                            new UIKit.UIColor((nfloat)color.Red, (nfloat)color.Green, (nfloat)color.Blue, (nfloat)color.Alpha)).SetFill();
                        UIKit.UIBezierPath.FromOval(new CoreGraphics.CGRect(0, 0, 14, 14)).Fill();
                    });
                    handler.PlatformView.SetThumbImage(thumb, UIKit.UIControlState.Normal);
                    handler.PlatformView.SetThumbImage(thumb, UIKit.UIControlState.Highlighted);
                });
            foreach (var key in new[] { nameof(Button.ContentLayout), nameof(Button.ImageSource), nameof(Button.FlowDirection), nameof(ButtonHandler) })
                ButtonHandler.Mapper.AppendToMapping(key, (handler, view) =>
                {
                    if (view is not Button button || button.ImageSource is null || string.IsNullOrEmpty(button.Text)) return;
                    // Keep Arabic text shaping while making native image/title placement deterministic.
                    handler.PlatformView.SemanticContentAttribute = UIKit.UISemanticContentAttribute.ForceLeftToRight;
                    if (handler.PlatformView.Configuration is not { } config) return;
                    config.ImagePadding = (nfloat)Math.Max(5, button.ContentLayout.Spacing);
                    config.ImagePlacement = button.ContentLayout.Position == Button.ButtonContentLayout.ImagePosition.Right
                        ? UIKit.NSDirectionalRectEdge.Trailing : UIKit.NSDirectionalRectEdge.Leading;
                    handler.PlatformView.Configuration = config;
                });
#endif


        }
    }
