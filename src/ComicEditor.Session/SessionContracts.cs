using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
namespace ComicEditor.Session;
public static class SessionHash {public static string Compute(ProjectDocument doc)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(doc.Root.ToJsonString())));}
public sealed record SessionResult(JsonObject Envelope,int ExitCode);
public interface IEditorSession
{
    long Revision {get;}
    bool Busy {get;}
    bool AllowWrite {get;}
    ProjectDocument Snapshot();
    PlannedChange Execute(IReadOnlyList<Operation> operations);
    LoadedProject Save(string path,string? expectedOutputHash);
    Task<T> InvokeAsync<T>(Func<T> action,CancellationToken token);
}
