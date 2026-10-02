using System.Text.Json.Nodes;
using System.Windows.Controls;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
using System.Windows.Threading;
using ComicEditor.Desktop.Agent;
using ComicEditor.Session;
namespace ComicEditor.UiTests;
public sealed class OcclusionAutoOrderUiCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiOcclusionAutoOrder.CanvasPlacesBehindInOneUndoWithFeedback",()=>
        {
            var doc=OcclusionAutoOrderCases.Scene();var vm=new EditorViewModel(doc);vm.Select("balloon","W");
            var before=vm.Document.Root.ToJsonString();new CanvasTools(vm).SetPanelOcclusion("B","back");
            Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(vm.Document,"balloon")));
            Assert.True(vm.Status.Contains("B")&&vm.Status.Contains("下移")&&vm.Status.Contains("3"),vm.Status);
            Assert.Equal("W",vm.SelectedId);Assert.Equal(1L,vm.Revision);
            vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());
            vm.Redo();Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(vm.Document,"balloon")));
        });
        yield return new("UiOcclusionAutoOrder.InspectorUsesSamePlacementWithoutChangingClip",()=>
        {
            var vm=new EditorViewModel(OcclusionAutoOrderCases.Scene());vm.Select("balloon","W");string? error=null;
            var view=new InspectorView(vm,"balloon",message=>error=message);
            ((ComboBox)view.Fields["balloon.panelOcclusion:panelId"]).SelectedValue="B";
            ((ComboBox)view.Fields["balloon.panelOcclusion:position"]).SelectedValue="back";
            Assert.Equal<string?>(null,error);
            Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(vm.Document,"balloon")));
            Assert.Equal("A",vm.Selected.Text("clipPanelId"));Assert.True(!vm.InputPending);
        });
        yield return new("UiOcclusionAutoOrder.LiveAgentReturnsAdjustmentAndRetryIsIdempotent",()=>
        {
            var vm=new EditorViewModel(OcclusionAutoOrderCases.Scene()){AllowAgentWrite=true};var before=vm.Document.Root.ToJsonString();
            var id=Guid.NewGuid().ToString();var engine=new SessionEngine(id,new EditorSessionAdapter(vm,Dispatcher.CurrentDispatcher));
            var request=new JsonObject{["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["sessionId"]=id,["kind"]="apply",["baseRevision"]=vm.Revision,["baseProjectHash"]=SessionHash.Compute(vm.Document),["operations"]=JsonNode.Parse("[{\"op\":\"balloon.panelOcclusion\",\"targetId\":\"W\",\"args\":{\"panelId\":\"B\",\"position\":\"back\",\"autoOrder\":true}}]")};
            var result=engine.HandleAsync(request).GetAwaiter().GetResult();Assert.Equal(0,result.ExitCode);
            Assert.Equal("OrderAdjusted",result.Envelope["data"]!["outcomes"]![0].Text("code"));
            Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(vm.Document,"balloon")));
            var revision=vm.Revision;var repeat=engine.HandleAsync(request).GetAwaiter().GetResult();
            Assert.True(JsonNode.DeepEquals(result.Envelope,repeat.Envelope));Assert.Equal(revision,vm.Revision);
            vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());
        });
    }
}
