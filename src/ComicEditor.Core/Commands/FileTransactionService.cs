using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Commands;
public sealed record PatchRequest(string RequestId,string BaseProjectHash,long? BaseRevision,IReadOnlyList<Operation> Operations);
public static class FileTransactionService
{
    public static PatchRequest ReadPatch(string path)
    {
        if(new FileInfo(path).Length>8*1024*1024) throw new EditorException("INPUT_LIMIT","Patch exceeds 8 MiB");
        return ParsePatch(JsonNode.Parse(File.ReadAllText(path))?.AsObject()??throw new EditorException("JSON","Invalid patch"));
    }
    public static PatchRequest ParsePatch(JsonObject root)
    {
        if(root.Any(p=>p.Key is not ("apiVersion" or "requestId" or "baseProjectHash" or "baseRevision" or "operations"))||root.Number("apiVersion")!=1) throw new EditorException("PATCH","Expected API1 patch");
        var requestId=root.Text("requestId"); var hash=root.Text("baseProjectHash");
        if(!Guid.TryParse(requestId,out _)||hash.Length!=64||hash.Any(c=>!Uri.IsHexDigit(c))) throw new EditorException("PATCH","UUID requestId and SHA256 baseProjectHash required");
        if(root["operations"] is not JsonArray array||array.Count>1000) throw new EditorException("INPUT_LIMIT","Operations array required, maximum 1000");
        var operations=new List<Operation>();
        foreach(var node in array)
        {
            if(node is not JsonObject item||item.Any(p=>p.Key is not ("op" or "targetId" or "args"))||item["args"] is not JsonObject args) throw new EditorException("PATCH","Invalid operation object");
            operations.Add(new(item.Text("op"),item["targetId"]?.GetValue<string>(),(JsonObject)args.DeepClone()));
        }
        long? revision=null; if(root["baseRevision"] is not null) { var number=root.Number("baseRevision"); if(number<0||number>long.MaxValue||number!=Math.Truncate(number)) throw new EditorException("PATCH","Invalid baseRevision"); revision=(long)number; }
        return new(requestId,hash,revision,operations);
    }
    public static PlannedChange Plan(LoadedProject project,PatchRequest patch,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(!string.Equals(project.Sha256,patch.BaseProjectHash,StringComparison.OrdinalIgnoreCase)) throw new EditorException("CONFLICT","Patch baseProjectHash does not match source",4);
        return new CommandProcessor().Plan(project.Document,patch.Operations);
    }
    public static LoadedProject Save(LoadedProject source,PlannedChange plan,string outputPath,SaveOptions options,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var store=new ProjectStore(_=> { token.ThrowIfCancellationRequested(); if(!string.Equals(AtomicFile.Hash(source.AbsolutePath),source.Sha256,StringComparison.OrdinalIgnoreCase)) throw new EditorException("CONFLICT","Source changed before commit",4); });
        return store.Save(plan.After,outputPath,options);
    }
}
