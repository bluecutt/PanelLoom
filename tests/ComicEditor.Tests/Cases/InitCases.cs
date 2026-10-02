using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Tests;
public sealed class InitCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("Init.AllPanelsAndLetteredObjectsLoad",()=>{ var doc=CommandCases.Scene(); var manifest=new JsonObject{["apiVersion"]=1,["project"]=doc.Root.DeepClone(),["assetMap"]=JsonNode.Parse("""[{"kind":"panel","id":"A","sourceImage":"b.png"},{"kind":"balloon","id":"X","sourceImage":"b.png"}]""")}; var path=Path.Combine(Path.GetDirectoryName(doc.SourcePath)! ,"manifest.json"); File.WriteAllText(path,manifest.ToJsonString()); var result=ProjectInitializer.Create(path); Assert.Equal(2,new ProjectView(result).Panels.Count()); Assert.Equal("Complete",result.Root["balloons"]![0]!.Text("kind")); Assert.Equal("A",result.Root["balloons"]![0]!.Text("clipPanelId")); Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![0]!["polygon"],result.Root["panels"]![0]!["polygon"])); Assert.Equal("b.png",result.Root["panels"]![0]!.Text("sourceImage")); }),
        new("Init.UnknownMappedObjectRejected",()=>{ var doc=CommandCases.Scene(); var manifest=new JsonObject{["apiVersion"]=1,["project"]=doc.Root.DeepClone(),["assetMap"]=JsonNode.Parse("""[{"kind":"panel","id":"wrong","sourceImage":"b.png"}]""")}; var path=Path.Combine(Path.GetDirectoryName(doc.SourcePath)!,"wrong.json"); File.WriteAllText(path,manifest.ToJsonString()); Assert.Throws<EditorException>(()=>ProjectInitializer.Create(path)); })
    ];
}
