using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Core.History;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.Canvas;
namespace ComicEditor.Desktop.ViewModels;
public sealed class EditorViewModel
{
    private ProjectHistory history;
    private string savedJson;
    public EditorViewModel(ProjectDocument document) { history=new(document); savedJson=document.Root.ToJsonString(); Drafts.Changed+=()=>PreviewChanged?.Invoke(); }
    public ProjectDocument Document=>history.Snapshot();
    public string? FilePath { get; private set; }
    public string? FileHash { get; private set; }
    public EditorSelectionState Selection { get; } = new();
    public InspectorDraftStore Drafts { get; } = new();
    public Func<DraftDecision>? ResolveDrafts { get; set; }
    public string SelectedKind => Selection.Current.Kind;
    public string? SelectedId => Selection.Current.Id;
    public long Revision { get; private set; }
    public long AssetEpoch {get;private set;}
    public event Action? AssetsChanged;
    private bool busy,pendingInput,allowAgentWrite;
    private int modalDepth;
    public bool Busy {get=>Volatile.Read(ref busy);set=>Volatile.Write(ref busy,value);}
    public bool InputPending {get=>Volatile.Read(ref pendingInput)||Drafts.HasPending;set=>Volatile.Write(ref pendingInput,value);}
    public int ModalDepth {get=>Volatile.Read(ref modalDepth);set=>Volatile.Write(ref modalDepth,value);}
    public bool InteractionBusy=>Busy||InputPending||ModalDepth>0;
    public bool AllowAgentWrite {get=>Volatile.Read(ref allowAgentWrite);set=>Volatile.Write(ref allowAgentWrite,value);}
    public bool Dirty=>savedJson!=Document.Root.ToJsonString();
    public string Status { get; set; }="就绪";
    public event Action? Changed;
    public event Action? PreviewChanged;
    public JsonObject? Selected=>SelectedKind=="page"?null:(SelectedKind=="panel"?new ProjectView(Document).Panels:new ProjectView(Document).Balloons).FirstOrDefault(o=>o.Text("id")==SelectedId);
    public void Select(string kind,string? id,bool activateTool=true) { if((kind!=SelectedKind||id!=SelectedId)&&!LeaveForChange())return;Selection.Select(Document,kind,id,activateTool);Notify(); }
    public void ActivateKind(string kind) { if(kind!=SelectedKind&&!LeaveForChange())return;Selection.ActivateKind(Document,kind);Notify(); }
    public void SetTool(EditMode tool)
    {
        if(tool!=Selection.Current.Tool&&!LeaveForChange())return;
        if(tool==EditMode.Balloon&&SelectedKind!="balloon")Selection.ActivateKind(Document,"balloon");
        else if(tool is EditMode.Image or EditMode.Panel or EditMode.Vertex or EditMode.FrameOnly or EditMode.Edge && SelectedKind!="panel")Selection.ActivateKind(Document,"panel");
        Selection.SetTool(tool);Notify();
    }
    public void SelectEdge(int? index) {if(index!=Selection.Current.EdgeIndex&&!LeaveForChange())return;Selection.SelectEdge(index);Notify();}
    public void SelectVertex(int? index) {if(index!=Selection.Current.VertexIndex&&!LeaveForChange())return;Selection.SelectVertex(index);Notify();}
    public PlannedChange Execute(IReadOnlyList<Operation> operations)
    {
        if(InputPending)throw new EditorException("BUSY","请先应用或放弃尚未提交的输入",6);
        return ExecuteCore(operations,true);
    }
    internal PlannedChange CommitDraftOperations(IReadOnlyList<Operation> operations)=>ExecuteCore(operations,false);
    internal void RefreshAfterDraft() {InputPending=false;Notify();}
    private PlannedChange ExecuteCore(IReadOnlyList<Operation> operations,bool notify)
    {
        if(Busy) throw new EditorException("BUSY","Editor is busy",6);
        var before=Document.DeepClone(); var plan=new CommandProcessor().Plan(before,operations); history.Commit(before,plan);if(plan.Changes.Count>0)Revision++;
        foreach(var op in operations.Where(o=>o.Op=="object.rename"&&o.Args.ContainsKey("newId"))){var kind=op.Args.Text("kind");var next=op.Args.Text("newId");Selection.Rename(kind,op.TargetId!,next);if(notify)Drafts.RemapIdentity(kind,op.TargetId!,next);}
        if(operations.Any(o=>o.Op=="asset.replace")){AssetEpoch++;AssetsChanged?.Invoke();}Status=Describe(plan);if(notify)Notify();return plan;
    }
    private static string Describe(PlannedChange plan)
    {
        var outcome=plan.Outcomes?.LastOrDefault();
        if(outcome?.Op=="panel.snap")return outcome.Code switch
        {"Moved"=>$"已吸附到 {outcome.Data.Text("targetPanelId")} 的边 {(int)outcome.Data.Number("targetEdgeIndex")+1} · 可撤销",
         "AlreadyAligned"=>"选中边已对齐，无需移动。","NoCandidate"=>"附近没有满足条件的可吸附边。","Unsafe"=>"形状安全校验拒绝，未移动该边。",_=>"吸附未执行。"};
        if(outcome?.Op=="object.reorder")return outcome.Code=="Moved"?"已移动相邻一层 · 可撤销":"已位于当前类型的边界，未改变层级。";
        if(outcome?.Op=="balloon.panelOcclusion")
        {
            var panel=outcome.Data.Text("panelId");var position=outcome.Data.Text("position");var delta=(int)outcome.Data.Number("layerDelta");
            var placement=position=="inherit"?$"已恢复与 {panel} 的原遮挡行为":$"已置于 {panel} {(position=="front"?"前方":"后方")}";
            return outcome.Code=="NoChange"?"遮挡关系已符合要求，无需改变。":placement+(delta!=0?$"；当前气泡自动{(delta>0?"上移":"下移")} {Math.Abs(delta)} 层，其他对象相互顺序不变":"")+" · 可撤销";
        }
        return plan.Changes.Count>0?"已应用 · 可撤销":"没有变化，无需撤销";
    }
    private bool LeaveForChange() {if(!TryLeaveContext(InputPending?ResolveDrafts?.Invoke()??DraftDecision.Cancel:DraftDecision.Discard)){Notify();return false;}return true;}
    public bool TryLeaveContext(DraftDecision decision)
    {
        if(Busy){Status="请先结束当前拖动或操作。";return false;}
        if(!InputPending)return true;
        if(decision==DraftDecision.Cancel)return false;
        if(decision==DraftDecision.Discard){DiscardDrafts();return true;}
        try {Drafts.CommitAll(this);InputPending=false;return true;}
        catch(EditorException ex){Status=ex.Message;return false;}
    }
    public void DiscardDrafts() {Drafts.DiscardAll();InputPending=false;Notify();}
    public void Undo() { if(!LeaveForChange())return; if(history.CanUndo) { history.Undo(); Revision++;Notify(); } }
    public void Redo() { if(!LeaveForChange())return; if(history.CanRedo) { history.Redo(); Revision++;Notify(); } }
    public void Open(string path)
    {
        if(Busy||InputPending)throw new EditorException("BUSY","请先结束操作并处理未提交输入",6);
        var loaded=new ProjectStore().Load(path); history=new(loaded.Document); FilePath=loaded.AbsolutePath; FileHash=loaded.Sha256; savedJson=Document.Root.ToJsonString(); Revision++; Selection.Select(Document,"page",null); InputPending=false; Notify();
    }
    public bool TryOpen(string path,Func<bool> confirmDiscard) {if((Dirty||InputPending)&&!confirmDiscard())return false;Open(path);return true; }
    public void Restore(ComicEditor.Core.AppState.RecoverySnapshot snapshot)
    {if(Busy||InputPending)throw new EditorException("BUSY","请先结束操作并处理未提交输入",6);history=new(snapshot.Document);FilePath=snapshot.Document.SourcePath;FileHash=snapshot.SourceHash;savedJson="";Revision++;Selection.Select(Document,"page",null);InputPending=false;Notify();}
    public LoadedProject Save(string path,SaveOptions options)
    {
        if(Busy||InputPending)throw new EditorException("BUSY","请先结束操作并处理未提交输入",6);
        var saved=new ProjectStore().Save(Document,path,options); history.RebaseSource(saved.Document.SourcePath); FilePath=saved.AbsolutePath; FileHash=saved.Sha256; savedJson=Document.Root.ToJsonString(); InputPending=false; Status="已保存";Revision++; Notify(); return saved;
    }
    private void Notify()
    {var previous=Selection.Current;Selection.Reconcile(Document);if((previous.EdgeIndex is not null&&Selection.Current.EdgeIndex is null)||(previous.VertexIndex is not null&&Selection.Current.VertexIndex is null))Status="原选中边或顶点已失效，请重新选择。";if(Changed is null)return;foreach(var subscriber in Changed.GetInvocationList())try{((Action)subscriber)();}catch(Exception ex){Status="工程已提交，界面刷新异常："+ex.Message;}}
}
