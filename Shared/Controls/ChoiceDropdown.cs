using Microsoft.Maui.Controls.Shapes;
namespace AlMithaq.Shared.Controls;

// Anchored selection, above the current form. It neither pushes content nor opens a second full-height sheet.
public static class ChoiceDropdown
{
    private sealed record Item(int Index,string Text);
    public static async Task<int?> ShowAsync(ContentPage page,View anchor,string title,IReadOnlyList<string> options,int selected,double maximumHeight=310)
    {
        var host = PopupSheet.Host(page);
        var result = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Close() => result.TrySetResult(null);
        PopupSheet.Register(page,Close,allowNested:true);
        var overlay = new AbsoluteLayout { ZIndex=PopupSheet.Layer(page),FlowDirection=FlowDirection.LeftToRight };
        var outside = new Button { BackgroundColor=Colors.Transparent,BorderWidth=0,Padding=0 };
        outside.Clicked += (_,_)=>Close(); SemanticProperties.SetDescription(outside,"إغلاق قائمة الاختيار");
        AbsoluteLayout.SetLayoutBounds(outside,new Rect(0,0,1,1)); AbsoluteLayout.SetLayoutFlags(outside,Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All); overlay.Add(outside);
        var numeric = options.All(x=>double.TryParse(x,out _));
        var columns = numeric ? 4 : title.Contains("تكرار") || title.Contains("سرعة") ? 2 : 1;
        var list = new CollectionView { SelectionMode=SelectionMode.None,ItemsLayout=new GridItemsLayout(columns,ItemsLayoutOrientation.Vertical) { HorizontalItemSpacing=4,VerticalItemSpacing=4 } };
        list.ItemTemplate = new DataTemplate(()=>
        {
            var button = Ui.Button("",()=>{},true); button.FontSize=13; button.Padding=new Thickness(8,4); button.CornerRadius=11;
            button.MinimumHeightRequest=button.HeightRequest=40; button.LineBreakMode=LineBreakMode.TailTruncation;
            button.SetBinding(Button.TextProperty,nameof(Item.Text));
            button.BindingContextChanged += (_,_)=>
            {
                if (button.BindingContext is not Item item) return;
                if (item.Index==selected) { button.BackgroundColor=Ui.Green;button.TextColor=Colors.White; }
                else { Ui.ThemeProperty(button,Button.BackgroundColorProperty,"AppCard");Ui.ThemeProperty(button,Button.TextColorProperty,"AppInk"); }
            };
            button.Clicked += (_,_)=> { if (button.BindingContext is Item item) result.TrySetResult(item.Index); };
            return button;
        });
        void Filter(string? query) => list.ItemsSource=options.Select((x,i)=>new Item(i,x)).Where(x=>AlMithaq.Core.Logic.ArabicSearch.Normalize(x.Text).Contains(AlMithaq.Core.Logic.ArabicSearch.Normalize(query??""),StringComparison.OrdinalIgnoreCase)).ToArray();
        var search=new SearchBar { Placeholder="ابحث في القائمة…",IsVisible=options.Count>12,FontFamily="Almarai-Regular",FontSize=12,HeightRequest=40 };
        Ui.ThemeProperty(search,SearchBar.TextColorProperty,"AppInk"); search.TextChanged+=(_,e)=>Filter(e.NewTextValue); Filter("");
        var caption=Ui.Label(title,12,true);
        var body=Ui.Stack(caption,search,list); body.Spacing=6;
        var menu=Ui.Card(body); menu.Padding=10;menu.Margin=0;menu.Stroke=new SolidColorBrush(Ui.Gold.WithAlpha(.6f));menu.StrokeThickness=.7;menu.StrokeShape=new RoundRectangle { CornerRadius=17 };menu.FlowDirection=FlowDirection.RightToLeft;
        menu.Shadow=new Shadow { Brush=Colors.Black,Opacity=.2f,Radius=12,Offset=new Point(0,4) };
        var bounds=Bounds(anchor,host);var width=Math.Min(340,Math.Max(240,Math.Min(bounds.Width,host.Width-20)));
        var header=search.IsVisible?83:37;
        var height=Math.Min(maximumHeight,header+Math.Ceiling(options.Count/(double)columns)*44+20);
        height=Math.Min(height,Math.Max(120,host.Height-20));list.HeightRequest=height-header-20;
        var x=Math.Clamp(bounds.Right-width,10,Math.Max(10,host.Width-width-10));var y=bounds.Bottom+4;
        if(y+height>host.Height-10)y=Math.Max(10,bounds.Top-height-4);
        AbsoluteLayout.SetLayoutBounds(menu,new Rect(x,y,width,height));overlay.Add(menu);
        Grid.SetRowSpan(overlay,Math.Max(1,host.RowDefinitions.Count));Grid.SetColumnSpan(overlay,Math.Max(1,host.ColumnDefinitions.Count));
        void Gone(object? s,EventArgs e)=>Close();
        page.Disappearing+=Gone;
        try
        {
            host.Add(overlay);
            if(selected>=0)page.Dispatcher.Dispatch(()=>list.ScrollTo(selected,position:ScrollToPosition.Center,animate:false));
            return await result.Task;
        }
        finally { page.Disappearing-=Gone;search.Unfocus();host.Remove(overlay);PopupSheet.Unregister(page,Close); }
    }
    private static Rect Bounds(View anchor,Grid host)
    {
#if ANDROID
        if(anchor.Handler?.PlatformView is global::Android.Views.View native && host.Handler?.PlatformView is global::Android.Views.View root)
        {
            var point=new int[2];var origin=new int[2];native.GetLocationOnScreen(point);root.GetLocationOnScreen(origin);var density=DeviceDisplay.Current.MainDisplayInfo.Density;
            return new Rect((point[0]-origin[0])/density,(point[1]-origin[1])/density,native.Width/density,native.Height/density);
        }
#elif IOS
        if(anchor.Handler?.PlatformView is UIKit.UIView native && host.Handler?.PlatformView is UIKit.UIView root)
        {
            var bounds=native.ConvertRectToView(native.Bounds,root);
            return new Rect(bounds.X,bounds.Y,bounds.Width,bounds.Height);
        }
#endif
        double x=0,y=0;Element? item=anchor;
        while(item is VisualElement view && item!=host) { x+=view.X+view.TranslationX;y+=view.Y+view.TranslationY;item=view.Parent; }
        return new Rect(x,y,anchor.Width,anchor.Height);
    }
}
