using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using ComicEditor.Core.AppState;
using ComicEditor.Core.Project;
using ComicEditor.Desktop;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;

public sealed class SkillAdapterEndToEndCases : ITestSuite
{
    private static readonly string ProjectRoot = Path.GetFullPath("../../..");
    private static string CliPath()
    {
        var configPath = Path.Combine(ProjectRoot,"comic-project.json");
        var config = JsonNode.Parse(File.ReadAllText(configPath))!;
        return Path.GetFullPath(Path.Combine(ProjectRoot,config.Text("editorCliPath")));
    }
    private static (int Exit,JsonObject Envelope) Cli(params string[] args)
    {
        var start = new ProcessStartInfo(CliPath()){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
        foreach(var arg in args)start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;
        var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();
        if(!process.WaitForExit(45000)){process.Kill();throw new Exception("Isolated verification CLI timed out");}
        var text=output.GetAwaiter().GetResult();var stderr=error.GetAwaiter().GetResult();
        Assert.True(string.IsNullOrWhiteSpace(stderr),stderr);
        return(process.ExitCode,JsonNode.Parse(text)!.AsObject());
    }
    private static JsonObject Balloon(ProjectDocument doc,string id)=>new ProjectView(doc).Balloons.Single(b=>b.Text("id")==id);
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiSkillNative.PrivateP09ActualWorkflow",()=>Verify("P09","artifacts/P09_0.2.0-preview.2_单线验收工作副本_v01.json",9,13));
        yield return new("UiSkillNative.PrivateP10ActualWorkflow",()=>Verify("P10","artifacts/P10_0.2.0-preview.2_恢复工作副本_20261001_v01.json",6,10));
    }
    private static void Verify(string page,string source,int panelCount,int balloonCount)
    {
        var loaded=new ProjectStore().Load(source);var sourceHash=loaded.Sha256;
        var dir=Path.GetFullPath(Path.Combine("artifacts/skill-native-sync/20261001/verify",page+"-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(dir);
        var capabilities=Cli("capabilities");Assert.Equal(0,capabilities.Exit);Assert.Equal(1d,capabilities.Envelope.Number("apiVersion"));
        Assert.Equal(2d,capabilities.Envelope["data"].Number("projectVersion"));
        var project=loaded.Document.Root.DeepClone().AsObject();
        project["assetBase"]=ProjectPaths.AssetBase(loaded.Document);
        var manifestPath=Path.Combine(dir,"manifest.json");
        File.WriteAllText(manifestPath,new JsonObject{["apiVersion"]=1,["project"]=project,["assetMap"]=new JsonArray()}.ToJsonString());
        var initialized=Path.Combine(dir,"initialized.json");
        Assert.Equal(0,Cli("init","--manifest",manifestPath,"--out",initialized).Exit);
        var inspect=Cli("inspect","--project",initialized);Assert.Equal(0,inspect.Exit);
        Assert.Equal(panelCount,inspect.Envelope["data"]!["project"]!["panels"]!.AsArray().Count);
        Assert.Equal(balloonCount,inspect.Envelope["data"]!["project"]!["balloons"]!.AsArray().Count);
        Assert.True(inspect.Envelope["data"]!["assets"]!.AsArray().All(a=>!a!.Flag("missing")&&a!["error"] is null));

        var vm=new EditorViewModel(new ProjectStore().Load(initialized).Document);vm.Open(initialized);
        var window=new MainWindow(vm){ShowActivated=false,ShowInTaskbar=false,WindowState=WindowState.Minimized};
        var paths=StatePaths.Resolve(dir,true);window.AttachState(paths,null);window.Show();
        try
        {
            var host=window.AgentSession!;Assert.True(host is not null);
            var registry=paths.Sessions;
            (int Exit,JsonObject Envelope) Live(params string[] args)=>LiveSessionCases.Pump(()=>Cli(args));
            JsonObject Snapshot()=>Live("session","snapshot","--session",host!.Id,"--registry-dir",registry).Envelope["data"]!.DeepClone().AsObject();
            var patchPath=Path.Combine(dir,"replace.patch.json");
            JsonObject Patch(JsonObject snapshot)=>new(){["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["baseRevision"]=snapshot["revision"]!.DeepClone(),["baseProjectHash"]=snapshot["hash"]!.DeepClone(),["operations"]=new JsonArray(new JsonObject{["op"]="asset.replace",["targetId"]="C08",["args"]=new JsonObject{["kind"]="balloon",["sourceImage"]=Path.Combine(dir,"C08-replacement.png"),["fitPolicy"]="preserve"}})};
            var original=vm.Document.Root.ToJsonString();
            var input=new InputRouter(vm);
            input.Begin(new("panel","U01",DragKind.Image),new(20,20));input.Update(new(27,25));input.Commit();
            Assert.Equal(loaded.Document.Root["panels"]![0]!["imageTransform"].Number("offsetX")+7,vm.Document.Root["panels"]![0]!["imageTransform"].Number("offsetX"));
            vm.Undo();Assert.Equal(original,vm.Document.Root.ToJsonString());vm.Redo();
            input.Begin(new("balloon","C01",DragKind.Balloon),new(20,20));input.Update(new(23,22));input.Commit();
            Assert.Equal(Balloon(loaded.Document,"C01")["transform"].Number("x")+3,Balloon(vm.Document,"C01")["transform"].Number("x"));
            vm.Select("panel","U01");window.PreviewCanvas.HandleWheel(120,new(50,50),System.Windows.Input.ModifierKeys.None);UpgradeGestureCases.Pump(310);
            Assert.True(vm.Dirty,"Actual canvas changes must stay unsaved before Agent replacement");
            var beforeAgent=vm.Document.DeepClone();var beforeJson=beforeAgent.Root.ToJsonString();
            var replacement=Path.Combine(dir,"C08-replacement.png");File.Copy(ProjectPaths.Resolve(beforeAgent,Balloon(beforeAgent,"C08").Text("sourceImage")),replacement);
            File.WriteAllText(patchPath,Patch(Snapshot()).ToJsonString());
            Assert.Equal(5,Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath).Exit);
            Assert.Equal(beforeJson,vm.Document.Root.ToJsonString());
            ((CheckBox)window.FindName("AgentWrite")).IsChecked=true;
            var snap=Snapshot();var patch=Patch(snap);File.WriteAllText(patchPath,patch.ToJsonString());
            var revision=vm.Revision;
            var dry=Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath,"--dry-run");
            Assert.Equal(0,dry.Exit);Assert.Equal(revision,vm.Revision);Assert.Equal(beforeJson,vm.Document.Root.ToJsonString());
            var applied=Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath);
            Assert.Equal(0,applied.Exit);Assert.Equal(revision+1,vm.Revision);
            Assert.Equal(replacement,Balloon(vm.Document,"C08").Text("sourceImage"));
            Assert.True(JsonNode.DeepEquals(Balloon(beforeAgent,"C08")["transform"],Balloon(vm.Document,"C08")["transform"]));
            Assert.Equal(Balloon(beforeAgent,"C08").Text("clipPanelId"),Balloon(vm.Document,"C08").Text("clipPanelId"));
            Assert.True(JsonNode.DeepEquals(beforeAgent.Root["panels"],vm.Document.Root["panels"]));
            foreach(var b in new ProjectView(beforeAgent).Balloons.Where(b=>b.Text("id")!="C08"))Assert.True(JsonNode.DeepEquals(b,Balloon(vm.Document,b.Text("id"))));
            Assert.True(JsonNode.DeepEquals(beforeAgent.Root["canvas"],vm.Document.Root["canvas"]));
            var repeat=Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath);
            Assert.True(JsonNode.DeepEquals(applied.Envelope,repeat.Envelope));Assert.Equal(revision+1,vm.Revision);
            vm.Undo();Assert.Equal(beforeJson,vm.Document.Root.ToJsonString());vm.Redo();
            patch["requestId"]=Guid.NewGuid().ToString();File.WriteAllText(patchPath,patch.ToJsonString());
            Assert.Equal(4,Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath).Exit);
            vm.Busy=true;try{Assert.Equal(6,Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath).Exit);}finally{vm.Busy=false;}
            var current=Snapshot();var saved=Path.Combine(dir,"live-saved.json");
            Assert.Equal(0,Live("session","save","--session",host!.Id,"--registry-dir",registry,"--out",saved,"--revision",current["revision"]!.ToString(),"--base-hash",current.Text("hash")).Exit);
            var reopened=new ProjectStore().Load(saved);
            Assert.True(JsonNode.DeepEquals(vm.Document.Root,reopened.Document.Root));Assert.True(!vm.Dirty);
            using(var renderer=new ComicEditor.Rendering.PageRenderer().RenderBitmap(reopened.Document,new(1)))Assert.Equal(1024,renderer.Pixels.Width);
            var final=Path.Combine(dir,"candidate-2.5x.png");var render=Cli("render","--project",saved,"--out",final,"--scale","2.5");
            Assert.Equal(0,render.Exit);Assert.Equal(2560d,render.Envelope["data"].Number("width"));Assert.Equal(3840d,render.Envelope["data"].Number("height"));
            using(var actual=new System.Drawing.Bitmap(final)){Assert.Equal(2560,actual.Width);Assert.Equal(3840,actual.Height);}
            ((CheckBox)window.FindName("AgentWrite")).IsChecked=false;
            File.WriteAllText(patchPath,Patch(Snapshot()).ToJsonString());Assert.Equal(5,Live("session","apply","--session",host!.Id,"--registry-dir",registry,"--patch",patchPath).Exit);
            Assert.Equal(sourceHash,AtomicFile.Hash(source));
            Console.WriteLine($"SKILL_NATIVE_WORKFLOW_PASS {page}: panels={panelCount} balloons={balloonCount}; init/manual-drag/wheel/live-replace/undo-redo/save-reopen/export=2560x3840; directory={dir}");
        }
        finally
        {
            vm.DiscardDrafts();vm.Busy=false;
            // Only this isolated test document may be dirty on a failed assertion.
            // Preserve it as evidence before closing so teardown cannot prompt over the user's windows.
            if(vm.Dirty)vm.Save(Path.Combine(dir,"teardown-state.json"),new());
            window.Close();
        }
    }
}
