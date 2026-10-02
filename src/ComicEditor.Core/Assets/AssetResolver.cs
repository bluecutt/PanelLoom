using ComicEditor.Core.Project;
using System.Security.Cryptography;
namespace ComicEditor.Core.Assets;
public sealed class AssetResolver
{
    public string Resolve(ProjectDocument doc,string reference)=>ProjectPaths.Resolve(doc,reference);
    public IReadOnlyList<AssetInspection> Inspect(ProjectDocument doc)
    {
        var results=new List<AssetInspection>(); var view=new ProjectView(doc);
        foreach(var pair in new[]{("panel",view.Panels),("balloon",view.Balloons)}) foreach(var obj in pair.Item2)
        {
            var reference=obj.Text("sourceImage"); var path=Resolve(doc,reference);
            try { var fingerprint=Fingerprint(path); results.Add(new(pair.Item1,obj.Text("id"),reference,path,fingerprint.Width,fingerprint.Height,fingerprint.Sha256,false,null)); }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException or OverflowException) { results.Add(new(pair.Item1,obj.Text("id"),reference,path,null,null,null,!File.Exists(path),ex.Message)); }
        }
        return results;
    }
    public static AssetFingerprint Fingerprint(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        var dimensions=ImageHeaders.Read(stream);
        if(dimensions.Width<=0||dimensions.Height<=0) throw new IOException("Invalid image dimensions");
        stream.Position=0; var hash=Convert.ToHexString(SHA256.HashData(stream));
        return new(dimensions.Width,dimensions.Height,hash,stream.Length,File.GetLastWriteTimeUtc(path));
    }
}
