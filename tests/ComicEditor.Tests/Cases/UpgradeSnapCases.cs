using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
namespace ComicEditor.Tests;
public sealed class UpgradeSnapCases : ITestSuite
{
    private static Operation Snap()=>CommandCases.Op("panel.snap","B","{\"index\":0,\"tolerance\":8}");
    private static JsonNode Outcome(PlannedChange plan)
    {var result=System.Text.Json.JsonSerializer.SerializeToNode(plan)!;Assert.Equal(1,result["Outcomes"]!.AsArray().Count);return result["Outcomes"]![0]!;}
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UpgradeSnap.MovedHasTargetAndNeighborUnchanged",()=>
        {
            var doc=CommandCases.Scene();var result=new CommandProcessor().Plan(doc,[Snap()]);var outcome=Outcome(result);
            Assert.Equal("Moved",outcome["Code"]!.GetValue<string>());Assert.Equal("A",outcome["Data"]!.Text("targetPanelId"));Assert.Equal(2d,outcome["Data"].Number("targetEdgeIndex"));Assert.Equal(6d,outcome["Data"].Number("distance"));
            Assert.Equal(70d,result.After.Root["panels"]![1]!["polygon"]![0]![1]!.GetValue<double>());Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![0],result.After.Root["panels"]![0]));
        });
        yield return new("UpgradeSnap.AlreadyAlignedDoesNotStampAnchors",()=>
        {
            var doc=CommandCases.Scene();var panel=doc.Root["panels"]![1]!.AsObject();panel["polygon"]=JsonNode.Parse("[[20,70],[140,70],[140,126],[20,126]]");panel.Remove("initialPolygon");panel.Remove("initialImageTransform");
            var result=new CommandProcessor().Plan(doc,[Snap()]);Assert.Equal(0,result.Changes.Count);Assert.Equal("AlreadyAligned",Outcome(result)["Code"]!.GetValue<string>());Assert.True(!result.After.Root["panels"]![1]!.AsObject().ContainsKey("initialPolygon"));
        });
        yield return new("UpgradeSnap.NoCandidateDoesNotPretendMoved",()=>
        {
            var doc=CommandCases.Scene();doc.Root["panels"]![1]!["polygon"]=JsonNode.Parse("[[20,90],[140,90],[140,126],[20,126]]");
            var result=new CommandProcessor().Plan(doc,[Snap()]);Assert.Equal(0,result.Changes.Count);Assert.Equal("NoCandidate",Outcome(result)["Code"]!.GetValue<string>());
        });
        yield return new("UpgradeSnap.UnsafeKeepsOriginalPolygon",()=>
        {
            var doc=CommandCases.Scene();doc.Root["panels"]![0]!["polygon"]=JsonNode.Parse("[[20,20],[140,20],[140,76.2],[20,76.2]]");doc.Root["panels"]![1]!["polygon"]=JsonNode.Parse("[[20,76],[140,76],[140,76.25],[20,76.25]]");
            var result=new CommandProcessor().Plan(doc,[Snap()]);Assert.Equal("Unsafe",Outcome(result)["Code"]!.GetValue<string>());Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![1],result.After.Root["panels"]![1]));
        });
    }
}
