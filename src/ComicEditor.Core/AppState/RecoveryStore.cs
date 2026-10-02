using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace ComicEditor.Core.AppState;
public sealed record RecoverySnapshot(string Path,ProjectDocument Document,string? SourceHash,long Revision,DateTimeOffset CreatedUtc);
public sealed class RecoveryStore(string directory)
{
    public RecoverySnapshot Write(string id,ProjectDocument doc,string? hash,long revision)
    {
        if(!Guid.TryParse(id,out var key))throw new EditorException("RECOVERY_ID","Recovery ID must be UUID");
        if(revision<0||ProjectValidator.Validate(doc).Any(i=>i.Severity=="error"))throw new EditorException("RECOVERY","Invalid recovery document");
        Directory.CreateDirectory(directory);var path=Path.Combine(Path.GetFullPath(directory),key.ToString("N")+".recovery.json");var now=DateTimeOffset.UtcNow;
        var root=new JsonObject{["format"]="ComicEditorRecovery",["version"]=1,["sourcePath"]=doc.SourcePath,["sourceHash"]=hash,["revision"]=revision,["createdUtc"]=now,["project"]=doc.Root.DeepClone()};
        AtomicFile.Write(path,File.Exists(path),File.Exists(path)?AtomicFile.Hash(path):null,s=>JsonSerializer.Serialize(s,root));return new(path,doc.DeepClone(),hash,revision,now);
    }
    public RecoverySnapshot Read(string path)
    {
        if(new FileInfo(path).Length>64*1024*1024)throw new EditorException("INPUT_LIMIT","Recovery too large");
        var root=JsonNode.Parse(File.ReadAllText(path))!.AsObject();if(root.Text("format")!="ComicEditorRecovery"||root.Number("version")!=1)throw new EditorException("RECOVERY","Unknown recovery format");
        var doc=new ProjectDocument(root["project"]!.DeepClone().AsObject(),root["sourcePath"]?.GetValue<string>());if(ProjectValidator.Validate(doc).Any(i=>i.Severity=="error"))throw new EditorException("RECOVERY","Invalid snapshot");
        return new(Path.GetFullPath(path),doc,root["sourceHash"]?.GetValue<string>(),root["revision"]!.GetValue<long>(),root["createdUtc"]!.GetValue<DateTimeOffset>());
    }
    public IEnumerable<string> List()=>Directory.Exists(directory)?Directory.GetFiles(directory,"*.recovery.json").OrderByDescending(File.GetLastWriteTimeUtc):[];
}
