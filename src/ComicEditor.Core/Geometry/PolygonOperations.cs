using System.Text.Json;
using System.Text.Json.Nodes;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Geometry;
public readonly record struct Point(double X,double Y);
public readonly record struct Bounds(double X,double Y,double Width,double Height);
public static class PolygonOperations
{
    public static Point[] Read(JsonNode node)=>node.AsArray().Select(p=>new Point(p![0]!.Deserialize<double>(),p[1]!.Deserialize<double>())).ToArray();
    public static JsonArray Write(IEnumerable<Point> points)=>new(points.Select(p=>(JsonNode)new JsonArray(p.X,p.Y)).ToArray());
    public static Bounds Bounds(IEnumerable<Point> points)
    {
        var p=points.ToArray(); var x=p.Min(p=>p.X); var y=p.Min(p=>p.Y);
        return new(x,y,p.Max(p=>p.X)-x,p.Max(p=>p.Y)-y);
    }
    public static Point[] Move(Point[] points,double dx,double dy)=>points.Select(p=>new Point((float)(p.X+dx),(float)(p.Y+dy))).ToArray();
    public static Point[] Resize(Point[] points,Bounds next)
    {
        var old=Bounds(points);
        if(old.Width<=0||old.Height<=0||next.Width<8||next.Height<8) throw new EditorException("BOUNDS","Panel bounds must be at least 8 x 8");
        return points.Select(p=>new Point((float)(next.X+(p.X-old.X)/old.Width*next.Width),(float)(next.Y+(p.Y-old.Y)/old.Height*next.Height))).ToArray();
    }
    public static double Area(Point[] p) { var sum=0d; for(var i=0;i<p.Length;i++) sum+=p[i].X*p[(i+1)%p.Length].Y-p[(i+1)%p.Length].X*p[i].Y; return Math.Abs(sum/2); }
    public static bool Contains(Point[] points,Point point)
    {
        var result=false;
        for(int i=0,j=points.Length-1;i<points.Length;j=i++) if((points[i].Y>point.Y)!=(points[j].Y>point.Y)&&point.X<(points[j].X-points[i].X)*(point.Y-points[i].Y)/(points[j].Y-points[i].Y)+points[i].X) result=!result;
        return result;
    }
}
