using System.Text.Json.Nodes;
namespace ComicEditor.Core.Commands;
public static class JsonDiff
{
    public static IReadOnlyList<Change> Compare(JsonObject before,JsonObject after)
    {
        var changes=new List<Change>();
        foreach(var key in before.Select(p=>p.Key).Union(after.Select(p=>p.Key)))
        {
            if(key is "panels" or "balloons")
            {
                var old=(before[key]?.AsArray()??[]).Select((n,i)=>(Node:n!,Index:i)).ToDictionary(x=>x.Node["id"]!.GetValue<string>());
                var next=(after[key]?.AsArray()??[]).Select((n,i)=>(Node:n!,Index:i)).ToDictionary(x=>x.Node["id"]!.GetValue<string>());
                foreach(var id in old.Keys.Union(next.Keys)) { old.TryGetValue(id,out var a); next.TryGetValue(id,out var b); Visit(a.Node,b.Node,$"/{key}/{(b.Node is not null ? b.Index : a.Index)}",(key=="panels"?"panel:":"balloon:")+id,changes); }
            }
            else Visit(before[key],after[key],"/"+Escape(key),"page",changes);
        }
        return changes;
    }
    private static string Escape(string value)=>value.Replace("~","~0").Replace("/","~1");
    private static void Visit(JsonNode? before,JsonNode? after,string path,string id,List<Change> changes)
    {
        if(JsonNode.DeepEquals(before,after)) return;
        if(before is JsonObject a&&after is JsonObject b) { foreach(var key in a.Select(p=>p.Key).Union(b.Select(p=>p.Key))) Visit(a[key],b[key],path+"/"+Escape(key),id,changes); }
        else if(before is JsonArray x&&after is JsonArray y&&x.Count==y.Count) { for(var i=0;i<x.Count;i++) Visit(x[i],y[i],path+"/"+i,id,changes); }
        else changes.Add(new(id,path,before?.DeepClone(),after?.DeepClone()));
    }
}
