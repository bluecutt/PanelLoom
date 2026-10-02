using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
namespace ComicEditor.Desktop.Editing;
public sealed class WheelGesture(EditorViewModel model)
{
    private EditorSelection? target;
    private ProjectDocument? baseline;
    private IReadOnlyList<Operation>? operations;
    private long revision;
    private double factor=1;
    public bool Active=>target is not null;
    public ProjectDocument? Preview {get;private set;}
    public bool Matches(EditorSelection selection)=>target is not null&&target.Kind==selection.Kind&&target.Id==selection.Id&&target.Tool==selection.Tool;
    public void Begin(EditorSelection selection,ProjectDocument document)
    {
        if(Active||model.Busy||model.InputPending)throw new EditorException("BUSY","请先结束当前操作并处理未提交输入",6);
        if(selection.Id is null||selection.Tool is not (EditMode.Image or EditMode.Balloon))throw new EditorException("TARGET","请选择要缩放的图片或气泡");
        var obj=Find(document,selection);if(obj.Flag("locked"))throw new EditorException("LOCKED","所选对象已锁定",5);
        target=selection;baseline=document.DeepClone();revision=model.Revision;factor=1;operations=null;Preview=null;model.Busy=true;
    }
    public ProjectDocument Update(int delta)
    {
        if(target is null||baseline is null)throw new InvalidOperationException("Wheel gesture not started");
        var obj=Find(baseline,target);var next=Math.Clamp(factor*Math.Pow(1.1,delta/120d),1e-9,1e9);
        string action;JsonObject args;
        if(target.Tool==EditMode.Image)
        {action="panel.image";var scale=Math.Clamp(obj["imageTransform"].Number("scale",1)*next,0.000001,10000);next=scale/obj["imageTransform"].Number("scale",1);args=new(){["scale"]=scale};}
        else
        {
            action="balloon.transform";var t=obj["transform"];var width=t.Number("width",240);var height=t.Number("height",150);
            next=Math.Clamp(next,Math.Max(0.000001/width,0.000001/height),Math.Min(100000/width,100000/height));
            var w=width*next;var h=height*next;
            args=new(){["width"]=w,["height"]=h,["x"]=t.Number("x",100)+(width-w)/2,["y"]=t.Number("y",100)+(height-h)/2};
        }
        if(Math.Abs(next-1)<1e-10){factor=1;operations=null;Preview=null;return baseline;}
        var ops=new Operation[]{new(action,target.Id,args)};var result=new CommandProcessor().Plan(baseline,ops);
        factor=next;operations=ops;Preview=result.After;return result.After;
    }
    public void Commit()
    {
        if(target is null)return;
        try{if(model.Revision!=revision)throw new EditorException("CONFLICT","滚轮操作期间工程已经变化",4);model.Busy=false;if(operations is not null)model.Execute(operations);}
        finally{Cancel();}
    }
    public void Cancel(){var owned=Active;target=null;baseline=null;operations=null;Preview=null;if(owned)model.Busy=false;}
    private static JsonObject Find(ProjectDocument doc,EditorSelection selection)=>(selection.Kind=="panel"?new ProjectView(doc).Panels:new ProjectView(doc).Balloons).FirstOrDefault(o=>o.Text("id")==selection.Id)??throw new EditorException("OBJECT_NOT_FOUND","所选对象不存在");
}
