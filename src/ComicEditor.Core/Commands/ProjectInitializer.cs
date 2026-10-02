using ComicEditor.Core.Project;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Validation;
using System.Text.Json.Nodes;
namespace ComicEditor.Core.Commands;
public static class ProjectInitializer
{
    public static ProjectDocument Create(string manifestPath)
    {
        manifestPath=Path.GetFullPath(manifestPath);
        if(new FileInfo(manifestPath).Length>8*1024*1024) throw new EditorException("INPUT_LIMIT","Manifest exceeds 8 MiB");
        var root=JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject()??throw new EditorException("JSON","Invalid manifest");
        if(root.Number("apiVersion")!=1||root.Any(p=>p.Key is not ("apiVersion" or "project" or "assetMap"))) throw new EditorException("MANIFEST","Expected API1 manifest");
        if(root["project"] is not JsonObject project||root["assetMap"] is not JsonArray map) throw new EditorException("MANIFEST","project and assetMap required");
        var doc=new ProjectDocument(project.DeepClone().AsObject(),manifestPath); var ids=new HashSet<string>();
        foreach(var entry in map)
        {
            if(entry is not JsonObject item||item.Any(p=>p.Key is not ("kind" or "id" or "sourceImage"))) throw new EditorException("MANIFEST","Invalid asset mapping");
            var kind=item.Text("kind"); var id=item.Text("id"); if(kind is not ("panel" or "balloon")||!ids.Add(kind+":"+id)) throw new EditorException("MANIFEST","Invalid or duplicate mapping");
            var array=doc.Root[kind=="panel"?"panels":"balloons"]?.AsArray(); var target=array?.OfType<JsonObject>().FirstOrDefault(n=>n.Text("id")==id)??throw new EditorException("OBJECT_NOT_FOUND","Unknown mapped ID: "+id);
            target["sourceImage"]=item.Text("sourceImage");
        }
        ProjectDefaults.CaptureInitial(doc);
        var issues=ProjectValidator.Validate(doc); if(issues.Any(i=>i.Severity=="error")) throw new EditorException("PROJECT_INVALID",string.Join(";",issues.Select(i=>i.Code+" "+i.Path)));
        var missing=new AssetResolver().Inspect(doc).Where(i=>i.Error is not null).ToArray(); if(missing.Length>0) throw new EditorException("ASSET_IO",string.Join(";",missing.Select(i=>i.Kind+":"+i.ObjectId+" "+i.Error)),3);
        return doc;
    }
}
