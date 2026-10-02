using ComicEditor.Core.Geometry;
namespace ComicEditor.Desktop.Canvas;
public sealed class ViewTransform
{
    public double Zoom { get; private set; }=1;
    public double X { get; private set; }
    public double Y { get; private set; }
    public Point ToPage(Point p)=>new((p.X-X)/Zoom,(p.Y-Y)/Zoom);
    public Point ToScreen(Point p)=>new(p.X*Zoom+X,p.Y*Zoom+Y);
    public void Wheel(int delta,Point cursor) { var anchor=ToPage(cursor);Zoom=Math.Clamp(Zoom*Math.Pow(1.15,delta/120d),.05,16);X=cursor.X-anchor.X*Zoom;Y=cursor.Y-anchor.Y*Zoom; }
    public void Pan(double dx,double dy) { X+=dx;Y+=dy; }
    public void Fit(double pageWidth,double pageHeight,double width,double height) { Zoom=Math.Max(.05,Math.Min((width-40)/pageWidth,(height-40)/pageHeight));X=(width-pageWidth*Zoom)/2;Y=(height-pageHeight*Zoom)/2; }
}
