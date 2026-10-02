namespace ComicEditor.Desktop.Canvas;
public sealed class PreviewCoordinator<T> : IDisposable
{
    private readonly object gate=new();
    private CancellationTokenSource? pending;
    private long generation;
    private long latestRevision=-1;
    private bool disposed;
    public async Task RequestAsync(long revision,Func<CancellationToken,Task<T>> render,Action<T> publish)
    {
        CancellationTokenSource local;long request;
        lock(gate) {if(disposed||revision<latestRevision)return;latestRevision=revision;pending?.Cancel();local=new();pending=local;request=++generation;}
        try {var value=await render(local.Token).ConfigureAwait(false);lock(gate){if(!disposed&&!local.IsCancellationRequested&&request==generation)publish(value);}}
        catch(OperationCanceledException)when(local.IsCancellationRequested) { }
        finally {lock(gate){if(ReferenceEquals(pending,local))pending=null;}local.Dispose();}
    }
    public void Dispose() {lock(gate){disposed=true;pending?.Cancel();generation++;}}
}
