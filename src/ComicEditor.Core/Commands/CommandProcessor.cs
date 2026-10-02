using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Assets;
using System.Text.Json.Nodes;
namespace ComicEditor.Core.Commands;
public sealed class CommandProcessor
{
    public PlannedChange Plan(ProjectDocument doc,IReadOnlyList<Operation> operations)
    {
        if(operations.Count>1000) throw new EditorException("INPUT_LIMIT","At most 1000 operations");
        var clone=doc.DeepClone(); var warnings=new List<Issue>();var outcomes=new List<OperationOutcome>();
        try { for(var i=0;i<operations.Count;i++)Apply(clone,operations[i],warnings,outcomes,i); }
        catch(Exception ex) when(ex is InvalidOperationException or FormatException or System.Text.Json.JsonException or ArgumentOutOfRangeException) { throw new EditorException("ARGS",ex.Message); }
        var issues=ProjectValidator.Validate(clone);
        if(issues.Any(i=>i.Severity=="error")){var conflict=issues.FirstOrDefault(i=>i.Code=="LAYER_RELATION_CONFLICT");throw new EditorException(conflict is null?"PROJECT_INVALID":conflict.Code,string.Join("; ",issues.Where(i=>i.Severity=="error").Select(i=>i.Code+" "+i.Path)));}
        return new(clone,JsonDiff.Compare(doc.Root,clone.Root),warnings.Concat(issues.Where(i=>i.Severity=="warning")).ToArray(),outcomes);
    }
    private static void Require(JsonObject args,params string[] names) { foreach(var name in names) if(args[name] is null) throw new EditorException("ARGS","Missing argument: "+name); }
    private static JsonObject Object(JsonObject root,string key) { if(root[key] is null) root[key]=new JsonObject(); return root[key]!.AsObject(); }
    private static void Put(JsonObject destination,JsonObject args,params string[] names) { foreach(var key in names) if(args.ContainsKey(key)) destination[key]=args[key]?.DeepClone(); }
    private static void Unlocked(JsonObject obj) { if(obj.Flag("locked")) throw new EditorException("LOCKED","Object is locked: "+obj.Text("id"),5); }
    private static void Apply(ProjectDocument doc,Operation op,List<Issue> warnings,List<OperationOutcome> outcomes,int operationIndex)
    {
        var definition=CommandRegistry.Actions.FirstOrDefault(a=>a.Name==op.Op)??throw new EditorException("UNKNOWN_OPERATION","Unknown operation: "+op.Op);
        foreach(var arg in op.Args) if(!definition.AllowedArgs.Contains(arg.Key,StringComparer.Ordinal)) throw new EditorException("ARGS","Unknown argument: "+arg.Key);
        foreach(var key in new[]{"outerEnabled","enabled","locked","visible","moveImage","moveGroup","flipX","flipY","lockAspect","autoOrder"}) if(op.Args.ContainsKey(key)) _=op.Args.Flag(key);
        var args=op.Args; var root=doc.Root; var kind=definition.Kind;
        if(op.Op.StartsWith("object.")||op.Op=="asset.replace") { Require(args,"kind"); kind=args.Text("kind"); if(kind is not ("panel" or "balloon")) throw new EditorException("KIND","Expected panel or balloon"); }
        JsonArray Array(string k) { var key=k=="panel"?"panels":"balloons"; if(root[key] is null) root[key]=new JsonArray(); return root[key]!.AsArray(); }
        JsonObject Find(string k,string? id) => Array(k).Select(n=>n!.AsObject()).FirstOrDefault(n=>n.Text("id")==id)??throw new EditorException("OBJECT_NOT_FOUND","Object not found: "+k+":"+id);
        JsonObject? target=null;
        if(!op.Op.StartsWith("page.")&&!op.Op.EndsWith(".add"))
        {
            if(string.IsNullOrWhiteSpace(op.TargetId)) throw new EditorException("TARGET","Target ID required");
            target=Find(kind!,op.TargetId); if(op.Op!="object.lock") Unlocked(target);
        }
        else if(op.TargetId is not null) throw new EditorException("TARGET","This action does not accept target ID");
        var item=target!;
        if(target is not null&&op.Op is "panel.polygon" or "panel.vertex" or "panel.bounds" or "panel.move" or "panel.image" or "balloon.transform" or "balloon.move" or "object.reset" or "asset.replace") ProjectDefaults.CaptureInitial(item,kind!);
        switch(op.Op)
        {
            case "panel.add": case "balloon.add":
                Require(args,"object"); var added=args["object"]!.DeepClone().AsObject(); var id=added.Text("id");
                if(string.IsNullOrWhiteSpace(id)||Array(kind!).Any(n=>n!.Text("id")==id)) throw new EditorException("DUPLICATE_ID","Nonempty new ID required");
                ProjectDefaults.CaptureInitial(added,kind!);
                Array(kind!).Add(added);ObjectOrder.Add(doc,kind!,id); break;
            case "panel.remove":
                var clips=Array("balloon").OfType<JsonObject>().Where(b=>b.Text("clipPanelId")==op.TargetId||b["panelOcclusion"] is JsonObject rules&&rules.ContainsKey(op.TargetId!)).ToArray();
                if(args.Text("clipPolicy") is not ("" or "free")) throw new EditorException("ARGS","clipPolicy must be free");
                if(clips.Length>0&&args.Text("clipPolicy")!="free") throw new EditorException(clips.Any(b=>b.Text("clipPanelId")==op.TargetId)?"CLIP_REFERENCE":"OCCLUSION_REFERENCE","Panel is referenced by balloons");
                foreach(var b in clips) { Unlocked(b);if(b.Text("clipPanelId")==op.TargetId)b["clipPanelId"]="";if(b["panelOcclusion"] is JsonObject rules){rules.Remove(op.TargetId!);if(rules.Count==0)b.Remove("panelOcclusion");} } Array("panel").Remove(item);ObjectOrder.Remove(doc,"panel",op.TargetId!); break;
            case "balloon.remove": Array("balloon").Remove(item);ObjectOrder.Remove(doc,"balloon",op.TargetId!); break;
            case "object.rename":
                if(args.Count<=1) throw new EditorException("ARGS","newId or label required");
                if(args.ContainsKey("newId"))
                {
                    var nextId=args.Text("newId"); if(string.IsNullOrWhiteSpace(nextId)||Array(kind!).Any(n=>n!=item&&n!.Text("id")==nextId)) throw new EditorException("DUPLICATE_ID","New ID is empty or already used");
                    if(kind=="panel") foreach(var b in Array("balloon").OfType<JsonObject>().Where(b=>b.Text("clipPanelId")==op.TargetId||b["panelOcclusion"] is JsonObject rules&&rules.ContainsKey(op.TargetId!))) { Unlocked(b);if(b.Text("clipPanelId")==op.TargetId)b["clipPanelId"]=nextId;if(b["panelOcclusion"] is JsonObject rules&&rules[op.TargetId!] is {} position){var copied=position.DeepClone();rules.Remove(op.TargetId!);rules[nextId]=copied;} }
                    item["id"]=nextId;
                    ObjectOrder.Rename(doc,kind!,op.TargetId!,nextId);
                }
                Put(item,args,"label"); break;
            case "panel.polygon":
                Require(args,"points"); item["polygon"]=args["points"]!.DeepClone();
                item["edgeVisibility"]=args["edgeVisibility"]?.DeepClone()??new JsonArray(item["polygon"]!.AsArray().Select((_,i)=>(JsonNode)JsonValue.Create(ProjectView.EdgeVisible(item,i))!).ToArray()); break;
            case "panel.vertex":
                Require(args,"index","x","y"); var polygon=item["polygon"]!.AsArray(); var index=Index(args,polygon.Count); polygon[index]=new JsonArray(args.Number("x"),args.Number("y")); break;
            case "panel.bounds":
                Require(args,"x","y","width","height"); item["polygon"]=PolygonOperations.Write(PolygonOperations.Resize(PolygonOperations.Read(item["polygon"]!),new(args.Number("x"),args.Number("y"),args.Number("width"),args.Number("height")))); break;
            case "panel.move":
                Require(args,"dx","dy"); var dx=args.Number("dx"); var dy=args.Number("dy"); item["polygon"]=PolygonOperations.Write(PolygonOperations.Move(PolygonOperations.Read(item["polygon"]!),dx,dy));
                if(args.Flag("moveImage",true)) { var transform=Object(item,"imageTransform"); transform["offsetX"]=transform.Number("offsetX")+dx; transform["offsetY"]=transform.Number("offsetY")+dy; } break;
            case "panel.image":
                if(args.Count==0) throw new EditorException("ARGS","At least one image property required");
                if(args.ContainsKey("fitMode")&&args.Text("fitMode") is not ("Cover" or "Contain")) throw new EditorException("ARGS","Fit mode must be Cover or Contain");
                Put(Object(item,"imageTransform"),args,"offsetX","offsetY","scale"); Put(item,args,"fitMode","fitBiasX","fitBiasY"); break;
            case "asset.replace":
                Require(args,"sourceImage"); if(string.IsNullOrWhiteSpace(args.Text("sourceImage"))) throw new EditorException("SOURCE","Nonempty source required");
                if(args.Text("fitPolicy","preserve") is not ("preserve" or "refit")) throw new EditorException("ARGS","Expected preserve or refit");
                if(AssetReplacement.Apply(doc,item,kind!,args) is {} warning) warnings.Add(warning); break;
            case "panel.edge":
                Require(args,"index","visible"); var pi=Index(args,item["polygon"]!.AsArray().Count); var edges=Enumerable.Range(0,item["polygon"]!.AsArray().Count).Select(i=>ProjectView.EdgeVisible(item,i)).ToArray(); edges[pi]=args.Flag("visible"); item["edgeVisibility"]=new JsonArray(edges.Select(b=>(JsonNode)JsonValue.Create(b)!).ToArray()); break;
            case "panel.snap":
                Require(args,"index"); var points=PolygonOperations.Read(item["polygon"]!); var tolerance=args.Number("tolerance",root["edgeSnap"].Number("tolerance",8)); if(!double.IsFinite(tolerance)||tolerance<0) throw new EditorException("ARGS","Invalid tolerance");
                var snap=EdgeSnap.SnapEdgeDetailed(item,new ProjectView(doc).Panels,Index(args,points.Length),tolerance);
                if(snap.Status==SnapStatus.Moved){ProjectDefaults.CaptureInitial(item,"panel");item["polygon"]=PolygonOperations.Write(snap.Points);}
                outcomes.Add(new(operationIndex,op.Op,op.TargetId,snap.Status.ToString(),new(){["targetPanelId"]=snap.TargetPanelId,["targetEdgeIndex"]=snap.TargetEdgeIndex,["distance"]=snap.Distance}));break;
            case "page.border":
                if(args.Count==0) throw new EditorException("ARGS","At least one border property required"); Put(Object(root,"border"),args,"color","width","mode");
                if(args.ContainsKey("outerEnabled")) Object(root,"outerBorder")["enabled"]=args["outerEnabled"]!.DeepClone(); if(args.ContainsKey("outerWidth")) Object(root,"outerBorder")["width"]=args["outerWidth"]!.DeepClone(); break;
            case "page.snap": Require(args,"enabled"); Put(Object(root,"edgeSnap"),args,"enabled","tolerance"); break;
            case "page.canvas": if(args.Count==0) throw new EditorException("ARGS","At least one canvas property required"); Put(Object(root,"canvas"),args,"width","height","background","exportScale"); break;
            case "object.layer":
                if(!args.ContainsKey("zIndex")&&!args.ContainsKey("readingOrder")) throw new EditorException("ARGS","zIndex or readingOrder required");
                if(kind!="panel"&&args.ContainsKey("readingOrder")) throw new EditorException("ARGS","readingOrder applies only to panels");
                var oldZ=item.Number("zIndex",kind=="panel"?0:1000);var oldReading=item.Number("readingOrder");
                Put(item,args,"zIndex","readingOrder");if(oldZ!=item.Number("zIndex",kind=="panel"?0:1000)||oldReading!=item.Number("readingOrder"))ObjectOrder.Reinsert(doc,kind!,item);break;
            case "object.reorder":
                Require(args,"direction");var reordered=ObjectOrder.Reorder(doc,kind!,op.TargetId!,args.Text("direction"));
                outcomes.Add(new(operationIndex,op.Op,op.TargetId,reordered?"Moved":"NoChange",new(){["kind"]=kind,["order"]=new JsonArray(ObjectOrder.Read(doc,kind!).Select(id=>(JsonNode)JsonValue.Create(id)!).ToArray())}));break;
            case "object.lock": Require(args,"locked"); item["locked"]=args.Flag("locked"); break;
            case "balloon.transform": TransformBalloon(item,args); break;
            case "balloon.clip": Require(args,"clipPanelId"); var clip=args.Text("clipPanelId"); if(clip.Length>0) _=Find("panel",clip); item["clipPanelId"]=clip; break;
            case "balloon.panelOcclusion":
                Require(args,"panelId","position");var panelId=args.Text("panelId");var positionValue=args.Text("position");
                if(positionValue is not ("front" or "back" or "inherit"))throw new EditorException("ARGS","position must be front, back or inherit");
                _=Find("panel",panelId);
                var oldPosition=item["panelOcclusion"].Text(panelId,"inherit");
                if(positionValue=="inherit"){if(item["panelOcclusion"] is JsonObject relation){relation.Remove(panelId);if(relation.Count==0)item.Remove("panelOcclusion");}}
                else Object(item,"panelOcclusion")[panelId]=positionValue;
                var layerDelta=args.Flag("autoOrder")?OcclusionOrder.PlaceSelectedBalloon(doc,op.TargetId!):0;
                outcomes.Add(new(operationIndex,op.Op,op.TargetId,layerDelta!=0?"OrderAdjusted":oldPosition==positionValue?"NoChange":"Placed",new(){["panelId"]=panelId,["position"]=positionValue,["layerDelta"]=layerDelta,["autoOrder"]=args.Flag("autoOrder")}));
                break;
            case "balloon.group": Require(args,"groupId"); item["groupId"]=args.Text("groupId"); break;
            case "balloon.visible": Require(args,"visible"); item["visible"]=args.Flag("visible"); break;
            case "balloon.copy":
                Require(args,"newId"); var copy=item.DeepClone().AsObject(); var copyId=args.Text("newId"); if(string.IsNullOrWhiteSpace(copyId)||Array("balloon").Any(n=>n!.Text("id")==copyId)) throw new EditorException("DUPLICATE_ID","New balloon ID required");
                copy["id"]=copyId; MoveBalloon(copy,args.Number("dx"),args.Number("dy")); Array("balloon").Add(copy);ObjectOrder.Add(doc,"balloon",copyId); break;
            case "balloon.move":
                Require(args,"dx","dy"); var group=item.Text("groupId"); var targets=args.Flag("moveGroup")&&group.Length>0 ? Array("balloon").OfType<JsonObject>().Where(b=>b.Text("groupId")==group&&!b.Flag("locked")) : [item];
                foreach(var b in targets) {ProjectDefaults.CaptureInitial(b,"balloon");MoveBalloon(b,args.Number("dx"),args.Number("dy"));} break;
            case "object.reset":
                Require(args,"scope"); var scope=args.Text("scope");
                if(kind=="panel"&&scope=="image") item["imageTransform"]=item["initialImageTransform"]?.DeepClone()??new JsonObject{["scale"]=1};
                else if(kind=="panel"&&scope=="frame") { item["polygon"]=item["initialPolygon"]?.DeepClone()??item["polygon"]!.DeepClone(); item["edgeVisibility"]=new JsonArray(item["polygon"]!.AsArray().Select((_,i)=>(JsonNode)JsonValue.Create(ProjectView.EdgeVisible(item,i))!).ToArray()); }
                else if(kind=="balloon"&&scope=="balloon") item["transform"]=item["initialTransform"]?.DeepClone()??item["transform"]?.DeepClone()??new JsonObject();
                else throw new EditorException("ARGS","Invalid reset scope for object kind"); break;
        }
    }
    private static int Index(JsonObject args,int count) { var number=args.Number("index",-1); if(!double.IsFinite(number)||number!=Math.Truncate(number)||number<0||number>=count) throw new EditorException("INDEX","Index out of range"); return (int)number; }
    private static void MoveBalloon(JsonObject balloon,double dx,double dy) { var t=Object(balloon,"transform"); t["x"]=t.Number("x",100)+dx; t["y"]=t.Number("y",100)+dy; }
    private static void TransformBalloon(JsonObject item,JsonObject args)
    {
        if(args.Count==0) throw new EditorException("ARGS","At least one balloon property required");
        var t=Object(item,"transform"); var ratio=t.Number("width",240)/t.Number("height",150); var locked=args.ContainsKey("lockAspect")?args.Flag("lockAspect"):item.Flag("lockAspect",true);
        if(locked)
        {
            if(args.ContainsKey("width")&&args.ContainsKey("height")&&Math.Abs(args.Number("width")/args.Number("height")-ratio)>0.000001) throw new EditorException("ASPECT_LOCK","Disable aspect lock explicitly before stretching");
            if(args.ContainsKey("width")&&!args.ContainsKey("height")) t["height"]=args.Number("width")/ratio;
            if(args.ContainsKey("height")&&!args.ContainsKey("width")) t["width"]=args.Number("height")*ratio;
        }
        Put(t,args,"x","y","width","height","rotation","flipX","flipY","opacity"); Put(item,args,"lockAspect");
    }
}
