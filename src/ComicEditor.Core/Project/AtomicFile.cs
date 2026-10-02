using System.Security.Cryptography;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Project;
public static class AtomicFile
{
    public static string Hash(string path) { using var stream=File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    public static void CheckDestination(string path,bool overwrite,string? expectedHash)
    {
        if (!File.Exists(path)) { if (expectedHash is not null) throw new EditorException("CONFLICT","Expected destination no longer exists",4); return; }
        if (!overwrite || string.IsNullOrWhiteSpace(expectedHash) || !string.Equals(Hash(path),expectedHash,StringComparison.OrdinalIgnoreCase))
            throw new EditorException("CONFLICT","Existing output requires overwrite and its current SHA256",4);
        if ((File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) throw new UnauthorizedAccessException("Destination is read-only");
    }
    public static void Write(string path,bool overwrite,string? expectedHash,Action<Stream> write,Action<string>? beforeCommit=null)
    {
        path=Path.GetFullPath(path);
        using var mutex=new Mutex(false,"Local\\ComicEditor.Save."+Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(path.ToUpperInvariant()))));
        try { if(!mutex.WaitOne(TimeSpan.FromSeconds(30))) throw new EditorException("BUSY","Output is busy",6); }
        catch(AbandonedMutexException) { /* The abandoned lock is owned by this thread. */ }
        var temporary=Path.Combine(Path.GetDirectoryName(path)!,"."+Path.GetFileName(path)+"."+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            CheckDestination(path,overwrite,expectedHash);
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,FileOptions.WriteThrough)) { write(stream); stream.Flush(true); }
            beforeCommit?.Invoke(path);
            CheckDestination(path,overwrite,expectedHash);
            if(File.Exists(path)) File.Replace(temporary,path,path+".backup-"+Guid.NewGuid().ToString("N"),true);
            else File.Move(temporary,path,false);
        }
        finally { if(File.Exists(temporary)) File.Delete(temporary); mutex.ReleaseMutex(); }
    }
}
