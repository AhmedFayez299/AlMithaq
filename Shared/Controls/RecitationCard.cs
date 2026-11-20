using Microsoft.Maui.Controls.Shapes;
namespace AlMithaq.Shared.Controls;

// All ornament is behind the native controls and never participates in hit testing.
public sealed class RecitationCard : Border
{
    public RecitationCard(View controls)
    {
        var ornament=new GraphicsView { Drawable=new CornerOrnament(),InputTransparent=true };
        controls.ZIndex=1;
        var layer=new Grid();layer.Add(ornament);layer.Add(controls);
        Content=layer;Padding=new Thickness(10,4);Margin=new Thickness(8,2,8,4);
        Stroke=new SolidColorBrush(Ui.Gold.WithAlpha(.72f));StrokeThickness=.7;
        StrokeShape=new RoundRectangle { CornerRadius=20 };
        Shadow=new Shadow { Brush=Colors.Black,Opacity=.12f,Radius=7,Offset=new Point(0,3) };
        var weak=new WeakReference<RecitationCard>(this);Action? repaint=null;
        repaint=()=> { if(weak.TryGetTarget(out var card))card.Paint();else AppAppearance.Changed-=repaint; };
        AppAppearance.Changed+=repaint;Paint();
    }
    private void Paint()
    {
        var dark=AppAppearance.IsDark;
        var warm=AppAppearance.Selection==3;
        Background=new LinearGradientBrush(new GradientStopCollection
        {
            new(Color.FromArgb(dark?"#164C3C":warm?"#FFF4DF":"#FFFDF5"),0),
            new(Color.FromArgb(dark?"#082B20":warm?"#F0E0BF":"#F3EBD9"),1)
        },new Point(0,0),new Point(1,1));
    }
    private sealed class CornerOrnament : IDrawable
    {
        public void Draw(ICanvas canvas,RectF bounds)
        {
            canvas.StrokeColor=Ui.Gold.WithAlpha(.25f);canvas.StrokeSize=.65f;
            foreach(var right in new[]{false,true})
            {
                canvas.SaveState();
                if(right){canvas.Translate(bounds.Width,0);canvas.Scale(-1,1);}
                var petal=new PathF();petal.MoveTo(0,26);petal.CurveTo(15,24,8,12,21,10);petal.CurveTo(24,4,29,6,34,0);
                canvas.DrawPath(petal);
                var arch=new PathF();arch.MoveTo(0,14);arch.CurveTo(8,14,10,8,11,0);canvas.DrawPath(arch);
                canvas.DrawLine(0,0,39,0);canvas.DrawLine(0,0,0,29);
                canvas.RestoreState();
            }
        }
    }
}
