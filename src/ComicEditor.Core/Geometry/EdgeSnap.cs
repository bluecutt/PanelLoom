using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Geometry;
public enum SnapStatus { Moved,AlreadyAligned,NoCandidate,Unsafe }
public sealed record SnapResult(Point[] Points,SnapStatus Status,string? TargetPanelId,int? TargetEdgeIndex,double Distance);
public static class EdgeSnap
{
    public static Point Project(Point p,Point a,Point b,bool clamp=false)
    {
        var dx=b.X-a.X; var dy=b.Y-a.Y; var square=dx*dx+dy*dy; if(square<0.000001) return a;
        var t=((p.X-a.X)*dx+(p.Y-a.Y)*dy)/square; if(clamp) t=Math.Clamp(t,0,1);
        return new((float)(a.X+t*dx),(float)(a.Y+t*dy));
    }
    public static Point[] SnapEdge(JsonObject panel,IEnumerable<JsonObject> others,int index,double tolerance)
        =>SnapEdgeDetailed(panel,others,index,tolerance).Points;
    public static SnapResult SnapEdgeDetailed(JsonObject panel,IEnumerable<JsonObject> others,int index,double tolerance)
    {
        var points=PolygonOperations.Read(panel["polygon"]!);if(index<0||index>=points.Length)throw new EditorException("INDEX","Index out of range");if(!double.IsFinite(tolerance)||tolerance<0)throw new EditorException("ARGS","Invalid tolerance");var a=points[index]; var b=points[(index+1)%points.Length];
        var dx=b.X-a.X; var dy=b.Y-a.Y; var length=Math.Sqrt(dx*dx+dy*dy); if(length<0.01) return new(points,SnapStatus.Unsafe,null,null,0);
        var ux=dx/length; var uy=dy/length; var bounds=PolygonOperations.Bounds(points);
        var ownSide=(bounds.X+bounds.Width/2-a.X)*-uy+(bounds.Y+bounds.Height/2-a.Y)*ux;
        var best=tolerance+0.000001; Point? bestA=null,bestB=null;
        string? targetId=null;int? targetEdge=null;
        foreach(var other in others.Where(o=>o.Text("id")!=panel.Text("id")))
        {
            var op=PolygonOperations.Read(other["polygon"]!); var ob=PolygonOperations.Bounds(op);
            var side=(ob.X+ob.Width/2-a.X)*-uy+(ob.Y+ob.Height/2-a.Y)*ux; if(side*ownSide>=0) continue;
            for(var i=0;i<op.Length;i++)
            {
                var ea=op[i]; var eb=op[(i+1)%op.Length]; var ex=eb.X-ea.X; var ey=eb.Y-ea.Y; var el=Math.Sqrt(ex*ex+ey*ey);
                if(el<0.01||Math.Abs(ux*ey-uy*ex)/el>0.00001) continue;
                var t0=(ea.X-a.X)*ux+(ea.Y-a.Y)*uy; var t1=(eb.X-a.X)*ux+(eb.Y-a.Y)*uy;
                if(Math.Min(length,Math.Max(t0,t1))-Math.Max(0,Math.Min(t0,t1))<4) continue;
                var pa=Project(a,ea,eb); var distance=Math.Sqrt(Math.Pow(pa.X-a.X,2)+Math.Pow(pa.Y-a.Y,2));
                if(distance<=tolerance&&distance<best) { best=distance; bestA=pa; bestB=Project(b,ea,eb);targetId=other.Text("id");targetEdge=i; }
            }
        }
        if(bestA is null) return new(points,SnapStatus.NoCandidate,null,null,0);
        if(best<=0.0001)return new(points,SnapStatus.AlreadyAligned,targetId,targetEdge,best);
        var candidate=(Point[])points.Clone(); candidate[index]=bestA.Value; candidate[(index+1)%points.Length]=bestB!.Value;
        return PolygonOperations.Area(candidate)<64 ? new(points,SnapStatus.Unsafe,targetId,targetEdge,best) : new(candidate,SnapStatus.Moved,targetId,targetEdge,best);
    }
}
