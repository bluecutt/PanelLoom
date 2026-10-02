using System.Windows.Controls;
using System.Windows.Input;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeCanvasToolsCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeCanvasTools.ChineseValuesKeepProtocol",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());var inspector=new InspectorView(vm,"page");var before=vm.Document.Root.ToJsonString();
            var mode=(ComboBox)inspector.Fields["page.border:mode"];
            Assert.Equal("原版边框",mode.SelectedItem?.ToString());
            Assert.Equal("Legacy",mode.SelectedValue?.ToString());
            Assert.Equal(before,vm.Document.Root.ToJsonString());
            vm.Select("panel","A");var panel=new InspectorView(vm,"panel");
            Assert.Equal("铺满分镜",((ComboBox)panel.Fields["panel.image:fitMode"]).SelectedItem?.ToString());
        });
        yield return new("UiUpgradeCanvasTools.ClipDropdownCommitsStableId",()=>
        {
            var doc=CommandCases.Scene();doc.Root["panels"]![1]!["label"]="EnglishCustomName";
            var vm=new EditorViewModel(doc);vm.Select("balloon","X");var view=new InspectorView(vm,"balloon");
            Assert.True(view.Fields["balloon.clip:clipPanelId"] is ComboBox,"Clip must be a real panel picker, not manual ID text");
            var clips=(ComboBox)view.Fields["balloon.clip:clipPanelId"];
            Assert.True(clips.Items.Cast<object>().Any(x=>x.ToString()!.Contains("EnglishCustomName")&&x.ToString()!.Contains("B")));
            clips.SelectedValue="B";
            Assert.Equal("B",vm.Document.Root["balloons"]![0].Text("clipPanelId"));
            Assert.True(!vm.InputPending);vm.Undo();Assert.Equal("A",vm.Document.Root["balloons"]![0].Text("clipPanelId"));
        });
        yield return new("UiUpgradeCanvasTools.DirectPickChangesOnlyClip",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("balloon","X");
            var toolsType=typeof(ComicEditor.Desktop.Canvas.CanvasView).Assembly.GetType("ComicEditor.Desktop.Canvas.CanvasTools");
            Assert.True(toolsType is not null,"Canvas picker/controller is required");
            var tools=Activator.CreateInstance(toolsType!,vm)!;
            var before=vm.Document.Root["balloons"]![0]!["transform"]!.DeepClone();
            toolsType!.GetMethod("BeginClipPick")!.Invoke(tools,["X"]);
            toolsType.GetMethod("PickPanelAt")!.Invoke(tools,[new ComicEditor.Core.Geometry.Point(100,100)]);
            Assert.Equal("B",vm.Document.Root["balloons"]![0].Text("clipPanelId"));
            Assert.True(System.Text.Json.Nodes.JsonNode.DeepEquals(before,vm.Document.Root["balloons"]![0]!["transform"]));
            toolsType.GetMethod("BeginClipPick")!.Invoke(tools,["X"]);
            toolsType.GetMethod("CancelPick")!.Invoke(tools,[]);
            Assert.Equal("B",vm.Document.Root["balloons"]![0].Text("clipPanelId"));
        });
        yield return new("UiUpgradeCanvasTools.ActualToolbarAndCanceledPick",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("balloon","X");
            var window=new ComicEditor.Desktop.MainWindow(vm);window.Show();window.UpdateLayout();
            try
            {
                Assert.True(window.FindName("ClipPickButton") is Button,"Canvas clip picker must be visible in the actual toolbar");
                Assert.Equal("balloon",vm.SelectedKind);Assert.Equal("X",vm.SelectedId);
                var button=(Button)window.FindName("ClipPickButton");button.RaiseEvent(new System.Windows.RoutedEventArgs(Button.ClickEvent));
                var canvas=window.PreviewCanvas;var tools=canvas.GetType().GetProperty("Tools")?.GetValue(canvas);
                Assert.True(tools is not null);
                Assert.Equal("X",tools!.GetType().GetProperty("PickingBalloonId")!.GetValue(tools)?.ToString());
                tools.GetType().GetMethod("CancelPick")!.Invoke(tools,[]);
                Assert.Equal("A",vm.Document.Root["balloons"]![0].Text("clipPanelId"));
            }
            finally{vm.DiscardDrafts();window.Close();}
        });
    }
}
