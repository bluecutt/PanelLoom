using System.Text.Json.Nodes;
using System.Windows.Controls;
using System.Windows.Threading;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Desktop.Agent;
using ComicEditor.Session;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class FinalReviewCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiFinalReview.GroupRenameKeepsOtherDraft",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var before=vm.Document.Root.ToJsonString();var view=new InspectorView(vm,"panel");
            ((TextBox)view.Fields["panel.image:offsetX"]).Text="9";((TextBox)view.Fields["object.rename:newId"]).Text="Z";
            vm.Drafts.CommitGroup(new("panel","A","object.rename"),vm);Assert.Equal("Z",vm.SelectedId);Assert.Equal("9",vm.Drafts.Raw(new("panel","Z","panel.image"),"offsetX"));Assert.True(vm.InputPending);
            vm.Drafts.CommitAll(vm);Assert.Equal(9d,vm.Selected!["imageTransform"].Number("offsetX"));Assert.True(!vm.InputPending);vm.Undo();vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());
        });
        foreach(var renameFirst in new[]{true,false})yield return new("UiFinalReview.ApplyAllRenameOrder."+renameFirst,()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var before=vm.Document.Root.ToJsonString();var view=new InspectorView(vm,"panel");
            void Name()=>((TextBox)view.Fields["object.rename:newId"]).Text="Z";void Image()=>((TextBox)view.Fields["panel.image:offsetX"]).Text="9";
            if(renameFirst){Name();Image();}else{Image();Name();}vm.Drafts.CommitAll(vm);Assert.Equal("Z",vm.SelectedId);Assert.Equal(9d,vm.Selected!["imageTransform"].Number("offsetX"));Assert.True(!vm.InputPending);Assert.Equal(1L,vm.Revision);vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());
        });
        foreach(var spelling in new[]{"2.0","2e0","2.0000001"})yield return new("UiFinalReview.ExportNumericSpelling."+spelling,()=>{
            var doc=CommandCases.Scene();doc.Root["canvas"]!["exportScale"]=JsonNode.Parse(spelling);var vm=new EditorViewModel(doc);var before=vm.Document.Root.ToJsonString();var inspector=new InspectorView(vm,"page");var selector=(ComboBox)inspector.Fields["page.canvas:exportScale"];Assert.True(selector.SelectedItem is not null,"Page export scale must have an actual selected item");
            var dialog=new ComicEditor.Desktop.Dialogs.ExportDialog(vm);try{var combo=((StackPanel)dialog.Content).Children.OfType<ComboBox>().Single();Assert.Equal(new ProjectView(doc).Scale,((ExportPreset)combo.SelectedItem).Scale);Assert.Equal(before,vm.Document.Root.ToJsonString());Assert.True(!vm.InputPending);}finally{dialog.Close();}
        });
        yield return new("UiFinalReview.ActualAgentPanelChoicesRefresh",()=>{
            var vm=new EditorViewModel(CommandCases.Scene()){AllowAgentWrite=true};vm.Select("balloon","X");var window=new ComicEditor.Desktop.MainWindow(vm);window.Show();window.UpdateLayout();
            try{var view=(InspectorView)((Border)window.FindName("InspectorHost")).Child;var clip=(ComboBox)view.Fields["balloon.clip:clipPanelId"];var target=(ComboBox)view.Fields["balloon.panelOcclusion:panelId"];var id=Guid.NewGuid().ToString();var req=new JsonObject{["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["sessionId"]=id,["kind"]="apply",["baseRevision"]=vm.Revision,["baseProjectHash"]=SessionHash.Compute(vm.Document),["operations"]=JsonNode.Parse("""[{"op":"object.rename","targetId":"A","args":{"kind":"panel","newId":"Z","label":"新分镜"}},{"op":"panel.add","args":{"object":{"id":"C","sourceImage":"art.png","polygon":[[20,90],[40,90],[40,110],[20,110]]}}},{"op":"panel.remove","targetId":"B","args":{"clipPolicy":"free"}}]""")};var result=new SessionEngine(id,new EditorSessionAdapter(vm,Dispatcher.CurrentDispatcher)).HandleAsync(req).GetAwaiter().GetResult();Assert.Equal(0,result.ExitCode);Assert.True(ReferenceEquals(view,((Border)window.FindName("InspectorHost")).Child));
                var values=clip.Items.Cast<ComicEditor.Desktop.Controls.UiOption>().Select(o=>o.Value).ToArray();Assert.True(values.Contains("Z")&&values.Contains("C")&&!values.Contains("A")&&!values.Contains("B"),"Existing inspector must reflect live panel IDs");Assert.Equal("Z",clip.SelectedValue?.ToString());Assert.True(clip.SelectedItem!.ToString()!.Contains("新分镜"));Assert.Equal("Z",target.SelectedValue?.ToString());Assert.True(!vm.InputPending);
                vm.Save(Path.Combine(TestFiles.Directory(),"review.json"),new());
            }finally{vm.DiscardDrafts();if(vm.Dirty)vm.Save(Path.Combine(TestFiles.Directory(),"close.json"),new());window.Close();}
        });
    }
}
