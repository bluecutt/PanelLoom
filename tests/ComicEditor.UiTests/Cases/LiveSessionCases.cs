using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using ComicEditor.Tests;
using ComicEditor.Core.Project;
using ComicEditor.Core.AppState;
using ComicEditor.Desktop;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Desktop.Agent;
using ComicEditor.Session;
namespace ComicEditor.UiTests;
public sealed class LiveSessionCases : ITestSuite
{
    public static T Pump<T>(Func<T> work)
    {
        var task=Task.Run(work);var expires=DateTime.UtcNow.AddSeconds(60);
        while(!task.IsCompleted&&DateTime.UtcNow<expires){var frame=new DispatcherFrame();var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(10)};timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
        Assert.True(task.IsCompleted,"Live request did not finish within 60s");return task.GetAwaiter().GetResult();
    }
    public IEnumerable<TestCase> Cases()=>[
        new("UiLive.CliWindowUnsavedUndoSave",()=>{
            var dir=TestFiles.Directory();var saved=new ProjectStore().Save(CommandCases.Scene(),Path.Combine(dir,"source.json"),new());var vm=new EditorViewModel(saved.Document);vm.Open(saved.AbsolutePath);
            var window=new MainWindow(vm);var paths=StatePaths.Resolve(dir,true);window.AttachState(paths,null);window.Show();
            try {
                var host=window.AgentSession!;Assert.True(host is not null);var registry=paths.Sessions;
                Assert.Equal(0,Pump(()=>CliCases.Run("session","list","--registry-dir",registry)).Exit);
                (int Exit,JsonObject Envelope) Snapshot()=>Pump(()=>CliCases.Run("session","snapshot","--session",host!.Id,"--registry-dir",registry));
                JsonObject PatchFrom(JsonObject snapshot)=>new(){["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["baseRevision"]=snapshot["data"]!["revision"]!.DeepClone(),["baseProjectHash"]=snapshot["data"]!["hash"]!.DeepClone(),["operations"]=JsonNode.Parse("[{\"op\":\"balloon.move\",\"targetId\":\"X\",\"args\":{\"dx\":4,\"dy\":5}},{\"op\":\"balloon.visible\",\"targetId\":\"X\",\"args\":{\"visible\":false}}]")};
                var patch=Path.Combine(dir,"patch.json");File.WriteAllText(patch,PatchFrom(Snapshot().Envelope).ToJsonString());Assert.Equal(5,Pump(()=>CliCases.Run("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patch)).Exit);
                ((System.Windows.Controls.CheckBox)window.FindName("AgentWrite")).IsChecked=true;Assert.True(vm.AllowAgentWrite);vm.Execute([CommandCases.Op("panel.image","A","{\"scale\":1.7}")]);var before=vm.Document.Root.ToJsonString();var snap=Snapshot();File.WriteAllText(patch,PatchFrom(snap.Envelope).ToJsonString());
                var first=Pump(()=>CliCases.Run("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patch));Assert.Equal(0,first.Exit);Assert.Equal(34d,vm.Document.Root["balloons"]![0]!["transform"].Number("x"));var revision=vm.Revision;
                var repeat=Pump(()=>CliCases.Run("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patch));Assert.True(JsonNode.DeepEquals(first.Envelope,repeat.Envelope));Assert.Equal(revision,vm.Revision);vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());
                var stale=JsonNode.Parse(File.ReadAllText(patch))!.AsObject();stale["requestId"]=Guid.NewGuid().ToString();File.WriteAllText(patch,stale.ToJsonString());Assert.Equal(4,Pump(()=>CliCases.Run("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patch)).Exit);
                var current=Snapshot().Envelope["data"]!;var output=Path.Combine(dir,"live-saved.json");Assert.Equal(0,Pump(()=>CliCases.Run("session","save","--session",host!.Id,"--registry-dir",registry,"--out",output,"--revision",current["revision"]!.ToString(),"--base-hash",current.Text("hash"))).Exit);Assert.True(!vm.Dirty);Assert.Equal(saved.Sha256,AtomicFile.Hash(saved.AbsolutePath));
                File.AppendAllText(output," ");current=Snapshot().Envelope["data"]!;Assert.Equal(4,Pump(()=>CliCases.Run("session","save","--session",host!.Id,"--registry-dir",registry,"--out",Path.Combine(dir,"external-conflict.json"),"--revision",current["revision"]!.ToString(),"--base-hash",current.Text("hash"))).Exit);
            }finally {window.Close();}
        }),
        new("UiLive.NotificationFaultDoesNotLieAboutCommit",()=>{var vm=new EditorViewModel(CommandCases.Scene()){AllowAgentWrite=true};vm.Changed+=()=>throw new Exception("test refresh fault");var id=Guid.NewGuid().ToString();var req=new JsonObject{["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["sessionId"]=id,["kind"]="apply",["baseRevision"]=vm.Revision,["baseProjectHash"]=SessionHash.Compute(vm.Document),["operations"]=JsonNode.Parse("[{\"op\":\"balloon.move\",\"targetId\":\"X\",\"args\":{\"dx\":2,\"dy\":0}}]")};var result=new SessionEngine(id,new EditorSessionAdapter(vm,Dispatcher.CurrentDispatcher)).HandleAsync(req).GetAwaiter().GetResult();Assert.Equal(0,result.ExitCode);Assert.Equal(32d,vm.Document.Root["balloons"]![0]!["transform"].Number("x"));})
    ];
}
