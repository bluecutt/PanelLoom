using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace ComicEditor.Core.Assets;
public static class ProjectBundler
{
    public static LoadedProject Bundle(LoadedProject source,string directory,CancellationToken token=default,Action<string>? validateImage=null)
    {
        token.ThrowIfCancellationRequested(); directory=Path.GetFullPath(directory);
        if(Directory.Exists(directory)||File.Exists(directory)) throw new EditorException("CONFLICT","Bundle requires a new directory",4);
        var assets=new AssetResolver().Inspect(source.Document);
        var invalid=assets.FirstOrDefault(a=>a.Error is not null); if(invalid is not null) throw new EditorException("ASSET_IO",invalid.ObjectId+": "+invalid.Error,3);
        var parent=Path.GetDirectoryName(directory)!;
        if(!Directory.Exists(parent)) throw new DirectoryNotFoundException(parent);
        var stage=Path.Combine(parent,".bundle-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stage);
        var promoted=false;
        try
        {
            Directory.CreateDirectory(PackagePath(stage,"assets")); var doc=source.Document.DeepClone(); doc.Root["assetBase"]=".";
            var mapping=new JsonArray(); var unique=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(var asset in assets)
            {
                token.ThrowIfCancellationRequested();
                if(!unique.TryGetValue(asset.Sha256!,out var reference))
                {
                    var extension=Path.GetExtension(asset.AbsolutePath).ToLowerInvariant(); if(extension.Length>12||extension.Any(c=>c!='.'&&!char.IsAsciiLetterOrDigit(c))) extension=".image";
                    reference="assets/"+asset.Sha256!.ToLowerInvariant()+extension;
                    var destination=PackagePath(stage,reference);
                    using(var input=new FileStream(asset.AbsolutePath,FileMode.Open,FileAccess.Read,FileShare.Read)) using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { input.CopyTo(output); output.Flush(true); }
                    var copied=AssetResolver.Fingerprint(destination); if(copied.Sha256!=asset.Sha256) throw new EditorException("CONFLICT","Source changed during bundle",4);
                    validateImage?.Invoke(destination); unique[asset.Sha256!]=reference;
                }
                var objects=doc.Root[asset.Kind=="panel"?"panels":"balloons"]!.AsArray(); var obj=objects.First(n=>n.Text("id")==asset.ObjectId)!; obj["sourceImage"]=reference;
                mapping.Add(new JsonObject{["kind"]=asset.Kind,["id"]=asset.ObjectId,["sourceImage"]=reference,["sha256"]=asset.Sha256,["width"]=asset.Width,["height"]=asset.Height});
            }
            var packageDoc=new ProjectDocument(doc.Root,PackagePath(stage,"project.json")); new ProjectStore().Save(packageDoc,packageDoc.SourcePath!,new());
            AtomicFile.Write(PackagePath(stage,"assets-manifest.json"),false,null,s=>JsonSerializer.Serialize(s,new JsonObject{["apiVersion"]=1,["assets"]=mapping}));
            token.ThrowIfCancellationRequested(); if(AtomicFile.Hash(source.AbsolutePath)!=source.Sha256) throw new EditorException("CONFLICT","Project changed during bundle",4);
            Directory.Move(stage,directory); promoted=true; return new ProjectStore().Load(PackagePath(directory,"project.json"));
        }
        finally { if(!promoted&&Directory.Exists(stage)&&Path.GetDirectoryName(stage)==parent&&Path.GetFileName(stage).StartsWith(".bundle-",StringComparison.Ordinal)) Directory.Delete(stage,true); }
    }
    public static string PackagePath(string directory,string relative)
    {
        directory=Path.GetFullPath(directory); var path=Path.GetFullPath(Path.Combine(directory,relative));
        if(Path.IsPathRooted(relative)||!path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new EditorException("PATH_ESCAPE","Package path escapes its root");
        return path;
    }
}
