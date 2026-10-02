using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using ComicEditor.Core.Project;
namespace ComicEditor.Session;
public sealed record SessionRegistration(int ApiVersion,string SessionId,string PipeName,int ProcessId,string OwnerSid,DateTimeOffset StartedUtc);
public sealed class SessionRegistry(string directory)
{
    public static string CurrentSid=>WindowsIdentity.GetCurrent().User!.Value;
    public string Add(SessionRegistration record)
    {Directory.CreateDirectory(directory);var path=Path.Combine(directory,Guid.Parse(record.SessionId).ToString("N")+".json");AtomicFile.Write(path,false,null,s=>JsonSerializer.Serialize(s,record,SessionJson.Options));return path;}
    public IReadOnlyList<SessionRegistration> List()
    {
        var list=new List<SessionRegistration>();if(!Directory.Exists(directory))return list;
        foreach(var path in Directory.GetFiles(directory,"*.json"))
        {try{if(new FileInfo(path).Length>16384)continue;var r=JsonSerializer.Deserialize<SessionRegistration>(File.ReadAllText(path),SessionJson.Options);if(r is null||r.ApiVersion!=1||r.OwnerSid!=CurrentSid||!Guid.TryParse(r.SessionId,out var id)||Path.GetFileNameWithoutExtension(path)!=id.ToString("N"))continue;using var process=Process.GetProcessById(r.ProcessId);if(process.HasExited)continue;list.Add(r);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException){}}
        return list;
    }
}
