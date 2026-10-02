using System.IO.Pipes;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using ComicEditor.Core.Project;
namespace ComicEditor.Session;
public sealed class SessionHost : IDisposable
{
    public string Id {get;}=Guid.NewGuid().ToString();
    public string PipeName {get;}
    private readonly SessionEngine engine;
    private readonly CancellationTokenSource stop=new();
    private readonly ConcurrentDictionary<NamedPipeServerStream,byte> connections=new();
    private readonly string registrationPath;
    private readonly string registrationHash;
    public event Action<Exception>? Error;
    public SessionHost(IEditorSession editor,string registryDirectory)
    {
        PipeName="ComicEditor.Native."+Guid.Parse(Id).ToString("N");engine=new(Id,editor);
        registrationPath=new SessionRegistry(registryDirectory).Add(new(1,Id,PipeName,Environment.ProcessId,SessionRegistry.CurrentSid,DateTimeOffset.UtcNow));registrationHash=AtomicFile.Hash(registrationPath);_=AcceptAsync();
    }
    private async Task AcceptAsync()
    {
        try {
            while(!stop.IsCancellationRequested)
            {
                var pipe=new NamedPipeServerStream(PipeName,PipeDirection.InOut,NamedPipeServerStream.MaxAllowedServerInstances,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
                connections.TryAdd(pipe,0);try{await pipe.WaitForConnectionAsync(stop.Token).ConfigureAwait(false);}catch{connections.TryRemove(pipe,out _);pipe.Dispose();throw;}
                _=ServeAsync(pipe);
            }
        }catch(OperationCanceledException)when(stop.IsCancellationRequested){}
        catch(ObjectDisposedException)when(stop.IsCancellationRequested){}
        catch(Exception ex){Error?.Invoke(ex);}
    }
    private async Task ServeAsync(NamedPipeServerStream pipe)
    {
        try {
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);deadline.CancelAfter(TimeSpan.FromSeconds(30));
            var request=await PipeTransport.ReadAsync(pipe,8*1024*1024,deadline.Token).ConfigureAwait(false);
            var seconds=request.Number("timeoutSeconds",30);if(seconds<=0||seconds>86400)throw new ComicEditor.Core.Validation.EditorException("ARGS","Invalid request timeout");deadline.CancelAfter(TimeSpan.FromSeconds(seconds));
            var result=await engine.HandleAsync(request,deadline.Token).ConfigureAwait(false);
            await PipeTransport.WriteAsync(pipe,new JsonObject{["exitCode"]=result.ExitCode,["envelope"]=result.Envelope},64*1024*1024,deadline.Token).ConfigureAwait(false);
        }catch(Exception ex)when(ex is IOException or OperationCanceledException or ObjectDisposedException){}
        catch(Exception ex)
        {try{var result=SessionJson.Result(null,false,"PROTOCOL",ex.Message,exitCode:2);await PipeTransport.WriteAsync(pipe,new(){["exitCode"]=result.ExitCode,["envelope"]=result.Envelope},64*1024*1024,stop.Token).ConfigureAwait(false);}catch(Exception transport)when(transport is IOException or OperationCanceledException or ObjectDisposedException){} }
        finally{connections.TryRemove(pipe,out _);pipe.Dispose();}
    }
    public void Dispose()
    {
        if(stop.IsCancellationRequested)return;stop.Cancel();foreach(var pipe in connections.Keys)pipe.Dispose();
        try{if(File.Exists(registrationPath)&&AtomicFile.Hash(registrationPath)==registrationHash)File.Delete(registrationPath);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Error?.Invoke(ex);}
        // Cancellation source lives until in-flight tasks finish; disposal is process-lifetime bounded.
    }
}
