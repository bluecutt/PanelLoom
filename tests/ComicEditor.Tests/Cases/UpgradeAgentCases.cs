using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Rendering;
namespace ComicEditor.Tests;
public sealed class UpgradeAgentCases : ITestSuite
{
    public static JsonArray Operations()=>JsonNode.Parse("""[{"op":"asset.replace","targetId":"A","args":{"kind":"panel","sourceImage":"art.png","fitPolicy":"preserve"}},{"op":"asset.replace","targetId":"X","args":{"kind":"balloon","sourceImage":"b.png","fitPolicy":"preserve"}},{"op":"panel.snap","targetId":"B","args":{"index":0}},{"op":"object.reorder","targetId":"A","args":{"kind":"panel","direction":"up"}},{"op":"balloon.panelOcclusion","targetId":"X","args":{"panelId":"B","position":"front"}}]""")!.AsArray();
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UpgradeAgent.OfflineOutcomeDryRunAndPixels",()=>{
            var scene=CommandCases.Scene();var dir=Path.GetDirectoryName(scene.SourcePath!)!;var manifest=Path.Combine(dir,"初始化 中文.json");File.WriteAllText(manifest,new JsonObject{["apiVersion"]=1,["project"]=scene.Root.DeepClone(),["assetMap"]=new JsonArray()}.ToJsonString());var project=Path.Combine(dir,"工程 中文.json");Assert.Equal(0,CliCases.Run("init","--manifest",manifest,"--out",project).Exit);
            var before=new ProjectStore().Load(project);var patch=Path.Combine(dir,"更新 请求.json");File.WriteAllText(patch,new JsonObject{["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["baseProjectHash"]=before.Sha256,["operations"]=Operations()}.ToJsonString());var output=Path.Combine(dir,"输出 工程.json");
            var dry=CliCases.Run("apply","--project",project,"--patch",patch,"--out",output,"--dry-run");Assert.Equal(0,dry.Exit);Assert.True(dry.Envelope["data"]!["outcomes"] is JsonArray,"Agent needs actual operation outcomes");Assert.True(!File.Exists(output));Assert.Equal(before.Sha256,AtomicFile.Hash(project));
            var applied=CliCases.Run("apply","--project",project,"--patch",patch,"--out",output);Assert.Equal(0,applied.Exit);Assert.True(JsonNode.DeepEquals(dry.Envelope["data"]!["outcomes"],applied.Envelope["data"]!["outcomes"]));
            var ops=Operations().Select(o=>new Operation(o!.Text("op"),o.Text("targetId"),o!["args"]!.DeepClone().AsObject())).ToArray();var expected=new CommandProcessor().Plan(before.Document,ops).After;using var reference=new PageRenderer().RenderBitmap(expected,new(1.75));var png=Path.Combine(dir,"高清 页.png");Assert.Equal(0,CliCases.Run("render","--project",output,"--out",png,"--scale","1.75").Exit);using var actual=new Bitmap(png);Assert.Equal(0,Pixels.Differences(reference.Pixels,actual));
        });
    }
}
