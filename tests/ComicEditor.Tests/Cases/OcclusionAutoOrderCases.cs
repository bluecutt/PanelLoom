using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Tests;

public sealed class OcclusionAutoOrderCases : ITestSuite
{
    public static ProjectDocument Scene()
    {
        var doc = CommandCases.Scene();
        doc.Root["panels"]![1]!["polygon"] = JsonNode.Parse("[[40,20],[120,20],[120,65],[40,65]]");
        var balloons = doc.Root["balloons"]!.AsArray();
        foreach (var id in new[] { "Y", "Z", "W" })
        { var b = balloons[0]!.DeepClone(); b["id"] = id; balloons.Add(b); }
        doc.Root["objectOrder"] = JsonNode.Parse("{\"balloons\":[\"X\",\"Y\",\"Z\",\"W\"]}");
        return doc;
    }
    public static Operation Relation(string id, string panel, string position, bool auto = true) =>
        new("balloon.panelOcclusion", id, new() { ["panelId"] = panel, ["position"] = position, ["autoOrder"] = auto });
    private static JsonObject Balloon(ProjectDocument doc, string id) => new ProjectView(doc).Balloons.First(b => b.Text("id") == id);
    public IEnumerable<TestCase> Cases()
    {
        yield return new("OcclusionAutoOrder.BackMovesOnlySelectedAcrossMultipleLayers", () =>
        {
            var doc = Scene(); Balloon(doc,"X")["panelOcclusion"] = JsonNode.Parse("{\"B\":\"back\"}");
            var before = doc.Root.ToJsonString(); var plan = new CommandProcessor().Plan(doc, [Relation("W","B","back")]);
            Assert.Equal("X,W,Y,Z", string.Join(",", ObjectOrder.Read(plan.After,"balloon")));
            Assert.True(JsonNode.DeepEquals(doc.Root["panels"], plan.After.Root["panels"]));
            foreach (var id in new[] { "X", "Y", "Z" }) Assert.True(JsonNode.DeepEquals(Balloon(doc,id),Balloon(plan.After,id)));
            Assert.True(JsonNode.DeepEquals(Balloon(doc,"W")["transform"],Balloon(plan.After,"W")["transform"]));
            Assert.Equal("A",Balloon(plan.After,"W").Text("clipPanelId"));
            Assert.Equal(before,doc.Root.ToJsonString());
            Assert.Equal("OrderAdjusted",plan.Outcomes!.Single().Code);
            Assert.Equal(-2d,plan.Outcomes!.Single().Data.Number("layerDelta"));
            var nodes = OcclusionOrder.Build(plan.After).ToList();
            Assert.True(nodes.IndexOf(new("balloon","W")) < nodes.IndexOf(new("panel-image","B")));
            Assert.True(nodes.IndexOf(new("panel-image","B")) < nodes.IndexOf(new("balloon","Y")));
        });
        yield return new("OcclusionAutoOrder.FrontMovesUpToNearestLegalPosition", () =>
        {
            var doc = Scene();foreach (var id in new[] { "X", "Y", "Z" }) Balloon(doc,id)["panelOcclusion"]=JsonNode.Parse("{\"B\":\"back\"}");
            var plan = new CommandProcessor().Plan(doc,[Relation("X","B","front")]);
            Assert.Equal("Y,Z,X,W",string.Join(",",ObjectOrder.Read(plan.After,"balloon")));
            Assert.Equal(2d,plan.Outcomes!.Single().Data.Number("layerDelta"));
            foreach (var id in new[] { "Y","Z","W" }) Assert.True(JsonNode.DeepEquals(Balloon(doc,id),Balloon(plan.After,id)));
        });
        yield return new("OcclusionAutoOrder.NoMovementNoExplicitOrderAndRepeatedNoop", () =>
        {
            var doc=CommandCases.Scene();var processor=new CommandProcessor();
            var plan=processor.Plan(doc,[Relation("X","B","back")]);
            Assert.True(plan.After.Root["objectOrder"] is null);
            Assert.Equal("Placed",plan.Outcomes!.Single().Code);
            Assert.Equal(0d,plan.Outcomes!.Single().Data.Number("layerDelta"));
            var repeat=processor.Plan(plan.After,[Relation("X","B","back")]);
            Assert.Equal(0,repeat.Changes.Count);Assert.Equal("NoChange",repeat.Outcomes!.Single().Code);
        });
        yield return new("OcclusionAutoOrder.StrictApiStillRejectsCycle", () =>
        {
            var doc=Scene();var before=doc.Root.ToJsonString();
            Assert.Equal("LAYER_RELATION_CONFLICT",Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[Relation("W","B","back",false)])).Code);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("OcclusionAutoOrder.TrueContradictionRejectsWholeBatchWithTargetDetails", () =>
        {
            var doc=Scene();Balloon(doc,"W")["panelOcclusion"]=JsonNode.Parse("{\"B\":\"front\"}");var before=doc.Root.ToJsonString();
            var error=Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[CommandCases.Op("panel.move","A","{\"dx\":1,\"dy\":0}"),Relation("W","A","back")]));
            Assert.Equal("LAYER_RELATION_CONFLICT",error.Code);
            Assert.True(error.Message.Contains("W")&&error.Message.Contains("A")&&error.Message.Contains("B"),error.Message);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("OcclusionAutoOrder.HiddenAndLockedSiblingsUntouched", () =>
        {
            var doc=Scene();Balloon(doc,"Y")["visible"]=false;Balloon(doc,"Z")["locked"]=true;
            var plan=new CommandProcessor().Plan(doc,[Relation("W","B","back")]);
            Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(plan.After,"balloon")));
            foreach(var id in new[]{"X","Y","Z"})Assert.True(JsonNode.DeepEquals(Balloon(doc,id),Balloon(plan.After,id)));
            Balloon(doc,"W")["locked"]=true;var before=doc.Root.ToJsonString();
            Assert.Equal("LOCKED",Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[Relation("W","B","back")])).Code);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("OcclusionAutoOrder.InvalidFlagAndMissingPanelRejectAtomically", () =>
        {
            var doc=Scene();var before=doc.Root.ToJsonString();var bad=Relation("W","B","back");bad.Args["autoOrder"]="yes";
            Assert.Equal("ARGS",Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[bad])).Code);
            Assert.Equal("OBJECT_NOT_FOUND",Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[Relation("W","missing","back")])).Code);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("OcclusionAutoOrder.InheritCanAlsoFindLegalPlacement", () =>
        {
            var doc=Scene();foreach(var id in new[]{"X","Y","Z"})Balloon(doc,id)["panelOcclusion"]=JsonNode.Parse("{\"B\":\"back\"}");
            var plan=new CommandProcessor().Plan(doc,[Relation("X","B","inherit")]);
            Assert.Equal("Y,Z,X,W",string.Join(",",ObjectOrder.Read(plan.After,"balloon")));
            Assert.True(Balloon(plan.After,"X")["panelOcclusion"] is null);
        });
        yield return new("OcclusionAutoOrder.MultiPanelRulesRemainIntactWhenOrderMoves", () =>
        {
            var doc=Scene();doc.Root["objectOrder"]!["panels"]=JsonNode.Parse("[\"A\",\"B\"]");Balloon(doc,"W")["panelOcclusion"]=JsonNode.Parse("{\"A\":\"front\"}");
            var plan=new CommandProcessor().Plan(doc,[Relation("W","B","back")]);
            Assert.Equal("front",Balloon(plan.After,"W")["panelOcclusion"].Text("A"));
            Assert.Equal("back",Balloon(plan.After,"W")["panelOcclusion"].Text("B"));
            Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(plan.After,"balloon")));
        });
        yield return new("OcclusionAutoOrder.CliDryRunReceiptAndSavedReloadAgree", () =>
        {
            var doc=Scene();var dir=Path.GetDirectoryName(doc.SourcePath)!;
            var saved=new ProjectStore().Save(doc,Path.Combine(dir,"occlusion.json"),new());var patch=Path.Combine(dir,"occlusion.patch.json");var output=Path.Combine(dir,"placed.json");
            File.WriteAllText(patch,new JsonObject{["apiVersion"]=1,["requestId"]=Guid.NewGuid().ToString(),["baseProjectHash"]=saved.Sha256,["operations"]=JsonNode.Parse("[{\"op\":\"balloon.panelOcclusion\",\"targetId\":\"W\",\"args\":{\"panelId\":\"B\",\"position\":\"back\",\"autoOrder\":true}}]")}.ToJsonString());
            var dry=CliCases.Run("apply","--project",saved.AbsolutePath,"--patch",patch,"--out",output,"--dry-run");
            Assert.Equal(0,dry.Exit);Assert.True(!File.Exists(output));
            Assert.Equal("OrderAdjusted",dry.Envelope["data"]!["outcomes"]![0].Text("code"));
            Assert.Equal(-3d,dry.Envelope["data"]!["outcomes"]![0]!["data"].Number("layerDelta"));
            Assert.Equal(0,CliCases.Run("apply","--project",saved.AbsolutePath,"--patch",patch,"--out",output).Exit);
            var loaded=new ProjectStore().Load(output).Document;
            Assert.Equal("W,X,Y,Z",string.Join(",",ObjectOrder.Read(loaded,"balloon")));
            Assert.Equal(saved.Sha256,AtomicFile.Hash(saved.AbsolutePath));
        });
        yield return new("OcclusionAutoOrder.PrivateActualP10BackMatchesManualFixPixels", () =>
        {
            var saved=new ProjectStore().Load("artifacts/diagnostics/2026-10-01-C08-back-A03/snapshot.json");
            var doc=saved.Document;var processor=new CommandProcessor();
            var automatic=processor.Plan(doc,[Relation("C08","A03","back")]);
            Assert.Equal("C01,C02,C03A,C03B,C04,C05,C09,C06,C08,C07",string.Join(",",ObjectOrder.Read(automatic.After,"balloon")));
            Assert.Equal("U03",Balloon(automatic.After,"C08").Text("clipPanelId"));
            var manual=processor.Plan(doc,[CommandCases.Op("object.reorder","C08","{\"kind\":\"balloon\",\"direction\":\"down\"}"),Relation("C08","A03","back",false)]).After;
            using var expected=new ComicEditor.Rendering.PageRenderer().RenderBitmap(manual,new(1));
            using var actual=new ComicEditor.Rendering.PageRenderer().RenderBitmap(automatic.After,new(1));
            Assert.Equal(0,Pixels.Differences(expected.Pixels,actual.Pixels));
            actual.Pixels.Save("artifacts/occlusion-auto-order/2026-10-01/P10-C08-behind-A03-preview.png");
            foreach(var b in new ProjectView(doc).Balloons.Where(b=>b.Text("id")!="C08")) Assert.True(JsonNode.DeepEquals(b,Balloon(automatic.After,b.Text("id"))));
            Assert.True(JsonNode.DeepEquals(doc.Root["panels"],automatic.After.Root["panels"]));
            Assert.Equal(saved.Sha256,AtomicFile.Hash(saved.AbsolutePath));
        });
    }
}
