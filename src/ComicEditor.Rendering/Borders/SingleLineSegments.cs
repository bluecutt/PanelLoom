using ComicEditor.Core.Project;
using ComicEditor.Core.Geometry;
using Point=ComicEditor.Core.Geometry.Point;
namespace ComicEditor.Rendering.Borders;
public sealed record Segment(Point A,Point B,string OwnerId,double ZIndex,int Order);
public static class SingleLineSegments
{
    private sealed record Edge(double Start,double End,string OwnerId,double ZIndex,int Order);
    private sealed record Group(double Ux,double Uy,double Offset,List<Edge> Edges);
    public static IReadOnlyList<Segment> Build(ProjectDocument doc)
    {
        var groups=new List<Group>(); var order=0;
        foreach(var panel in ObjectOrder.Ordered(doc,"panel"))
        {
            var points=DrawingGeometry.Points(panel["polygon"]!);
            for(var i=0;i<points.Length;i++)
            {
                if(!ProjectView.EdgeVisible(panel,i)) continue;
                var a=points[i]; var b=points[(i+1)%points.Length]; var dx=(double)b.X-a.X; var dy=(double)b.Y-a.Y;
                if(Math.Abs(dx)+Math.Abs(dy)<0.0001) continue;
                var length=Math.Sqrt(dx*dx+dy*dy); var ux=dx/length; var uy=dy/length;
                if(ux<-0.000001||(Math.Abs(ux)<0.000001&&uy<0)) { ux=-ux; uy=-uy; }
                var offset=-uy*a.X+ux*a.Y; var group=groups.FirstOrDefault(g=>Math.Abs(g.Ux-ux)<0.000001&&Math.Abs(g.Uy-uy)<0.000001&&Math.Abs(g.Offset-offset)<0.0001);
                if(group is null) { group=new(ux,uy,offset,[]); groups.Add(group); }
                var t0=ux*a.X+uy*a.Y; var t1=ux*b.X+uy*b.Y;
                group.Edges.Add(new(Math.Min(t0,t1),Math.Max(t0,t1),panel.Text("id"),panel.Number("zIndex"),order++));
            }
        }
        var result=new List<Segment>();
        foreach(var group in groups)
        {
            var ends=group.Edges.SelectMany(e=>new[]{e.Start,e.End}).Distinct().Order().ToArray();
            for(var i=0;i<ends.Length-1;i++)
            {
                var lo=ends[i]; var hi=ends[i+1]; if(hi-lo<0.0001) continue; var mid=(lo+hi)/2;
                var owner=group.Edges.Where(e=>mid>=e.Start&&mid<=e.End).MaxBy(e=>e.Order); if(owner is null) continue;
                result.Add(new(new Point((float)(group.Ux*lo-group.Uy*group.Offset),(float)(group.Uy*lo+group.Ux*group.Offset)),new Point((float)(group.Ux*hi-group.Uy*group.Offset),(float)(group.Uy*hi+group.Ux*group.Offset)),owner.OwnerId,owner.ZIndex,owner.Order));
            }
        }
        return result;
    }
}
