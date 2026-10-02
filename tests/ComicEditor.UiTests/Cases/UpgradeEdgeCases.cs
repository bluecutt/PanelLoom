using System.Windows.Controls;
using ComicEditor.Core.Project;
using ComicEditor.Core.Geometry;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeEdgeCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeEdge.PickHiddenSlantedEdge",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");vm.SetTool(EditMode.Edge);
            var method=typeof(HandleHitTest).GetMethod("FindEdge");Assert.True(method is not null);
            var hit=(DragHit?)method!.Invoke(null,[vm,new Point(80,70),3d]);Assert.Equal(2,hit!.Index);
            vm.SelectEdge(hit.Index);vm.Execute([CommandCases.Op("panel.edge","A","{\"index\":2,\"visible\":false}")]);
            Assert.Equal(2,((DragHit?)method.Invoke(null,[vm,new Point(80,70),3d]))!.Index);
            var p=vm.Document; p.Root["panels"]![0]!["polygon"]=PolygonOperations.Write([new(20,20),new(140,30),new(140,70),new(20,70)]);
            var slant=new EditorViewModel(p);slant.Select("panel","A");Assert.Equal(0,((DragHit?)method.Invoke(null,[slant,new Point(80,25),3d]))!.Index);
        });
        yield return new("UiUpgradeEdge.SelectorIsSelectionNotDraft",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","B");vm.SelectEdge(2);var v=new InspectorView(vm,"panel");
            var index=(ComboBox)v.Fields["panel.snap:index"];Assert.Equal("2",index.SelectedValue?.ToString());index.SelectedValue="0";
            Assert.Equal(0,vm.Selection.Current.EdgeIndex);Assert.True(!vm.InputPending);vm.SelectEdge(1);v.RefreshValues();Assert.Equal("1",index.SelectedValue?.ToString());
        });
        yield return new("UiUpgradeEdge.DirectSnapFeedbackAndLock",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","B");vm.SelectEdge(0);var tools=new CanvasTools(vm);
            var method=typeof(CanvasTools).GetMethod("SnapSelectedEdge");Assert.True(method is not null);method!.Invoke(tools,[]);
            Assert.True(vm.Status.Contains("已吸附到 A"));method.Invoke(tools,[]);Assert.True(vm.Status.Contains("已对齐"));
            vm.Execute([CommandCases.Op("object.lock","B","{\"kind\":\"panel\",\"locked\":true}")]);method.Invoke(tools,[]);Assert.True(vm.Status.Contains("锁定"));
        });
        yield return new("UiUpgradeEdge.AltDragAndVertexSelection",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","B");var input=new InputRouter(vm);var before=vm.Document.Root["panels"]![0]!.ToJsonString();
            input.Begin(new("panel","B",DragKind.Vertex,0),new(20,76));input.Update(new(21,71),true);input.Commit();
            Assert.Equal(before,vm.Document.Root["panels"]![0]!.ToJsonString());Assert.Equal(21d,PolygonOperations.Read(vm.Selected!["polygon"]!)[0].X);
        });
    }
}
