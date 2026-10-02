using System.Text;
using ComicEditor.Core.Validation;
namespace ComicEditor.Session;
public sealed class RequestLedger(int capacity,int maxBytes=128*1024*1024)
{
    private readonly Dictionary<string,(string Hash,SessionResult Receipt)> completed=[];
    private long bytes;
    public SessionResult? Find(string id,string hash)
    {
        if(!completed.TryGetValue(id,out var item))return null;
        if(item.Hash!=hash)throw new EditorException("CONFLICT","requestId already used with a different payload",4);
        return new(item.Receipt.Envelope.DeepClone().AsObject(),item.Receipt.ExitCode);
    }
    public void EnsureCapacity(SessionResult planned)
    {if(completed.Count>=capacity||bytes+Encoding.UTF8.GetByteCount(planned.Envelope.ToJsonString())+1024>maxBytes)throw new EditorException("SESSION_CAPACITY","Session receipt capacity reached; open a new session",6);}
    public void Add(string id,string hash,SessionResult result)
    {var owned=new SessionResult(result.Envelope.DeepClone().AsObject(),result.ExitCode);completed.Add(id,(hash,owned));bytes+=Encoding.UTF8.GetByteCount(owned.Envelope.ToJsonString())+1024;}
}
