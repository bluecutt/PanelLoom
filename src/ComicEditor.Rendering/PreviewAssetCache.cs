using System.Drawing;
using ComicEditor.Core.Validation;
namespace ComicEditor.Rendering;
public sealed class BitmapLease(Bitmap pixels,Action release) : IDisposable
{private Action? remaining=release;public Bitmap Pixels{get;}=pixels;public void Dispose()=>Interlocked.Exchange(ref remaining,null)?.Invoke();}
public sealed class PreviewAssetCache : IDisposable
{
    private sealed class Entry(string path,long epoch,long length,DateTime modified,Bitmap bitmap,bool resident)
    {public string Path=path;public long Epoch=epoch,Length=length,Stamp;public DateTime Modified=modified;public Bitmap Pixels=bitmap;public long Bytes=(long)bitmap.Width*bitmap.Height*4;public int Users;public bool Resident=resident,Retired;}
    private readonly object gate=new();private readonly Dictionary<string,Entry> current=new(StringComparer.OrdinalIgnoreCase);private readonly HashSet<Entry> live=[];private long stamp;private bool disposed;
    public long BudgetBytes{get;}public long ResidentBytes{get{lock(gate)return live.Where(e=>e.Resident).Sum(e=>e.Bytes);}}
    public PreviewAssetCache(long budgetBytes=256L*1024*1024){if(budgetBytes<=0)throw new ArgumentOutOfRangeException(nameof(budgetBytes));BudgetBytes=budgetBytes;}
    public BitmapLease Acquire(string path,long assetEpoch)
    {
        path=System.IO.Path.GetFullPath(path);var info=new FileInfo(path);if(!info.Exists)throw new EditorException("ASSET_IO","预览素材不存在："+path,3);
        lock(gate){ObjectDisposedException.ThrowIf(disposed,this);if(current.TryGetValue(path,out var cached)&&cached.Epoch==assetEpoch&&cached.Length==info.Length&&cached.Modified==info.LastWriteTimeUtc)return Lease(cached);}
        Bitmap pixels;
        try{using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);using var source=new Bitmap(stream);pixels=(Bitmap)source.Clone();}
        catch(Exception ex)when(ex is IOException or ArgumentException or System.Runtime.InteropServices.ExternalException){throw new EditorException("ASSET_IO",ex.Message,3);}
        lock(gate)
        {
            if(disposed){pixels.Dispose();throw new ObjectDisposedException(nameof(PreviewAssetCache));}
            if(current.TryGetValue(path,out var previous)){current.Remove(path);previous.Retired=true;if(previous.Users==0)Free(previous);}
            var entry=new Entry(path,assetEpoch,info.Length,info.LastWriteTimeUtc,pixels,(long)pixels.Width*pixels.Height*4<=BudgetBytes);live.Add(entry);if(entry.Resident)current[path]=entry;else entry.Retired=true;
            var lease=Lease(entry);Evict();return lease;
        }
    }
    private BitmapLease Lease(Entry e){e.Users++;e.Stamp=++stamp;return new(e.Pixels,()=>{lock(gate){e.Users--;if(e.Users==0&&e.Retired)Free(e);Evict();}});}
    private void Free(Entry e){current.Remove(e.Path,out var removed);if(removed is not null&&!ReferenceEquals(removed,e))current[e.Path]=removed;live.Remove(e);e.Pixels.Dispose();}
    private void Evict(){while(live.Where(e=>e.Resident).Sum(e=>e.Bytes)>BudgetBytes){var e=live.Where(e=>e.Resident&&e.Users==0).MinBy(e=>e.Stamp);if(e is null)break;Free(e);}}
    public void Invalidate(string? path=null){lock(gate){foreach(var e in live.Where(e=>path is null||string.Equals(e.Path,System.IO.Path.GetFullPath(path),StringComparison.OrdinalIgnoreCase)).ToArray()){e.Retired=true;current.Remove(e.Path);if(e.Users==0)Free(e);}}}
    public void Dispose(){lock(gate){disposed=true;Invalidate();}}
}
