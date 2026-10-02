using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Rendering;
namespace ComicEditor.Tests;
public sealed class UpgradeOrderCases : ITestSuite
{
    public static ProjectDocument Scene()
    {
        var doc = CommandCases.Scene();
        var c = doc.Root["panels"]![1]!.DeepClone(); c["id"] = "C";
        doc.Root["panels"]!.AsArray().Add(c);
        return doc;
    }
    private static string Order(ProjectDocument doc, string kind="panels") => string.Join(",",doc.Root["objectOrder"]![kind]!.AsArray().Select(x=>x!.GetValue<string>()));
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UpgradeOrder.NullOrderMetadataIsRejected",()=>
        {
            var doc=Scene();doc.Root["objectOrder"]=null;
            Assert.True(ProjectValidator.Validate(doc).Any(i=>i.Severity=="error"),"Present null metadata must match schema validation");
        });
        yield return new("UpgradeOrder.NullOrderItemReturnsValidationIssue",()=>
        {
            var doc=Scene();doc.Root["objectOrder"]=JsonNode.Parse("{\"panels\":[null,\"B\",\"C\"]}");
            Assert.True(ProjectValidator.Validate(doc).Any(i=>i.Severity=="error"));
        });
        yield return new("UpgradeOrder.AdjacentTieLockAndUndo", () =>
        {
            var doc = Scene(); doc.Root["panels"]![2]!["locked"] = true;
            var before = doc.Root.ToJsonString();
            var result = new CommandProcessor().Plan(doc,[CommandCases.Op("object.reorder","B","{\"kind\":\"panel\",\"direction\":\"up\"}")]);
            Assert.Equal("A,C,B",Order(result.After));
            Assert.Equal(before,doc.Root.ToJsonString());
            for(var i=0;i<3;i++)Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![i],result.After.Root["panels"]![i]));
            var history=new ComicEditor.Core.History.ProjectHistory(doc);history.Commit(doc,result);history.Undo();Assert.Equal(before,history.Snapshot().Root.ToJsonString());
            Assert.Equal(5,Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[CommandCases.Op("object.reorder","C","{\"kind\":\"panel\",\"direction\":\"down\"}")])).ExitCode);
        });
        yield return new("UpgradeOrder.BoundaryNoopDoesNotCreateExtension", () =>
        {
            var doc=Scene();var result=new CommandProcessor().Plan(doc,[CommandCases.Op("object.reorder","A","{\"kind\":\"panel\",\"direction\":\"down\"}")]);
            Assert.Equal(0,result.Changes.Count);Assert.True(!result.After.Root.ContainsKey("objectOrder"));
            var json=System.Text.Json.JsonSerializer.SerializeToNode(result)!;
            Assert.Equal("NoChange",json["Outcomes"]![0]!["Code"]!.GetValue<string>());
        });
        yield return new("UpgradeOrder.RenameAddRemoveKeepCompleteIds", () =>
        {
            var processor=new CommandProcessor();var doc=processor.Plan(Scene(),[CommandCases.Op("object.reorder","B","{\"kind\":\"panel\",\"direction\":\"up\"}")]).After;
            doc=processor.Plan(doc,[CommandCases.Op("object.rename","B","{\"kind\":\"panel\",\"newId\":\"Z\"}"),CommandCases.Op("panel.remove","C")]).After;
            Assert.Equal("A,Z",Order(doc));
            var panel=doc.Root["panels"]![0]!.DeepClone();panel["id"]="D";
            doc=processor.Plan(doc,[new("panel.add",null,new(){["object"]=panel})]).After;
            Assert.Equal("A,Z,D",Order(doc));
            Assert.True(!ProjectValidator.Validate(doc).Any(x=>x.Severity=="error"));
        });
        yield return new("UpgradeOrder.LayerNumericReinsertsOnlySelected", () =>
        {
            var processor=new CommandProcessor();var doc=processor.Plan(Scene(),[CommandCases.Op("object.reorder","B","{\"kind\":\"panel\",\"direction\":\"up\"}")]).After;
            var unchanged=processor.Plan(doc,[CommandCases.Op("object.layer","B","{\"kind\":\"panel\",\"zIndex\":0}")]).After;
            Assert.Equal("A,C,B",Order(unchanged));
            var changed=processor.Plan(doc,[CommandCases.Op("object.layer","B","{\"kind\":\"panel\",\"zIndex\":-1}")]).After;
            Assert.Equal("B,A,C",Order(changed));
        });
        yield return new("UpgradeOrder.InvalidOrderRejected", () =>
        {
            foreach(var invalid in new[]{"{\"panels\":[\"A\",\"A\",\"C\"]}","{\"panels\":[\"A\",\"B\"]}","{\"panels\":[\"A\",\"B\",\"missing\"]}"})
            {
                var doc=Scene();doc.Root["objectOrder"]=JsonNode.Parse(invalid);
                Assert.True(ProjectValidator.Validate(doc).Any(x=>x.Severity=="error"),"Invalid order must not silently fall back");
            }
        });
        yield return new("UpgradeOrder.ExplicitOrderControlsRenderedOverlap", () =>
        {
            var doc=Scene();var folder=Path.GetDirectoryName(doc.SourcePath)!;
            using(var red=new Bitmap(20,20)){using(var g=Graphics.FromImage(red))g.Clear(Color.Red);red.Save(Path.Combine(folder,"art.png"));}
            using(var blue=new Bitmap(20,20)){using(var g=Graphics.FromImage(blue))g.Clear(Color.Blue);blue.Save(Path.Combine(folder,"b.png"));}
            foreach(var p in doc.Root["panels"]!.AsArray())p!["polygon"]=JsonNode.Parse("[[20,20],[140,20],[140,120],[20,120]]");
            doc.Root["panels"]![1]!["sourceImage"]="b.png";
            doc.Root["objectOrder"]=JsonNode.Parse("{\"panels\":[\"A\",\"C\",\"B\"]}");
            using var page=new PageRenderer().RenderBitmap(doc,new(1));
            Assert.Equal(Color.Blue.ToArgb(),page.Pixels.GetPixel(100,50).ToArgb());
        });
        yield return new("UpgradeOrder.BalloonCopyRenameDeleteAndReadingOrder", () =>
        {
            var doc=Scene();doc.Root["panels"]![0]!["readingOrder"]=2;
            Assert.Equal("B,C,A",string.Join(",",ObjectOrder.Read(doc,"panel")));
            var y=doc.Root["balloons"]![0]!.DeepClone();y["id"]="Y";doc.Root["balloons"]!.AsArray().Add(y);
            var processor=new CommandProcessor();
            doc=processor.Plan(doc,[CommandCases.Op("object.reorder","X","{\"kind\":\"balloon\",\"direction\":\"up\"}")]).After;
            Assert.Equal("Y,X",Order(doc,"balloons"));
            doc=processor.Plan(doc,[CommandCases.Op("balloon.copy","X","{\"newId\":\"Z\"}"),CommandCases.Op("object.rename","X","{\"kind\":\"balloon\",\"newId\":\"W\"}"),CommandCases.Op("balloon.remove","Y")]).After;
            Assert.Equal("W,Z",Order(doc,"balloons"));
            Assert.Equal(0,processor.Plan(doc,[CommandCases.Op("object.reorder","Z","{\"kind\":\"balloon\",\"direction\":\"up\"}")]).Changes.Count);
        });
    }
}
