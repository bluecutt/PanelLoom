using System.Text.Json;
using System.Text.Json.Nodes;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Project;
public sealed class ProjectStore(Action<string>? beforeCommit = null)
{
    public LoadedProject Load(string path)
    {
        path=Path.GetFullPath(path);
        if(new FileInfo(path).Length>64*1024*1024) throw new EditorException("INPUT_LIMIT","Project exceeds 64 MiB");
        var bytes=File.ReadAllBytes(path);
        JsonObject root;
        try { root=JsonNode.Parse(bytes,documentOptions:new JsonDocumentOptions{MaxDepth=100})?.AsObject() ?? throw new JsonException("Empty project"); }
        catch(Exception ex) when(ex is JsonException or InvalidOperationException) { throw new EditorException("JSON",ex.Message); }
        var document=new ProjectDocument(root,path); EnsureValid(document);
        return new(document,path,Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
    }
    public LoadedProject Save(ProjectDocument doc,string path,SaveOptions options)
    {
        EnsureValid(doc); path=Path.GetFullPath(path);
        var clone=doc.DeepClone();
        clone.Root["assetBase"]=Path.GetRelativePath(Path.GetDirectoryName(path)!,ProjectPaths.AssetBase(doc)).Replace('\\','/');
        AtomicFile.Write(path,options.Overwrite,options.ExpectedDestinationHash,stream=>JsonSerializer.Serialize(stream,clone.Root,new JsonSerializerOptions { WriteIndented=true }),beforeCommit);
        return Load(path);
    }
    private static void EnsureValid(ProjectDocument doc)
    {
        var errors=ProjectValidator.Validate(doc).Where(i=>i.Severity=="error").ToArray();
        if(errors.Length>0) throw new EditorException("PROJECT_INVALID",string.Join("; ",errors.Select(i=>$"{i.Code} {i.Path}: {i.Message}")));
    }
}
