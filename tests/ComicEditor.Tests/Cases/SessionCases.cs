using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.History;
using ComicEditor.Core.Project;
using ComicEditor.Session;
namespace ComicEditor.Tests;
public sealed class FakeEditorSession : IEditorSession
{
    public ProjectHistory History {get;}=new(CommandCases.Scene());
    public long Revision {get;private set;}
    public bool Busy {get;set;}
    public bool AllowWrite {get;set;}
    public Action? BeforeInvoke {get;set;}
    public string? LastSavePath {get;private set;}
    public ProjectDocument Snapshot()=>History.Snapshot();
    public PlannedChange Execute(IReadOnlyList<Operation> ops){var before=Snapshot();var plan=new CommandProcessor().Plan(before,ops);History.Commit(before,plan);Revision++;return plan;}
    public LoadedProject Save(string path,string? expected){LastSavePath=path;return new ProjectStore().Save(Snapshot(),path,new(File.Exists(path),expected));}
    public Task<T> InvokeAsync<T>(Func<T> action,CancellationToken token){token.ThrowIfCancellationRequested();BeforeInvoke?.Invoke();return Task.FromResult(action());}
}
public sealed class SessionCases : ITestSuite
{
    public static JsonObject Request(string id,FakeEditorSession editor,string kind="apply")=>new(){["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["sessionId"]=id,["kind"]=kind,["baseRevision"]=editor.Revision,["baseProjectHash"]=SessionHash.Compute(editor.Snapshot()),["operations"]=JsonNode.Parse("[{\"op\":\"balloon.move\",\"targetId\":\"X\",\"args\":{\"dx\":2,\"dy\":3}}]")};
    public IEnumerable<TestCase> Cases()=>[
        new("Session.UnauthorizedWriteDenied",()=>{var editor=new FakeEditorSession();var id=Guid.NewGuid().ToString();var before=editor.Snapshot().Root.ToJsonString();var r=new SessionEngine(id,editor).HandleAsync(Request(id,editor)).GetAwaiter().GetResult();Assert.Equal(5,r.ExitCode);Assert.Equal(before,editor.Snapshot().Root.ToJsonString());}),
        new("Session.StaleRevisionPreservesUnsavedWork",()=>{var editor=new FakeEditorSession{AllowWrite=true};var id=Guid.NewGuid().ToString();var request=Request(id,editor);editor.Execute([CommandCases.Op("balloon.move","X","{\"dx\":7,\"dy\":0}")]);var before=editor.Snapshot().Root.ToJsonString();Assert.Equal(4,new SessionEngine(id,editor).HandleAsync(request).GetAwaiter().GetResult().ExitCode);Assert.Equal(before,editor.Snapshot().Root.ToJsonString());}),
        new("Session.SameRequestAppliedOnce",()=>{var editor=new FakeEditorSession{AllowWrite=true};var id=Guid.NewGuid().ToString();var request=Request(id,editor);var engine=new SessionEngine(id,editor);var a=engine.HandleAsync(request).GetAwaiter().GetResult();var b=engine.HandleAsync(request).GetAwaiter().GetResult();Assert.Equal(0,a.ExitCode);Assert.True(JsonNode.DeepEquals(a.Envelope,b.Envelope));Assert.Equal(1L,editor.Revision);Assert.Equal(32d,editor.Snapshot().Root["balloons"]![0]!["transform"].Number("x"));}),
        new("Session.SameIdDifferentPayloadRejected",()=>{var editor=new FakeEditorSession{AllowWrite=true};var id=Guid.NewGuid().ToString();var req=Request(id,editor);var engine=new SessionEngine(id,editor);engine.HandleAsync(req).GetAwaiter().GetResult();req["operations"]![0]!["args"]!["dx"]=20;Assert.Equal(4,engine.HandleAsync(req).GetAwaiter().GetResult().ExitCode);Assert.Equal(1L,editor.Revision);}),
        new("Session.BatchUndo",()=>{var editor=new FakeEditorSession{AllowWrite=true};var before=editor.Snapshot().Root.ToJsonString();var id=Guid.NewGuid().ToString();var req=Request(id,editor);req["operations"]!.AsArray().Add(JsonNode.Parse("{\"op\":\"panel.image\",\"targetId\":\"A\",\"args\":{\"scale\":1.5}}"));Assert.Equal(0,new SessionEngine(id,editor).HandleAsync(req).GetAwaiter().GetResult().ExitCode);editor.History.Undo();Assert.Equal(before,editor.Snapshot().Root.ToJsonString());}),
        new("Session.BusyAndQueuedAuthorizationRechecked",()=>{var editor=new FakeEditorSession{AllowWrite=true,Busy=true};var id=Guid.NewGuid().ToString();var engine=new SessionEngine(id,editor);Assert.Equal(6,engine.HandleAsync(Request(id,editor)).GetAwaiter().GetResult().ExitCode);editor.Busy=false;editor.BeforeInvoke=()=>editor.AllowWrite=false;Assert.Equal(5,engine.HandleAsync(Request(id,editor)).GetAwaiter().GetResult().ExitCode);}),
        new("Session.CapacityRetainsOldReceipts",()=>{var editor=new FakeEditorSession{AllowWrite=true};var id=Guid.NewGuid().ToString();var engine=new SessionEngine(id,editor,1);var req=Request(id,editor);var first=engine.HandleAsync(req).GetAwaiter().GetResult();Assert.Equal(0,first.ExitCode);Assert.Equal(6,engine.HandleAsync(Request(id,editor)).GetAwaiter().GetResult().ExitCode);Assert.True(JsonNode.DeepEquals(first.Envelope,engine.HandleAsync(req).GetAwaiter().GetResult().Envelope));}),
        new("Session.AtomicInvalidBatch",()=>{var editor=new FakeEditorSession{AllowWrite=true};var before=editor.Snapshot().Root.ToJsonString();var id=Guid.NewGuid().ToString();var req=Request(id,editor);req["operations"]!.AsArray().Add(JsonNode.Parse("{\"op\":\"panel.vertex\",\"targetId\":\"A\",\"args\":{\"index\":99,\"x\":2,\"y\":3}}"));Assert.Equal(2,new SessionEngine(id,editor).HandleAsync(req).GetAwaiter().GetResult().ExitCode);Assert.Equal(before,editor.Snapshot().Root.ToJsonString());}),
        new("Session.CurrentUserPipeAndRegistry",()=>{var editor=new FakeEditorSession();var dir=TestFiles.Directory();using(var host=new SessionHost(editor,dir)){using var c=new CancellationTokenSource(TimeSpan.FromSeconds(5));var r=PipeTransport.SendAsync(host.PipeName,Request(host.Id,editor,"snapshot"),c.Token).GetAwaiter().GetResult();Assert.Equal(0,r.ExitCode);Assert.True(r.Envelope["data"]!["project"] is JsonObject);var registry=new SessionRegistry(dir).List();Assert.Equal(host.Id,registry.Single().SessionId);Assert.Equal(SessionRegistry.CurrentSid,registry.Single().OwnerSid);}Assert.Equal(0,new SessionRegistry(dir).List().Count);}),
        new("Session.PacketAndOperationLimits",()=>{var editor=new FakeEditorSession{AllowWrite=true};var dir=TestFiles.Directory();using var host=new SessionHost(editor,dir);using var c=new CancellationTokenSource(TimeSpan.FromSeconds(5));var req=Request(host.Id,editor);var ops=req["operations"]!.AsArray();for(var n=1;n<1001;n++)ops.Add(ops[0]!.DeepClone());Assert.Equal(2,PipeTransport.SendAsync(host.PipeName,req,c.Token).GetAwaiter().GetResult().ExitCode);using var pipe=new System.IO.Pipes.NamedPipeClientStream(".",host.PipeName,System.IO.Pipes.PipeDirection.InOut,System.IO.Pipes.PipeOptions.CurrentUserOnly);pipe.Connect(5000);var header=new byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header,9*1024*1024);pipe.Write(header);pipe.Flush();var reply=PipeTransport.ReadAsync(pipe,1024*1024,c.Token).GetAwaiter().GetResult();Assert.Equal(2,reply["exitCode"]!.GetValue<int>());Assert.Equal(0L,editor.Revision);})
    ];
}
