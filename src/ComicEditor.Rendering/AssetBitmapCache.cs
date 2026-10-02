using System.Drawing;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Validation;
namespace ComicEditor.Rendering;
public sealed class AssetBitmapCache : IDisposable
{
    private readonly Dictionary<string,(string Hash,Bitmap Bitmap)> cache=new(StringComparer.OrdinalIgnoreCase);
    public Bitmap Get(AssetInspection asset)
    {
        if(asset.Error is not null) throw new EditorException("ASSET_IO",asset.ObjectId+": "+asset.Error,3);
        var current=AssetResolver.Fingerprint(asset.AbsolutePath);
        if(current.Sha256!=asset.Sha256) throw new EditorException("ASSET_CHANGED","Source changed during rendering: "+asset.ObjectId,4);
        if(cache.TryGetValue(asset.AbsolutePath,out var entry)&&entry.Hash==current.Sha256) return entry.Bitmap;
        if(entry.Bitmap is not null) entry.Bitmap.Dispose();
        try { var bitmap=new Bitmap(asset.AbsolutePath); cache[asset.AbsolutePath]=(current.Sha256,bitmap); return bitmap; }
        catch(Exception ex) when(ex is ArgumentException or IOException or System.Runtime.InteropServices.ExternalException) { throw new EditorException("ASSET_DECODE",asset.ObjectId+": "+ex.Message,3); }
    }
    public void VerifySources(IEnumerable<AssetInspection> sources)
    {
        foreach(var asset in sources.DistinctBy(a=>a.AbsolutePath,StringComparer.OrdinalIgnoreCase)) if(AssetResolver.Fingerprint(asset.AbsolutePath).Sha256!=asset.Sha256) throw new EditorException("ASSET_CHANGED","Source changed during rendering: "+asset.ObjectId,4);
    }
    public void Dispose() { foreach(var item in cache.Values) item.Bitmap.Dispose(); cache.Clear(); }
}
