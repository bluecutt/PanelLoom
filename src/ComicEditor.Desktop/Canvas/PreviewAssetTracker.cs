using ComicEditor.Core.Project;
namespace ComicEditor.Desktop.Canvas;
public sealed class PreviewAssetTracker : IDisposable
{
    private readonly List<FileSystemWatcher> watchers=[];private readonly HashSet<string> paths=new(StringComparer.OrdinalIgnoreCase);private long epoch;private bool disposed;private readonly object gate=new();
    public long Epoch=>Interlocked.Read(ref epoch);public event Action? Changed;
    public void Invalidate(){Interlocked.Increment(ref epoch);Changed?.Invoke();}
    public void Reset(ProjectDocument doc)
    {
        var view=new ProjectView(doc);var next=view.Panels.Concat(view.Balloons).Select(p=>ProjectPaths.Resolve(doc,p.Text("sourceImage"))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        lock(gate){if(disposed||paths.SetEquals(next))return;foreach(var w in watchers)w.Dispose();watchers.Clear();paths.Clear();paths.UnionWith(next);
            foreach(var dir in next.Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase).Where(d=>d is not null&&Directory.Exists(d))){var w=new FileSystemWatcher(dir!){NotifyFilter=NotifyFilters.FileName|NotifyFilters.LastWrite|NotifyFilters.Size};w.Changed+=OnChange;w.Created+=OnChange;w.Deleted+=OnChange;w.Renamed+=OnRename;w.Error+=(_,_)=>Invalidate();watchers.Add(w);w.EnableRaisingEvents=true;}}
        Invalidate();
    }
    private void OnChange(object sender,FileSystemEventArgs e){bool relevant;lock(gate)relevant=!disposed&&paths.Contains(e.FullPath);if(relevant)Invalidate();}
    private void OnRename(object sender,RenamedEventArgs e){bool relevant;lock(gate)relevant=!disposed&&(paths.Contains(e.FullPath)||paths.Contains(e.OldFullPath));if(relevant)Invalidate();}
    public void Dispose(){lock(gate){disposed=true;foreach(var w in watchers)w.Dispose();watchers.Clear();paths.Clear();}}
}
