using ComicEditor.Core.Project;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Validation;
using System.Text.Json.Nodes;
namespace ComicEditor.Core.History;
public sealed class ProjectHistory(ProjectDocument initial)
{
    private ProjectDocument current=initial.DeepClone();
    private readonly List<ProjectDocument> undo=[];
    private readonly List<ProjectDocument> redo=[];
    public bool CanUndo=>undo.Count>0;
    public bool CanRedo=>redo.Count>0;
    public ProjectDocument Snapshot()=>current.DeepClone();
    public void RebaseSource(string? path)
    {
        if(path is null)return;
        ProjectDocument Rebase(ProjectDocument d) { var clone=d.DeepClone(); clone.Root["assetBase"]=Path.GetRelativePath(Path.GetDirectoryName(path)!,ProjectPaths.AssetBase(d)).Replace('\\','/'); return clone with {SourcePath=path}; }
        current=Rebase(current); for(var i=0;i<undo.Count;i++)undo[i]=Rebase(undo[i]); for(var i=0;i<redo.Count;i++)redo[i]=Rebase(redo[i]);
    }
    public void Commit(ProjectDocument before,PlannedChange change)
    {
        if(!JsonNode.DeepEquals(current.Root,before.Root)) throw new EditorException("CONFLICT","History base differs from current state",4);
        if(change.Changes.Count==0) return;
        undo.Add(current.DeepClone()); if(undo.Count>100) undo.RemoveAt(0); redo.Clear(); current=change.After.DeepClone();
    }
    public ProjectDocument Undo() { if(undo.Count>0) { redo.Add(current.DeepClone()); current=undo[^1]; undo.RemoveAt(undo.Count-1); } return Snapshot(); }
    public ProjectDocument Redo() { if(redo.Count>0) { undo.Add(current.DeepClone()); current=redo[^1]; redo.RemoveAt(redo.Count-1); } return Snapshot(); }
}
