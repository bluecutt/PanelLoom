using ComicEditor.Core.Project;
using ComicEditor.Core.Geometry;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace ComicEditor.Core.Validation;
public static class ProjectValidator
{
    public static IReadOnlyList<Issue> Validate(ProjectDocument doc)
    {
        var issues=new List<Issue>();
        void Error(string code,string? id,string path,string message)=>issues.Add(new("error",code,id,path,message));
        void Number(JsonNode? node,string key,double fallback,string path,double minimum=double.NegativeInfinity,double maximum=double.PositiveInfinity,bool integer=false)
        {
            try { var value=node.Number(key,fallback); if(!double.IsFinite(value)||value<minimum||value>maximum||(integer&&value!=Math.Truncate(value))) Error("NUMBER",null,path+"/"+key,"Invalid finite number or range"); }
            catch(Exception ex) when(ex is InvalidOperationException or System.Text.Json.JsonException or FormatException) { Error("TYPE",null,path+"/"+key,"Expected number"); }
        }
        void Polygon(JsonNode? node,string id,string path)
        {
            if(node is not JsonArray points||points.Count<3) { Error("POLYGON",id,path,"At least three vertices required"); return; }
            try
            {
                var coordinates=points.Select(p=>p is JsonArray a&&a.Count==2 ? (X:a[0]!.DeserializeNumber(),Y:a[1]!.DeserializeNumber()) : throw new FormatException()).ToArray();
                if(coordinates.Any(p=>!double.IsFinite(p.X)||!double.IsFinite(p.Y))) throw new FormatException();
                var area=0d;
                for(var i=0;i<coordinates.Length;i++) { var a=coordinates[i]; var b=coordinates[(i+1)%coordinates.Length]; area+=a.X*b.Y-b.X*a.Y; }
                if(Math.Abs(area)<0.0001) Error("POLYGON_DEGENERATE",id,path,"Polygon area is zero");
                for(var i=0;i<coordinates.Length;i++) for(var j=i+2;j<coordinates.Length;j++)
                {
                    if(i==0&&j==coordinates.Length-1) continue;
                    var a=coordinates[i]; var b=coordinates[(i+1)%coordinates.Length]; var c=coordinates[j]; var d=coordinates[(j+1)%coordinates.Length];
                    static double Cross((double X,double Y) a,(double X,double Y) b,(double X,double Y) c)=>(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
                    if(Cross(a,b,c)*Cross(a,b,d)<0&&Cross(c,d,a)*Cross(c,d,b)<0) issues.Add(new("warning","POLYGON_SELF_INTERSECTION",id,path,"Complex legacy shape retained; verify crop visually"));
                }
            }
            catch(Exception ex) when(ex is InvalidOperationException or System.Text.Json.JsonException or FormatException) { Error("POLYGON",id,path,"Expected finite [x,y] pairs"); }
        }
        var root=doc.Root;
        try
        {
            if(root.Text("format")!="ComicPanelEditorProject") Error("FORMAT",null,"/format","Unsupported format");
            if(root.Number("version")!=2) Error("VERSION",null,"/version","Only version 2 supported");
            Number(root["canvas"],"width",1024,"/canvas",1,100000,true); Number(root["canvas"],"height",1536,"/canvas",1,100000,true); Number(root["canvas"],"exportScale",2,"/canvas",0.01,100);
            Number(root["border"],"width",4,"/border",0,1000); Number(root["outerBorder"],"width",5,"/outerBorder",0,1000); Number(root["edgeSnap"],"tolerance",8,"/edgeSnap",0,10000);
            if(root["border"].Text("mode","Legacy") is not ("Legacy" or "SingleLine")) Error("BORDER_MODE",null,"/border/mode","Unknown border mode");
            foreach(var pair in new[]{(root["canvas"],"background","#FFFFFF"),(root["border"],"color","#000000")}) if(!Regex.IsMatch(pair.Item1.Text(pair.Item2,pair.Item3),"^#[0-9a-fA-F]{6}$")) Error("COLOR",null,"/"+pair.Item2,"Expected #RRGGBB");
            var panelIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var kind in new[]{"panels","balloons"})
            {
                if(root[kind] is null) continue;
                if(root[kind] is not JsonArray objects) { Error("TYPE",null,"/"+kind,"Expected array"); continue; }
                var ids=new HashSet<string>(StringComparer.Ordinal);
                for(var i=0;i<objects.Count;i++)
                {
                    var path=$"/{kind}/{i}"; if(objects[i] is not JsonObject item) { Error("TYPE",null,path,"Expected object"); continue; }
                    var id=item.Text("id"); if(string.IsNullOrWhiteSpace(id)) Error("ID",id,path,"Nonempty ID required");
                    else if(!ids.Add(id)) Error("DUPLICATE_ID",id,path,"ID repeated within object kind");
                    if(kind=="panels") panelIds.Add(id);
                    if(string.IsNullOrWhiteSpace(item.Text("sourceImage"))) Error("SOURCE",id,path+"/sourceImage","Full original source reference required");
                    Number(item,"zIndex",0,path,int.MinValue,int.MaxValue,true);
                    if(kind=="panels")
                    {
                        Number(item,"readingOrder",0,path,int.MinValue,int.MaxValue,true);
                        Polygon(item["polygon"],id,path+"/polygon"); if(item["initialPolygon"] is not null) Polygon(item["initialPolygon"],id,path+"/initialPolygon");
                        if(item["edgeVisibility"] is JsonArray edges && item["polygon"] is JsonArray points && edges.Count!=points.Count) Error("EDGES",id,path,"Edge visibility length must match vertex count");
                        foreach(var key in new[]{"offsetX","offsetY"}) Number(item["imageTransform"],key,0,path+"/imageTransform");
                        Number(item["imageTransform"],"scale",1,path+"/imageTransform",0.000001,10000);
                        Number(item,"fitBiasX",0,path,-1,1); Number(item,"fitBiasY",0,path,-1,1);
                    }
                    else
                    {
                        foreach(var key in new[]{"x","y","rotation"}) Number(item["transform"],key,0,path+"/transform");
                        Number(item["transform"],"width",240,path+"/transform",0.000001,100000); Number(item["transform"],"height",150,path+"/transform",0.000001,100000); Number(item["transform"],"opacity",1,path+"/transform",0,1);
                    }
                }
            }
            foreach(var item in new ProjectView(doc).Balloons) if(item.Text("clipPanelId") is {Length:>0} clip&&!panelIds.Contains(clip)) Error("CLIP_REFERENCE",item.Text("id"),"/balloons/clipPanelId","Clip panel not found");
            foreach(var balloon in new ProjectView(doc).Balloons.Where(b=>b.ContainsKey("panelOcclusion")))
            {
                if(balloon["panelOcclusion"] is not JsonObject rules){Error("PANEL_OCCLUSION",balloon.Text("id"),"/balloons/panelOcclusion","Expected ID-to-front/back object");continue;}
                foreach(var (id,position) in rules)
                {
                    if(!panelIds.Contains(id))Error("OCCLUSION_REFERENCE",balloon.Text("id"),"/balloons/panelOcclusion/"+id,"Target panel missing");
                    if(position is not JsonValue value||!value.TryGetValue<string>(out var text)||text is not ("front" or "back"))Error("PANEL_OCCLUSION",balloon.Text("id"),"/balloons/panelOcclusion/"+id,"Expected front or back");
                }
            }
            if(root.ContainsKey("objectOrder"))
            {
                if(root["objectOrder"] is not JsonObject order)Error("OBJECT_ORDER",null,"/objectOrder","Expected object");
                else foreach(var (key,value) in order)
                {
                    if(key is not ("panels" or "balloons")||value is not JsonArray list){Error("OBJECT_ORDER",null,"/objectOrder/"+key,"Expected panels/balloons ID array");continue;}
                    if(list.Any(n=>n is not JsonValue valueNode||!valueNode.TryGetValue<string>(out var id)||string.IsNullOrWhiteSpace(id))){Error("OBJECT_ORDER",null,"/objectOrder/"+key,"Expected nonempty string IDs");continue;}
                    var actual=(root[key]?.AsArray()??[]).Select(n=>n!.Text("id")).ToHashSet(StringComparer.Ordinal);
                    var ids=list.Select(n=>n!.GetValue<string>()).ToArray();
                    if(ids.Length!=actual.Count||ids.Distinct(StringComparer.Ordinal).Count()!=ids.Length||!actual.SetEquals(ids))Error("OBJECT_ORDER",null,"/objectOrder/"+key,"IDs must include each existing object exactly once");
                }
            }
            if(!issues.Any(i=>i.Severity=="error")&&new ProjectView(doc).Balloons.Any(b=>b["panelOcclusion"] is JsonObject rules&&rules.Count>0))
            {try{_=OcclusionOrder.Build(doc);}catch(EditorException ex){Error(ex.Code,null,"/balloons/panelOcclusion",ex.Message);}}
        }
        catch(Exception ex) when(ex is InvalidOperationException or System.Text.Json.JsonException or FormatException) { Error("TYPE",null,"/",ex.Message); }
        return issues;
    }
}
internal static class NumericNode
{
    public static double DeserializeNumber(this JsonNode node)=>System.Text.Json.JsonSerializer.Deserialize<double>(node.ToJsonString());
}
