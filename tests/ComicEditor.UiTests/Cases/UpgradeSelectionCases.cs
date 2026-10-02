using System.Windows.Controls;
using ComicEditor.Core.Project;
using ComicEditor.Desktop;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;

public sealed class UpgradeSelectionCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeSelection.ListTabCanvasAgree", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene());
            var window = new MainWindow(vm);
            window.Show(); window.UpdateLayout();
            try
            {
                ((ListBox)window.FindName("PanelsList")).SelectedIndex = 0;
                Assert.Equal("panel", vm.SelectedKind);
                Assert.Equal("A", vm.SelectedId);
                Assert.Equal(EditMode.Image, window.PreviewCanvas.Mode);
                ((ComboBox)window.FindName("ModeBox")).SelectedIndex = 3;
                ((TabControl)window.FindName("ObjectTabs")).SelectedIndex = 1;
                ((ListBox)window.FindName("BalloonsList")).SelectedIndex = 0;
                Assert.Equal("X", vm.SelectedId);
                Assert.Equal(EditMode.Balloon, window.PreviewCanvas.Mode);
                ((TabControl)window.FindName("ObjectTabs")).SelectedIndex = 0;
                Assert.Equal("A", vm.SelectedId);
                Assert.Equal(EditMode.Vertex, window.PreviewCanvas.Mode);
            }
            finally { window.Close(); }
        });
        yield return new("UiUpgradeSelection.EmptyTypeStaysEmpty", () =>
        {
            var doc = CommandCases.Scene(); doc.Root["balloons"]!.AsArray().Clear();
            var vm = new EditorViewModel(doc);
            var window = new MainWindow(vm);
            window.Show(); window.UpdateLayout();
            try
            {
                ((ListBox)window.FindName("PanelsList")).SelectedIndex = 0;
                ((TabControl)window.FindName("ObjectTabs")).SelectedIndex = 1;
                Assert.Equal("balloon", vm.SelectedKind);
                Assert.True(vm.SelectedId is null);
                Assert.Equal(EditMode.Balloon, window.PreviewCanvas.Mode);
                Assert.True(vm.Selected is null);
            }
            finally { window.Close(); }
        });
        yield return new("UiUpgradeSelection.InvalidObjectIsNotSelected", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene());
            var before = vm.Document.Root.ToJsonString();
            vm.Select("panel", "missing");
            Assert.True(vm.SelectedId is null);
            Assert.Equal("panel", vm.SelectedKind);
            Assert.Equal(before, vm.Document.Root.ToJsonString());
        });
        yield return new("UiUpgradeSelection.EmptyLockedAndInvalidIndex", () =>
        {
            var doc = CommandCases.Scene(); doc.Root["panels"]![0]!["locked"] = true;
            var vm = new EditorViewModel(doc); vm.Select("panel", "A");
            Assert.Equal("A", vm.SelectedId);
            Assert.Equal(5, Assert.Throws<ComicEditor.Core.Validation.EditorException>(() =>
                new InputRouter(vm).Begin(new("panel", "A", DragKind.Image), new(30, 30))).ExitCode);
            Assert.True(!vm.Busy);
            var state = vm.Selection;
            state.SelectEdge(3); state.SelectVertex(3);
            Assert.Equal<int?>(3, state.Current.EdgeIndex);
            var smaller = doc.DeepClone(); smaller.Root["panels"]![0]!["polygon"]!.AsArray().RemoveAt(3);
            state.Reconcile(smaller);
            Assert.True(state.Current.EdgeIndex is null && state.Current.VertexIndex is null);
            vm.SetTool(EditMode.View); vm.Select("balloon", "X", false);
            Assert.Equal(EditMode.View, vm.Selection.Current.Tool);
        });
    }
}
