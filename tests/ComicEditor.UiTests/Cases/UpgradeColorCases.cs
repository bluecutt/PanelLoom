using ComicEditor.Core.Project;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeColorCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeColor.ConfirmCancelOneUndo",()=>
        {
            var doc=CommandCases.Scene();doc.Root["border"]=new System.Text.Json.Nodes.JsonObject{["color"]="#000022"};
            var vm=new EditorViewModel(doc);var view=new InspectorView(vm,"page");var picker=view.Fields["page.border:color"];
            Assert.Equal("ColorPicker",picker.GetType().Name);
            var type=picker.GetType();type.GetMethod("Begin")!.Invoke(picker,["#000022"]);type.GetMethod("SetColorPreview")!.Invoke(picker,["#112233"]);
            Assert.Equal("#000022",vm.Document.Root["border"].Text("color"));Assert.Equal(0L,vm.Revision);Assert.True(vm.InteractionBusy);
            Assert.Equal("#112233",vm.Drafts.Preview(vm.Document).Root["border"].Text("color"));
            type.GetMethod("Cancel")!.Invoke(picker,[]);Assert.Equal("#000022",vm.Document.Root["border"].Text("color"));Assert.True(!vm.InteractionBusy);
            type.GetMethod("Begin")!.Invoke(picker,["#000022"]);type.GetMethod("SetColorPreview")!.Invoke(picker,["#112233"]);type.GetMethod("Confirm")!.Invoke(picker,[]);
            Assert.Equal("#112233",vm.Document.Root["border"].Text("color"));Assert.Equal(1L,vm.Revision);Assert.True(!vm.InteractionBusy);
            vm.Undo();Assert.Equal("#000022",vm.Document.Root["border"].Text("color"));
        });
        yield return new("UiUpgradeColor.InvalidAlphaAndConflictingDraftAreSafe",()=>
        {
            var picker=new ComicEditor.Desktop.Controls.ColorPicker();
            Assert.Throws<FormatException>(()=>picker.Begin("#AA112233"));Assert.Throws<FormatException>(()=>picker.Begin("wrong"));Assert.True(!picker.Active);
            picker.Begin("#000022");Assert.Throws<FormatException>(()=>picker.SetColorPreview("#AA112233"));Assert.Equal("#000022",picker.CurrentHex);picker.Cancel();
            var vm=new EditorViewModel(CommandCases.Scene());var view=new InspectorView(vm,"page");
            ((System.Windows.Controls.TextBox)view.Fields["page.border:width"]).Text="7";
            var color=(ComicEditor.Desktop.Controls.ColorPicker)view.Fields["page.border:color"];
            color.Begin("#000000");Assert.True(!color.Active&&vm.InputPending&&vm.Revision==0);vm.DiscardDrafts();
        });
        yield return new("UiUpgradeColor.BackgroundUsesSameTransaction",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());var view=new InspectorView(vm,"page");var color=(ComicEditor.Desktop.Controls.ColorPicker)view.Fields["page.canvas:background"];
            color.Begin("#FFFFFF");color.SetColorPreview("#EEEEEE");Assert.Equal("#EEEEEE",vm.Drafts.Preview(vm.Document).Root["canvas"].Text("background"));color.Cancel();
            Assert.Equal("#FFFFFF",vm.Document.Root["canvas"].Text("background","#FFFFFF"));Assert.True(!vm.InteractionBusy&&vm.Revision==0);
        });
    }
}
