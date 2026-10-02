using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Validation;
using System.Text.Json.Nodes;
namespace ComicEditor.Desktop.Canvas;
public enum EditMode { View,Image,Panel,Vertex,Balloon,FrameOnly,Edge }
public enum DragKind { Select,Image,Panel,Vertex,FrameSize,Balloon,Size,Rotate,FrameOnly }
public sealed record DragHit(string Kind,string Id,DragKind DragKind,int Index=-1);
public sealed class InputRouter(EditorViewModel model)
{
    private DragHit? target;
    private Point origin;
    private ProjectDocument? baseline;
    private IReadOnlyList<Operation>? operations;
    private long revision;
    public bool MoveGroup { get; set; }
    public bool Dragging=>target is not null;
    public ProjectDocument? Preview { get; private set; }
    public void Begin(DragHit hit,Point start)
    {
        if(hit.DragKind==DragKind.Select)return;
        if(model.Busy||model.InputPending)throw new EditorException("BUSY","Finish the current interaction first",6);
        var doc=model.Document; var obj=Find(doc,hit);
        if(obj.Flag("locked"))throw new EditorException("LOCKED","Object locked",5);
        target=hit;origin=start;baseline=doc;revision=model.Revision;operations=null;Preview=null;model.Busy=true;
    }
    public void Update(Point point,bool bypassSnap=false)
    {
        if(target is null||baseline is null)return;
        var dx=point.X-origin.X;var dy=point.Y-origin.Y;if(Math.Abs(dx)+Math.Abs(dy)<.5){operations=null;Preview=null;return;}
        var obj=Find(baseline,target);var args=new JsonObject();string action;
        switch(target.DragKind)
        {
            case DragKind.Vertex:
                var points=PolygonOperations.Read(obj["polygon"]!);var vertex=new Point(points[target.Index].X+dx,points[target.Index].Y+dy);
                if(!bypassSnap&&new ProjectView(baseline).SnapEnabled) {var tolerance=baseline.Root["edgeSnap"].Number("tolerance",8);var near=new ProjectView(baseline).Panels.Where(p=>p.Text("id")!=target.Id).SelectMany(p=>PolygonOperations.Read(p["polygon"]!)).OrderBy(p=>Math.Pow(p.X-vertex.X,2)+Math.Pow(p.Y-vertex.Y,2)).FirstOrDefault(); if(Math.Sqrt(Math.Pow(near.X-vertex.X,2)+Math.Pow(near.Y-vertex.Y,2))<=tolerance)vertex=near;}
                action="panel.vertex";args=new(){["index"]=target.Index,["x"]=vertex.X,["y"]=vertex.Y};break;
            case DragKind.Image:
                action="panel.image";args=new(){["offsetX"]=obj["imageTransform"].Number("offsetX")+dx,["offsetY"]=obj["imageTransform"].Number("offsetY")+dy};break;
            case DragKind.Panel:
                action="panel.move";args=new(){["dx"]=dx,["dy"]=dy,["moveImage"]=true};break;
            case DragKind.FrameOnly:
                action="panel.move";args=new(){["dx"]=dx,["dy"]=dy,["moveImage"]=false};break;
            case DragKind.FrameSize:
                var b=PolygonOperations.Bounds(PolygonOperations.Read(obj["polygon"]!));var left=target.Index is 0 or 3;var top=target.Index is 0 or 1;
                var width=Math.Max(8,b.Width+(left?-dx:dx));var height=Math.Max(8,b.Height+(top?-dy:dy));action="panel.bounds";args=new(){["x"]=left?b.X+b.Width-width:b.X,["y"]=top?b.Y+b.Height-height:b.Y,["width"]=width,["height"]=height};break;
            case DragKind.Balloon:
                action="balloon.move";args=new(){["dx"]=dx,["dy"]=dy,["moveGroup"]=MoveGroup};break;
            case DragKind.Rotate:
                var t=obj["transform"];var center=new Point(t.Number("x",100)+t.Number("width",240)/2,t.Number("y",100)+t.Number("height",150)/2);
                action="balloon.transform";args=new(){["rotation"]=t.Number("rotation")+(Math.Atan2(point.Y-center.Y,point.X-center.X)-Math.Atan2(origin.Y-center.Y,origin.X-center.X))*180/Math.PI};break;
            default:
                args=HandleHitTest.ResizeBalloon(obj,target.Index,dx,dy);action="balloon.transform";break;
        }
        var list=new List<Operation>{new(action,target.Id,args)};
        if(target.DragKind is DragKind.Panel or DragKind.FrameOnly&&!bypassSnap&&new ProjectView(baseline).SnapEnabled)for(var i=0;i<obj["polygon"]!.AsArray().Count;i++)list.Add(new("panel.snap",target.Id,new(){["index"]=i}));
        var plan=new CommandProcessor().Plan(baseline,list);operations=list;Preview=plan.After;
    }
    public void Commit()
    {
        var ops=operations;try { if(target is null)return;if(model.Revision!=revision)throw new EditorException("CONFLICT","Project changed during drag",4);model.Busy=false;if(ops is not null)model.Execute(ops); }
        finally {Cancel();}
    }
    public void Cancel() {var owned=target is not null;target=null;baseline=null;operations=null;Preview=null;if(owned)model.Busy=false;}
    private static JsonObject Find(ProjectDocument doc,DragHit hit)=>(hit.Kind=="panel"?new ProjectView(doc).Panels:new ProjectView(doc).Balloons).First(o=>o.Text("id")==hit.Id);
}
