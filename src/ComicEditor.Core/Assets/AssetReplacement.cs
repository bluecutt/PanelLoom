using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Assets;
public static class AssetReplacement
{
    public static Issue? Apply(ProjectDocument doc,JsonObject item,string kind,JsonObject args)
    {
        var source=args.Text("sourceImage"); AssetFingerprint? old=null; AssetFingerprint next; Issue? warning=null;
        try { next=AssetResolver.Fingerprint(ProjectPaths.Resolve(doc,source)); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or OverflowException) { throw new EditorException("ASSET_IO",$"{kind}:{item.Text("id")}: {ex.Message}",3); }
        try { old=AssetResolver.Fingerprint(ProjectPaths.Resolve(doc,item.Text("sourceImage"))); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or OverflowException)
        { warning=new("warning","ASSET_RELINK",item.Text("id"),source,"Previous source could not be inspected: "+ex.Message); }
        if((old is null||old.Width!=next.Width||old.Height!=next.Height)&&!args.ContainsKey("fitPolicy")) throw new EditorException("FIT_POLICY_REQUIRED","Changed or unknown previous dimensions require explicit preserve or refit");
        item["sourceImage"]=source;
        if(args.Text("fitPolicy")!="refit") return warning;
        if(kind=="panel") item["imageTransform"]=new JsonObject{["offsetX"]=0,["offsetY"]=0,["scale"]=1};
        else
        {
            var transform=item["transform"]?.AsObject()??new JsonObject(); var oldW=transform.Number("width",240); var oldH=transform.Number("height",150);
            var scale=Math.Min(oldW/next.Width,oldH/next.Height); var width=next.Width*scale; var height=next.Height*scale;
            transform["x"]=transform.Number("x",100)+(oldW-width)/2; transform["y"]=transform.Number("y",100)+(oldH-height)/2; transform["width"]=width; transform["height"]=height; item["transform"]=transform;
        }
        return warning;
    }
}
