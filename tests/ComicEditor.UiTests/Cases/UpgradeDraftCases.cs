using System.Windows;
using System.Windows.Controls;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.Agent;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;

public sealed class UpgradeDraftCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeDraft.TwoGroupsApplyAndLeave", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene()); vm.Select("panel", "A");
            var inspector = new InspectorView(vm, "panel");
            ((TextBox)inspector.Fields["panel.image:offsetX"]).Text = "25";
            ((TextBox)inspector.Fields["object.layer:zIndex"]).Text = "7";
            Apply(inspector, "原图取景");
            Assert.Equal(25d, vm.Document.Root["panels"]![0]!["imageTransform"].Number("offsetX"));
            Assert.True(vm.InputPending, "Applying one group must keep the other draft pending");
            Assert.Equal(1L, vm.Revision);
            vm.Select("panel", "A");
            Assert.True(vm.InputPending, "Re-selecting the same object must not discard input");
            var restored = new InspectorView(vm, "panel");
            Assert.Equal("7", ((TextBox)restored.Fields["object.layer:zIndex"]).Text);
            vm.Select("balloon", "X");
            Assert.Equal("panel", vm.SelectedKind); // No choice supplied: cancel, not silent discard.
            Assert.True(vm.InputPending);
        });
        yield return new("UiUpgradeDraft.InvalidValuePreservesDocument", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene()); vm.Select("panel", "A");
            var before = vm.Document.Root.ToJsonString(); var error = "";
            var inspector = new InspectorView(vm, "panel", message => error = message);
            ((TextBox)inspector.Fields["panel.image:offsetX"]).Text = "not-a-number";
            Apply(inspector, "原图取景");
            Assert.Equal(before, vm.Document.Root.ToJsonString());
            Assert.Equal(0L, vm.Revision);
            Assert.True(vm.InputPending && error.Length > 0);
            vm.Select("balloon", "X");
            Assert.Equal("A", vm.SelectedId);
        });
        yield return new("UiUpgradeDraft.NoChangeDoesNotAddRevision", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene());
            var plan = vm.Execute([CommandCases.Op("balloon.transform", "X", "{\"x\":30}")]);
            Assert.Equal(0, plan.Changes.Count);
            Assert.Equal(0L, vm.Revision);
            vm.Undo(); Assert.Equal(0L, vm.Revision);
        });
        yield return new("UiUpgradeDraft.ApplyAllIsAtomicAndOneUndo", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene()); vm.Select("panel", "A");
            var before = vm.Document.Root.ToJsonString();
            var inspector = new InspectorView(vm, "panel");
            ((TextBox)inspector.Fields["panel.image:offsetX"]).Text = "25";
            ((TextBox)inspector.Fields["object.layer:zIndex"]).Text = "7";
            vm.ResolveDrafts = () => DraftDecision.Apply; vm.Select("balloon", "X");
            Assert.Equal("X", vm.SelectedId);
            Assert.Equal(25d, vm.Document.Root["panels"]![0]!["imageTransform"].Number("offsetX"));
            Assert.Equal(7d, vm.Document.Root["panels"]![0].Number("zIndex"));
            Assert.True(!vm.InputPending);
            Assert.Equal(1L, vm.Revision);
            vm.Undo(); Assert.Equal(before, vm.Document.Root.ToJsonString());
        });
        yield return new("UiUpgradeDraft.InvalidApplyAllAndLockDoNotPartiallyCommit", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene()); vm.Select("panel", "A");
            var before = vm.Document.Root.ToJsonString();
            var inspector = new InspectorView(vm, "panel");
            ((TextBox)inspector.Fields["panel.image:offsetX"]).Text = "25";
            ((TextBox)inspector.Fields["object.layer:zIndex"]).Text = "invalid";
            Assert.True(!vm.TryLeaveContext(DraftDecision.Apply));
            Assert.Equal(before, vm.Document.Root.ToJsonString());
            Assert.True(vm.InputPending);
            var locked = CommandCases.Scene(); locked.Root["panels"]![0]!["locked"] = true;
            var lockedVm = new EditorViewModel(locked); lockedVm.Select("panel", "A");
            var view = new InspectorView(lockedVm, "panel");
            ((TextBox)view.Fields["panel.image:offsetX"]).Text = "25";
            Assert.True(!lockedVm.TryLeaveContext(DraftDecision.Apply));
            Assert.Equal(1d, lockedVm.Document.Root["panels"]![0]!["imageTransform"].Number("offsetX"));
        });
        yield return new("UiUpgradeDraft.BusyAndDiscardPreserveSource", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene()); vm.Select("panel", "A");
            var before = vm.Document.Root.ToJsonString();
            var view = new InspectorView(vm, "panel");
            ((TextBox)view.Fields["panel.image:offsetX"]).Text = "25";
            var adapter = new EditorSessionAdapter(vm, System.Windows.Threading.Dispatcher.CurrentDispatcher);
            Assert.True(adapter.Busy);
            Assert.Equal(6, Assert.Throws<EditorException>(() => vm.Execute([CommandCases.Op("balloon.move", "X", "{\"dx\":1,\"dy\":0}")])).ExitCode);
            vm.ResolveDrafts = () => DraftDecision.Discard; vm.Select("balloon", "X");
            Assert.Equal("X", vm.SelectedId); Assert.True(!adapter.Busy);
            Assert.Equal(before, vm.Document.Root.ToJsonString());
        });
        yield return new("UiUpgradeDraft.CloseCancelKeepsWindowAndTextUndo", () =>
        {
            var vm = new EditorViewModel(CommandCases.Scene()); vm.Select("panel", "A");
            var window = new ComicEditor.Desktop.MainWindow(vm); window.Show(); window.UpdateLayout();
            vm.ResolveDrafts = () => DraftDecision.Cancel;
            try
            {
                var inspector = (InspectorView)((Border)window.FindName("InspectorHost")).Child;
                var box = (TextBox)inspector.Fields["panel.image:offsetX"];
                box.Focus(); box.Text = "25";
                Assert.True(!ComicEditor.Desktop.Canvas.FocusCommandPolicy.CanvasShortcutAllowed(box));
                window.Close(); Assert.True(window.IsVisible && vm.InputPending);
                box.Undo(); Assert.True(!vm.InputPending);
                Assert.Equal(1d, vm.Document.Root["panels"]![0]!["imageTransform"].Number("offsetX"));
            }
            finally { vm.DiscardDrafts(); window.Close(); }
        });
    }
    public static void Apply(InspectorView inspector, string groupLabel)
    {
        var group = ((StackPanel)inspector.Content).Children.OfType<Expander>().Single(x => x.Header.ToString() == groupLabel);
        var button = ((StackPanel)group.Content).Children.OfType<Button>().First(x => x.Content.ToString() == "应用");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
}
