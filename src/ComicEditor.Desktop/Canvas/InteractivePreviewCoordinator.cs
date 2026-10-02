namespace ComicEditor.Desktop.Canvas;
public readonly record struct PreviewEpoch(long Document,long Assets,long Interaction);
public sealed class InteractivePreviewCoordinator<TState,TFrame>(Func<TState,CancellationToken,Task<TFrame>> render,Action<TFrame> publish,Action<TFrame> release) : IDisposable
{
    private readonly object gate=new();private PreviewEpoch epoch;private TState? latest;private bool pending,active,disposed;private CancellationTokenSource? cancellation;
    public event Action<Exception>? Failed;
    public void Submit(PreviewEpoch next,TState state,bool final=false)
    {lock(gate){if(disposed)return;if(next!=epoch){epoch=next;cancellation?.Cancel();}latest=state;pending=true;}if(final)Tick();}
    public void Invalidate(PreviewEpoch next){lock(gate){if(disposed)return;epoch=next;pending=false;latest=default;cancellation?.Cancel();}}
    public void Tick()
    {
        TState state;PreviewEpoch captured;CancellationTokenSource local;
        lock(gate){if(disposed||active||!pending)return;active=true;pending=false;state=latest!;captured=epoch;cancellation=local=new();}
        _=Task.Run(async()=>
        {
            TFrame? frame=default;var produced=false;
            try{frame=await render(state,local.Token).ConfigureAwait(false);produced=true;bool valid;lock(gate)valid=!disposed&&epoch==captured&&!local.IsCancellationRequested;if(valid)publish(frame);else release(frame);produced=false;}
            catch(OperationCanceledException)when(local.IsCancellationRequested){}
            catch(Exception ex){bool relevant;lock(gate)relevant=!disposed&&epoch==captured&&!local.IsCancellationRequested;if(relevant)Failed?.Invoke(ex);}
            finally{if(produced)release(frame!);lock(gate){active=false;if(ReferenceEquals(cancellation,local))cancellation=null;}local.Dispose();}
        });
    }
    public void Dispose(){lock(gate){disposed=true;pending=false;cancellation?.Cancel();}}
}
