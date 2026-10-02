using System.Text.Json.Nodes;
using System.Windows.Threading;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Agent;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Desktop.Editing;
using ComicEditor.Session;
using ComicEditor.Rendering;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeAgentCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeAgent.WorkflowParityBusyAndNoop",()=>{
            var vm=new EditorViewModel(CommandCases.Scene()){AllowAgentWrite=true};var id=Guid.NewGuid().ToString();var engine=new SessionEngine(id,new EditorSessionAdapter(vm,Dispatcher.CurrentDispatcher));
            JsonObject Request(JsonArray ops)=>new(){["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["sessionId"]=id,["kind"]="apply",["baseRevision"]=vm.Revision,["baseProjectHash"]=SessionHash.Compute(vm.Document),["operations"]=ops};
            var before=vm.Document;var operations=ComicEditor.Tests.UpgradeAgentCases.Operations();var request=Request(operations);var result=engine.HandleAsync(request).GetAwaiter().GetResult();Assert.Equal(0,result.ExitCode);Assert.True(result.Envelope["data"]!["outcomes"] is JsonArray,"Live outcome receipts required");Assert.Equal(vm.Revision,(long)result.Envelope["data"].Number("revision"));
            var expected=new CommandProcessor().Plan(before,operations.Select(o=>new Operation(o!.Text("op"),o.Text("targetId"),o!["args"]!.DeepClone().AsObject())).ToArray()).After;using var direct=new PageRenderer().RenderBitmap(expected,new(1.75));using var live=new PageRenderer().RenderBitmap(vm.Document,new(1.75));Assert.Equal(0,Pixels.Differences(direct.Pixels,live.Pixels));
            var repeat=engine.HandleAsync(request).GetAwaiter().GetResult();Assert.True(JsonNode.DeepEquals(result.Envelope,repeat.Envelope));vm.Undo();Assert.True(JsonNode.DeepEquals(before.Root,vm.Document.Root));
            var noop=Request(JsonNode.Parse("[{\"op\":\"object.reorder\",\"targetId\":\"B\",\"args\":{\"kind\":\"panel\",\"direction\":\"up\"}}]")!.AsArray());var revision=vm.Revision;var unchanged=engine.HandleAsync(noop).GetAwaiter().GetResult();Assert.Equal(0,unchanged.ExitCode);Assert.Equal(revision,vm.Revision);Assert.Equal("NoChange",unchanged.Envelope["data"]!["outcomes"]![0].Text("code"));
            vm.Select("balloon","X");vm.Drafts.Configure(new("balloon","X","balloon.transform"),new(){["width"]=40});vm.Drafts.StageRaw(new("balloon","X","balloon.transform"),"width","45");Assert.Equal(6,engine.HandleAsync(Request(operations.DeepClone().AsArray())).GetAwaiter().GetResult().ExitCode);vm.DiscardDrafts();
            var path=Path.Combine(TestFiles.Directory(),"闭环 工程.json");var save=Request(new JsonArray());save.Remove("operations");save["kind"]="save";save["savePath"]=path;Assert.Equal(0,engine.HandleAsync(save).GetAwaiter().GetResult().ExitCode);var loaded=new ProjectStore().Load(path);using var final=new PageRenderer().RenderBitmap(loaded.Document,new(2.5));Assert.Equal(400,final.Pixels.Width);
        });
    }
}
