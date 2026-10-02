using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Validation;
namespace ComicEditor.Tests;
public sealed class CommandCases : ITestSuite
{
    public static ProjectDocument Scene()
    {
        var doc=TestFiles.Neutral(); var dir=TestFiles.Directory();
        using(var image=new System.Drawing.Bitmap(20,20)) { image.Save(Path.Combine(dir,"art.png")); image.Save(Path.Combine(dir,"b.png")); }
        doc=doc with { SourcePath=Path.Combine(dir,"project.json") };
        var p=doc.Root["panels"]![0]!.AsObject(); p["polygon"]=JsonNode.Parse("[[20,20],[140,20],[140,70],[20,70]]");
        p["initialPolygon"]=JsonNode.Parse("[[19,20],[139,20],[139,70],[19,70]]");
        p["imageTransform"]=JsonNode.Parse("{\"offsetX\":1,\"scale\":1.25}"); p["initialImageTransform"]=JsonNode.Parse("{\"offsetX\":0,\"scale\":1}");
        var b=p.DeepClone().AsObject(); b["id"]="B"; b["polygon"]=JsonNode.Parse("[[20,76],[140,76],[140,126],[20,126]]"); doc.Root["panels"]!.AsArray().Add(b);
        doc.Root["balloons"]!.AsArray().Add(JsonNode.Parse("""{"id":"X","kind":"Complete","sourceImage":"b.png","clipPanelId":"A","groupId":"g","transform":{"x":30,"y":30,"width":40,"height":40},"initialTransform":{"x":25,"y":25,"width":40,"height":40}}"""));
        return doc;
    }
    public static Operation Op(string name,string? target,string json="{}")=>new(name,target,JsonNode.Parse(json)!.AsObject());
    public static Operation[] SuccessOperations() => [
        Op("panel.add",null,"""{"object":{"id":"C","sourceImage":"art.png","polygon":[[0,0],[10,0],[10,10],[0,10]]}}"""),
        Op("panel.remove","B"),Op("balloon.add",null,"""{"object":{"id":"Y","sourceImage":"b.png","kind":"Tail","transform":{"width":20,"height":20}}}"""),Op("balloon.remove","X"),
        Op("object.rename","A","""{"kind":"panel","newId":"A2","label":"new"}"""),
        Op("panel.polygon","A","""{"points":[[20,20],[130,20],[140,60],[20,70]]}"""),Op("panel.vertex","A","""{"index":0,"x":21,"y":21}"""),
        Op("panel.bounds","A","""{"x":21,"y":22,"width":100,"height":40}"""),Op("panel.move","A","""{"dx":2,"dy":3,"moveImage":true}"""),
        Op("panel.image","A","""{"offsetX":3,"offsetY":4,"scale":1.3,"fitMode":"Contain","fitBiasX":0.2,"fitBiasY":0.3}"""),
        Op("asset.replace","A","""{"kind":"panel","sourceImage":"b.png","fitPolicy":"preserve"}"""),
        Op("panel.edge","A","""{"index":0,"visible":false}"""),Op("panel.snap","B","""{"index":0,"tolerance":8}"""),
        Op("page.border",null,"""{"color":"#111111","width":3,"mode":"SingleLine","outerEnabled":false,"outerWidth":2}"""),
        Op("page.snap",null,"""{"enabled":true,"tolerance":7}"""),Op("page.canvas",null,"""{"width":200,"height":180,"background":"#EEEEEE","exportScale":2}"""),
        Op("object.layer","A","""{"kind":"panel","zIndex":15}"""),Op("object.reorder","A","""{"kind":"panel","direction":"up"}"""),Op("object.lock","A","""{"kind":"panel","locked":true}"""),
        Op("balloon.transform","X","""{"x":45,"y":50,"width":50,"rotation":30,"flipX":true,"flipY":true,"opacity":0.8}"""),
        Op("balloon.clip","X","""{"clipPanelId":""}"""),Op("balloon.panelOcclusion","X","""{"panelId":"B","position":"front"}"""),Op("balloon.group","X","""{"groupId":"new"}"""),Op("balloon.visible","X","""{"visible":false}"""),
        Op("balloon.copy","X","""{"newId":"Y","dx":4,"dy":5}"""),Op("balloon.move","X","""{"dx":4,"dy":5,"moveGroup":true}"""),
        Op("object.reset","A","""{"kind":"panel","scope":"image"}""")
    ];
    public IEnumerable<TestCase> Cases()
    {
        yield return new("Commands.RegistryComplete",()=>Assert.Equal(string.Join(",",SuccessOperations().Select(x=>x.Op).Order()),string.Join(",",CommandRegistry.Actions.Select(x=>x.Name).Order())));
        foreach(var operation in SuccessOperations())
        {
            yield return new("Commands.Success."+operation.Op,()=>{ var doc=Scene(); var before=doc.Root.ToJsonString(); var result=new CommandProcessor().Plan(doc,[operation]); Assert.True(result.Changes.Count>0); Assert.Equal(before,doc.Root.ToJsonString()); });
            yield return new("Commands.RejectUnknownArg."+operation.Op,()=>{ var bad=operation with { Args=(JsonObject)operation.Args.DeepClone() }; bad.Args["executeCode"]="forbidden"; Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(Scene(),[bad])); });
        }
        yield return new("Commands.OnlyRequestedPanelChanges",()=>{ var doc=Scene(); var result=new CommandProcessor().Plan(doc,[Op("panel.vertex","A","""{"index":0,"x":22,"y":23}""")]); Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![1],result.After.Root["panels"]![1])); Assert.True(JsonNode.DeepEquals(doc.Root["balloons"],result.After.Root["balloons"])); });
        yield return new("Commands.AtomicFailure",()=>{ var doc=Scene(); var before=doc.Root.ToJsonString(); Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[Op("panel.move","A","{\"dx\":5,\"dy\":6}"),Op("unknown",null)])); Assert.Equal(before,doc.Root.ToJsonString()); });
        yield return new("Commands.LockBlocksChanges",()=>{ var doc=Scene(); doc.Root["panels"]![0]!["locked"]=true; Assert.Equal(5,Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[Op("panel.move","A","{\"dx\":5,\"dy\":6}")])).ExitCode); });
        yield return new("Commands.DeleteClipAndRenameReferences",()=>{ var doc=Scene(); var processor=new CommandProcessor(); Assert.Throws<EditorException>(()=>processor.Plan(doc,[Op("panel.remove","A")])); var removed=processor.Plan(doc,[Op("panel.remove","A","{\"clipPolicy\":\"free\"}")]); Assert.Equal("",removed.After.Root["balloons"]![0]!.Text("clipPanelId")); var renamed=processor.Plan(doc,[Op("object.rename","A","{\"kind\":\"panel\",\"newId\":\"Z\"}")]); Assert.Equal("Z",renamed.After.Root["balloons"]![0]!.Text("clipPanelId")); });
        yield return new("Commands.AspectLockAndStretch",()=>{ var doc=Scene(); var processor=new CommandProcessor(); var result=processor.Plan(doc,[Op("balloon.transform","X","{\"width\":60}")]); Assert.Equal(60d,result.After.Root["balloons"]![0]!["transform"].Number("height")); Assert.Throws<EditorException>(()=>processor.Plan(doc,[Op("balloon.transform","X","{\"width\":60,\"height\":30}")])); processor.Plan(doc,[Op("balloon.transform","X","{\"lockAspect\":false,\"width\":60,\"height\":30}")]); });
        yield return new("Commands.SnapIsIndependent",()=>{ var doc=Scene(); var result=new CommandProcessor().Plan(doc,[Op("panel.snap","B","{\"index\":0,\"tolerance\":8}")]); Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![0],result.After.Root["panels"]![0])); Assert.Equal(70d,result.After.Root["panels"]![1]!["polygon"]![0]![1]!.GetValue<double>()); });
        yield return new("Commands.InvalidBooleanRejected",()=>Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(Scene(),[Op("page.border",null,"{\"outerEnabled\":\"yes\"}")])));
        yield return new("Commands.NumericOverflowRejected",()=>Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(Scene(),[Op("panel.move","A","{\"dx\":1e999,\"dy\":0}")])));
        yield return new("Commands.GroupMoveSkipsLockedSibling",()=>{ var doc=Scene(); var sibling=doc.Root["balloons"]![0]!.DeepClone(); sibling["id"]="Y"; sibling["locked"]=true; doc.Root["balloons"]!.AsArray().Add(sibling); var result=new CommandProcessor().Plan(doc,[Op("balloon.move","X","{\"dx\":5,\"dy\":6,\"moveGroup\":true}")]); Assert.True(JsonNode.DeepEquals(doc.Root["balloons"]![1],result.After.Root["balloons"]![1])); });
        yield return new("Commands.PanelResetAndBalloonReset",()=>{ var doc=Scene(); var result=new CommandProcessor().Plan(doc,[Op("object.reset","A","{\"kind\":\"panel\",\"scope\":\"frame\"}"),Op("object.reset","X","{\"kind\":\"balloon\",\"scope\":\"balloon\"}")]); Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![0]!["initialPolygon"],result.After.Root["panels"]![0]!["polygon"])); Assert.True(JsonNode.DeepEquals(doc.Root["balloons"]![0]!["initialTransform"],result.After.Root["balloons"]![0]!["transform"])); });
    }
}
