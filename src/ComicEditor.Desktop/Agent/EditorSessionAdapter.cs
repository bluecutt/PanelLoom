using System.Windows.Threading;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Session;
namespace ComicEditor.Desktop.Agent;
public sealed class EditorSessionAdapter(EditorViewModel model,Dispatcher dispatcher) : IEditorSession
{
    public long Revision=>model.Revision;
    public bool Busy=>model.InteractionBusy;
    public bool AllowWrite=>model.AllowAgentWrite;
    public ProjectDocument Snapshot()=>model.Document;
    public PlannedChange Execute(IReadOnlyList<Operation> operations)=>model.Execute(operations);
    public LoadedProject Save(string path,string? expectedOutputHash)
    {
        if(model.FilePath is {} source&&model.FileHash is {} hash&&(!File.Exists(source)||AtomicFile.Hash(source)!=hash))throw new EditorException("CONFLICT","Saved source changed externally",4);
        var expected=expectedOutputHash;if(model.FilePath is {} current&&string.Equals(Path.GetFullPath(path),current,StringComparison.OrdinalIgnoreCase))expected=model.FileHash;
        return model.Save(path,new(File.Exists(path),expected));
    }
    public Task<T> InvokeAsync<T>(Func<T> action,CancellationToken token)
    {token.ThrowIfCancellationRequested();return dispatcher.CheckAccess()?Task.FromResult(action()):dispatcher.InvokeAsync(()=>{token.ThrowIfCancellationRequested();return action();},DispatcherPriority.Send,token).Task;}
}
