namespace AlMithaq.Shared.Controls;

// Keep the last branded frame until the new native page has laid out and painted.
public static class LaunchTransition
{
    public static void Show(Window window, Page root)
    {
        Page target=root;
        if(target is FlyoutPage flyout)target=flyout.Detail;
        if(target is NavigationPage navigation)target=navigation.CurrentPage;
#if ANDROID
        if (Platforms.Android.AnimatedLaunchCover.RevealWhenReady(window, root, target)) return;
        var activity=Platform.CurrentActivity;
        if(activity?.Window?.DecorView is global::Android.Views.ViewGroup decor && decor.Width>0 && decor.Height>0)
        {
            var bitmap=global::Android.Graphics.Bitmap.CreateBitmap(decor.Width,decor.Height,global::Android.Graphics.Bitmap.Config.Argb8888!)!;
            using(var canvas=new global::Android.Graphics.Canvas(bitmap))decor.Draw(canvas);
            var cover=new global::Android.Widget.ImageView(activity);cover.SetImageBitmap(bitmap);cover.SetScaleType(global::Android.Widget.ImageView.ScaleType.FitXy);
            cover.Elevation=1000;decor.AddView(cover,new global::Android.Views.ViewGroup.LayoutParams(-1,-1));
            var clock=System.Diagnostics.Stopwatch.StartNew();
            var observer=decor.ViewTreeObserver!;
            ReadyFrame? frame=null;
            var removed=false;
            void Remove()
            {
                if(removed)return;removed=true;
                if(observer.IsAlive && frame is not null)observer.RemoveOnPreDrawListener(frame);
                if(cover.Parent is global::Android.Views.ViewGroup parent)parent.RemoveView(cover);
                cover.SetImageBitmap(null);cover.Dispose();bitmap.Dispose();
                if(frame is not null)decor.Post(frame.Dispose);
                StartupTrace.Write($"Launch cover removed after {clock.ElapsedMilliseconds}ms");
            }
            frame=new ReadyFrame(()=>
            {
                var view=target.Handler?.PlatformView as global::Android.Views.View ?? root.Handler?.PlatformView as global::Android.Views.View;
                return view is not null && view.IsAttachedToWindow && view.IsLaidOut && view.Width>0 && view.Height>0;
            },()=>decor.PostOnAnimation(new Java.Lang.Runnable(()=>decor.PostOnAnimation(new Java.Lang.Runnable(Remove)))));
            observer.AddOnPreDrawListener(frame);window.Page=root;
            root.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(2),Remove);return;
        }
#elif IOS
        if(Platform.GetCurrentUIViewController()?.View?.Window is UIKit.UIWindow native && native.SnapshotView(false) is { } cover)
        {
            cover.Frame=native.Bounds;native.AddSubview(cover);var removed=false;
            void Remove() { if(removed)return;removed=true;target.Loaded-=Ready;cover.RemoveFromSuperview();cover.Dispose(); }
            void Ready(object? s,EventArgs e)=>root.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(64),Remove);
            target.Loaded+=Ready;window.Page=root;
            if(target.IsLoaded)Ready(null,EventArgs.Empty);
            root.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(2),Remove);return;
        }
#endif
        window.Page=root;
    }
#if ANDROID
    private sealed class ReadyFrame(Func<bool> ready,Action show) : Java.Lang.Object,global::Android.Views.ViewTreeObserver.IOnPreDrawListener
    {
        private bool _scheduled;
        public bool OnPreDraw() { if(!_scheduled && ready()) { _scheduled=true;show(); } return true; }
    }
#endif
}

