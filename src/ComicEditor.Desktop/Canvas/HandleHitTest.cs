using ComicEditor.Core.Geometry;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Core.Project;
using System.Text.Json.Nodes;
namespace ComicEditor.Desktop.Canvas;
public static class HandleHitTest
{
    public static DragHit? FindEdge(EditorViewModel model,Point page,double tolerance)
    {
        foreach(var panel in ObjectOrder.Ordered(model.Document,"panel").Reverse().OrderBy(p=>p.Text("id")==model.SelectedId?0:1))
        {
            var points=PolygonOperations.Read(panel["polygon"]!);
            for(var i=0;i<points.Length;i++)if(EdgeDistance(page,points[i],points[(i+1)%points.Length])<=tolerance)return new("panel",panel.Text("id"),DragKind.Select,i);
        }
        return null;
    }
    public static double EdgeDistance(Point p,Point a,Point b)
    {var dx=b.X-a.X;var dy=b.Y-a.Y;var length=dx*dx+dy*dy;var t=length==0?0:Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/length,0,1);return Math.Sqrt(Math.Pow(p.X-a.X-t*dx,2)+Math.Pow(p.Y-a.Y-t*dy,2));}
    public static DragHit? Find(EditorViewModel model,EditMode mode,Point page,double tolerance)
    {
        if(mode==EditMode.View)return null; var doc=model.Document;var view=new ProjectView(doc);var selected=model.Selected;
        if(mode==EditMode.Edge)return FindEdge(model,page,tolerance);
        if(mode==EditMode.Balloon)
        {
            if(model.SelectedKind=="balloon"&&selected is not null)
            {
                var handles=BalloonCorners(selected);for(var i=0;i<handles.Length;i++)if(Near(page,handles[i],tolerance))return new("balloon",model.SelectedId!,DragKind.Size,i);
                if(Near(page,RotationHandle(selected,tolerance*3),tolerance))return new("balloon",model.SelectedId!,DragKind.Rotate);
            }
            foreach(var o in ObjectOrder.Ordered(doc,"balloon").Where(b=>b.Flag("visible",true)).Reverse())
            { if(!PolygonOperations.Contains(BalloonCorners(o),page))continue;var clip=o.Text("clipPanelId");if(clip.Length>0&&!PolygonOperations.Contains(PolygonOperations.Read(view.Panels.First(p=>p.Text("id")==clip)["polygon"]!),page))continue;return new("balloon",o.Text("id"),DragKind.Balloon); }
            return null;
        }
        if(model.SelectedKind=="panel"&&selected is not null)
        {
            var points=mode==EditMode.Vertex?PolygonOperations.Read(selected["polygon"]!):Corners(PolygonOperations.Bounds(PolygonOperations.Read(selected["polygon"]!)));
            if(mode is EditMode.Vertex or EditMode.Panel or EditMode.FrameOnly)for(var i=0;i<points.Length;i++)if(Near(page,points[i],tolerance))return new("panel",model.SelectedId!,mode==EditMode.Vertex?DragKind.Vertex:DragKind.FrameSize,i);
        }
        foreach(var panel in ObjectOrder.Ordered(doc,"panel").Reverse())if(PolygonOperations.Contains(PolygonOperations.Read(panel["polygon"]!),page))return new("panel",panel.Text("id"),mode==EditMode.Image?DragKind.Image:mode is EditMode.Vertex or EditMode.Edge?DragKind.Select:mode==EditMode.FrameOnly?DragKind.FrameOnly:DragKind.Panel);
        return null;
    }
    public static Point[] Corners(Bounds b)=>[new(b.X,b.Y),new(b.X+b.Width,b.Y),new(b.X+b.Width,b.Y+b.Height),new(b.X,b.Y+b.Height)];
    public static Point Rotate(Point p,double radians)=>new(p.X*Math.Cos(radians)-p.Y*Math.Sin(radians),p.X*Math.Sin(radians)+p.Y*Math.Cos(radians));
    public static Point[] BalloonCorners(JsonObject obj)
    {
        var t=obj["transform"];var w=t.Number("width",240);var h=t.Number("height",150);var x=t.Number("x",100)+w/2;var y=t.Number("y",100)+h/2;var angle=t.Number("rotation")*Math.PI/180;
        return Corners(new(-w/2,-h/2,w,h)).Select(p=>{var q=Rotate(p,angle);return new Point(q.X+x,q.Y+y);}).ToArray();
    }
    public static Point RotationHandle(JsonObject obj,double distance)
    {var t=obj["transform"];var w=t.Number("width",240);var h=t.Number("height",150);var p=Rotate(new(0,-h/2-distance),t.Number("rotation")*Math.PI/180);return new(t.Number("x",100)+w/2+p.X,t.Number("y",100)+h/2+p.Y);}
    public static JsonObject ResizeBalloon(JsonObject obj,int corner,double dx,double dy)
    {
        var t=obj["transform"];var oldW=t.Number("width",240);var oldH=t.Number("height",150);var angle=t.Number("rotation")*Math.PI/180;var delta=Rotate(new(dx,dy),-angle);var sx=corner is 0 or 3?-1:1;var sy=corner is 0 or 1?-1:1;
        var w=Math.Max(8,oldW+sx*delta.X);var h=obj.Flag("lockAspect",true)?w*oldH/oldW:Math.Max(8,oldH+sy*delta.Y);
        var shift=Rotate(new(sx*(w-oldW)/2,sy*(h-oldH)/2),angle);var args=new JsonObject{["x"]=t.Number("x",100)+oldW/2+shift.X-w/2,["y"]=t.Number("y",100)+oldH/2+shift.Y-h/2,["width"]=w};if(!obj.Flag("lockAspect",true))args["height"]=h;return args;
    }
    private static bool Near(Point a,Point b,double t)=>Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)<=t*t;
}
