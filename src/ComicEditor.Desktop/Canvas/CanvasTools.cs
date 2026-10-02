using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.ViewModels;
namespace ComicEditor.Desktop.Canvas;
public sealed class CanvasTools(EditorViewModel model)
{
    public string? PickingBalloonId {get;private set;}
    public event Action? Changed;
    private bool Ready()=>model.TryLeaveContext(model.InputPending?model.ResolveDrafts?.Invoke()??DraftDecision.Cancel:DraftDecision.Discard);
    public void BeginClipPick(string balloonId)
    {
        if(!Ready())return;
        var balloon=new ProjectView(model.Document).Balloons.FirstOrDefault(b=>b.Text("id")==balloonId)??throw new EditorException("OBJECT_NOT_FOUND","气泡不存在");
        if(balloon.Flag("locked"))throw new EditorException("LOCKED","气泡已锁定",5);
        PickingBalloonId=balloonId;model.Status="点击画布中的目标分镜；Esc 取消，不会移动气泡。";Changed?.Invoke();
    }
    public void CancelPick(){PickingBalloonId=null;Changed?.Invoke();}
    public IReadOnlyList<JsonObject> PanelsAt(Point point)=>ObjectOrder.Ordered(model.Document,"panel").Reverse().Where(p=>PolygonOperations.Contains(PolygonOperations.Read(p["polygon"]!),point)).ToArray();
    public bool PickPanelAt(Point point)
    {
        if(PickingBalloonId is not {} id)return false;
        var panel=PanelsAt(point).FirstOrDefault();if(panel is null){model.Status="此处没有分镜，请点击分镜内部或按 Esc 退出。";Changed?.Invoke();return false;}
        if(!Ready())return false;
        model.Execute([new("balloon.clip",id,new(){["clipPanelId"]=panel.Text("id")})]);CancelPick();return true;
    }
    public IReadOnlyList<EditorSelection> CandidatesAt(Point point)
    {
        var doc=model.Document;var view=new ProjectView(doc);var nodes=OcclusionOrder.Build(doc);
        var panels=view.Panels.ToDictionary(p=>p.Text("id"));var balloons=view.Balloons.ToDictionary(b=>b.Text("id"));
        var result=new List<EditorSelection>();
        foreach(var node in nodes.Reverse())
        {
            if(node.Kind=="panel-image"&&PolygonOperations.Contains(PolygonOperations.Read(panels[node.Id]["polygon"]!),point))result.Add(new("panel",node.Id,EditMode.Image,null,null));
            if(node.Kind!="balloon")continue;
            var b=balloons[node.Id];if(!b.Flag("visible",true)||!PolygonOperations.Contains(HandleHitTest.BalloonCorners(b),point))continue;
            var clip=b.Text("clipPanelId");if(clip.Length>0&&!PolygonOperations.Contains(PolygonOperations.Read(panels[clip]["polygon"]!),point))continue;
            result.Add(new("balloon",node.Id,EditMode.Balloon,null,null));
        }
        return result;
    }
    public void Reorder(string direction){if(!Ready()||model.SelectedId is null)return;model.Execute([new("object.reorder",model.SelectedId,new(){["kind"]=model.SelectedKind,["direction"]=direction})]);}
    public void SnapSelectedEdge()
    {
        try{if(!Ready()||model.SelectedKind!="panel"||model.SelectedId is null)return;if(model.Selection.Current.EdgeIndex is not {} index){model.Status="请先在画布上选择一条边。";return;}model.Execute([new("panel.snap",model.SelectedId,new(){["index"]=index})]);}
        catch(EditorException ex){model.Status=ComicEditor.Desktop.Controls.UiLabels.Message(ex);}
        finally{Changed?.Invoke();}
    }
    public void SetClip(string panelId){if(!Ready()||model.SelectedKind!="balloon"||model.SelectedId is null)return;model.Execute([new("balloon.clip",model.SelectedId,new(){["clipPanelId"]=panelId})]);}
    public void SetPanelOcclusion(string panelId,string position){if(!Ready()||model.SelectedKind!="balloon"||model.SelectedId is null)return;model.Execute([new("balloon.panelOcclusion",model.SelectedId,new(){["panelId"]=panelId,["position"]=position,["autoOrder"]=true})]);}
    public void SetLocked(bool locked){if(!Ready()||model.SelectedId is null)return;model.Execute([new("object.lock",model.SelectedId,new(){["kind"]=model.SelectedKind,["locked"]=locked})]);}
    public void CopySelected()
    {if(!Ready()||model.SelectedKind!="balloon"||model.SelectedId is null)return;var next=model.SelectedId+"_"+Guid.NewGuid().ToString("N")[..6];model.Execute([new("balloon.copy",model.SelectedId,new(){["newId"]=next,["dx"]=12,["dy"]=12})]);model.Select("balloon",next);}
}
