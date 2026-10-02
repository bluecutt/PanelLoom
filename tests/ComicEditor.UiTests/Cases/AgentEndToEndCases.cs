using System.Text.Json.Nodes;
using ComicEditor.Tests;
using ComicEditor.Core.Project;
using ComicEditor.Core.AppState;
using ComicEditor.Desktop;
using ComicEditor.Desktop.ViewModels;
namespace ComicEditor.UiTests;
public sealed class AgentEndToEndCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        foreach(var name in new[]{"P09Legacy","P10SingleLine"})yield return new("UiAgent.CompleteWorkflow."+name,()=>{
            var original=new ProjectStore().Load("artifacts/private-fixtures/"+name+".json");var dir=TestFiles.Directory();var manifest=Path.Combine(dir,"manifest.json");File.WriteAllText(manifest,new JsonObject{["apiVersion"]=1,["project"]=original.Document.Root.DeepClone(),["assetMap"]=new JsonArray()}.ToJsonString());var initialized=Path.Combine(dir,"initialized.json");Assert.Equal(0,CliCases.Run("init","--manifest",manifest,"--out",initialized).Exit);
            var vm=new EditorViewModel(original.Document);vm.Open(initialized);var window=new MainWindow(vm);var paths=StatePaths.Resolve(dir,true);window.AttachState(paths,null);window.Show();
            try {
                ((System.Windows.Controls.CheckBox)window.FindName("AgentWrite")).IsChecked=true;var panel=new ProjectView(vm.Document).Panels.First();vm.Execute([new("panel.image",panel.Text("id"),new(){["offsetX"]=panel["imageTransform"].Number("offsetX")+1})]);var before=vm.Document.Root.DeepClone();var host=window.AgentSession!;
                var snap=LiveSessionCases.Pump(()=>CliCases.Run("session","snapshot","--session",host.Id,"--registry-dir",paths.Sessions));Assert.Equal(0,snap.Exit);var balloon=new ProjectView(vm.Document).Balloons.First();var patch=Path.Combine(dir,"patch.json");File.WriteAllText(patch,new JsonObject{["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["baseRevision"]=snap.Envelope["data"]!["revision"]!.DeepClone(),["baseProjectHash"]=snap.Envelope["data"]!["hash"]!.DeepClone(),["operations"]=new JsonArray(new JsonObject{["op"]="balloon.move",["targetId"]=balloon.Text("id"),["args"]=new JsonObject{["dx"]=1,["dy"]=1}})}.ToJsonString());Assert.Equal(0,LiveSessionCases.Pump(()=>CliCases.Run("session","apply","--session",host.Id,"--registry-dir",paths.Sessions,"--patch",patch)).Exit);
                Assert.True(JsonNode.DeepEquals(before["panels"],vm.Document.Root["panels"]));for(var i=1;i<before["balloons"]!.AsArray().Count;i++)Assert.True(JsonNode.DeepEquals(before["balloons"]![i],vm.Document.Root["balloons"]![i]));
                snap=LiveSessionCases.Pump(()=>CliCases.Run("session","snapshot","--session",host.Id,"--registry-dir",paths.Sessions));var output=Path.Combine(dir,"final.json");Assert.Equal(0,LiveSessionCases.Pump(()=>CliCases.Run("session","save","--session",host.Id,"--registry-dir",paths.Sessions,"--out",output,"--revision",snap.Envelope["data"]!["revision"]!.ToString(),"--base-hash",snap.Envelope["data"].Text("hash"))).Exit);Assert.Equal(0,CliCases.Run("inspect","--project",output).Exit);
                var render=CliCases.Run("render","--project",output,"--out",Path.Combine(dir,"final.png"),"--scale","2");Assert.Equal(0,render.Exit);var view=new ProjectView(vm.Document);Assert.Equal(view.Width*2d,render.Envelope["data"].Number("width"));Assert.Equal(view.Height*2d,render.Envelope["data"].Number("height"));Assert.Equal(original.Sha256,AtomicFile.Hash(original.AbsolutePath));
            }finally{if(vm.Dirty)vm.Save(Path.Combine(dir,"cleanup.json"),new());window.Close();}
        });
    }
}
