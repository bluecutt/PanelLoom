using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Tests;
public sealed class UpgradeOcclusionCommandCases : ITestSuite
{
    private static Operation Relation(string balloon,string panel,string position)=>new("balloon.panelOcclusion",balloon,new(){["panelId"]=panel,["position"]=position});
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UpgradeOcclusionCommand.ExistingRelationBlocksUnsafeReorder",()=>
        {
            var doc=CommandCases.Scene();doc.Root["panels"]![1]!["polygon"]=JsonNode.Parse("[[40,20],[120,20],[120,65],[40,65]]");
            var y=doc.Root["balloons"]![0]!.DeepClone();y["id"]="Y";doc.Root["balloons"]!.AsArray().Add(y);
            var processor=new CommandProcessor();doc=processor.Plan(doc,[Relation("X","B","back")]).After;
            var before=doc.Root.ToJsonString();
            Assert.Equal("LAYER_RELATION_CONFLICT",Assert.Throws<EditorException>(()=>processor.Plan(doc,[CommandCases.Op("object.reorder","X","{\"kind\":\"balloon\",\"direction\":\"up\"}")])).Code);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("UpgradeOcclusionCommand.OptionalAtomicReferences",()=>
        {
            var doc=CommandCases.Scene();var before=doc.Root.ToJsonString();var processor=new CommandProcessor();
            var front=processor.Plan(doc,[Relation("X","B","front")]).After;
            Assert.Equal("front",front.Root["balloons"]![0]!["panelOcclusion"].Text("B"));
            Assert.Equal(before,doc.Root.ToJsonString());
            var back=processor.Plan(doc,[Relation("X","B","back")]).After;
            Assert.Equal("back",back.Root["balloons"]![0]!["panelOcclusion"].Text("B"));
            var inherited=processor.Plan(front,[Relation("X","B","inherit")]).After;
            Assert.True(!inherited.Root["balloons"]![0]!.AsObject().ContainsKey("panelOcclusion"));
            Assert.Equal(0,processor.Plan(doc,[Relation("X","B","inherit")]).Changes.Count);
        });
        yield return new("UpgradeOcclusionCommand.RenameDeleteAndLockedDependencies",()=>
        {
            var processor=new CommandProcessor();var doc=processor.Plan(CommandCases.Scene(),[Relation("X","B","front")]).After;
            var renamed=processor.Plan(doc,[CommandCases.Op("object.rename","B","{\"kind\":\"panel\",\"newId\":\"Z\"}")]).After;
            Assert.Equal("front",renamed.Root["balloons"]![0]!["panelOcclusion"].Text("Z"));
            Assert.True(renamed.Root["balloons"]![0]!["panelOcclusion"]!["B"] is null);
            Assert.Equal("OCCLUSION_REFERENCE",Assert.Throws<EditorException>(()=>processor.Plan(doc,[CommandCases.Op("panel.remove","B")])).Code);
            var removed=processor.Plan(doc,[CommandCases.Op("panel.remove","B","{\"clipPolicy\":\"free\"}")]).After;
            Assert.Equal("A",removed.Root["balloons"]![0].Text("clipPanelId"));
            Assert.True(!removed.Root["balloons"]![0]!.AsObject().ContainsKey("panelOcclusion"));
            doc.Root["balloons"]![0]!["locked"]=true;var before=doc.Root.ToJsonString();
            Assert.Equal(5,Assert.Throws<EditorException>(()=>processor.Plan(doc,[CommandCases.Op("panel.remove","B","{\"clipPolicy\":\"free\"}")])).ExitCode);
            Assert.Equal(5,Assert.Throws<EditorException>(()=>processor.Plan(doc,[CommandCases.Op("object.rename","B","{\"kind\":\"panel\",\"newId\":\"Z\"}")])).ExitCode);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("UpgradeOcclusionCommand.CycleRejectsWholeBatch",()=>
        {
            var doc=CommandCases.Scene();var second=doc.Root["balloons"]![0]!.DeepClone();second["id"]="Y";doc.Root["balloons"]!.AsArray().Add(second);
            var before=doc.Root.ToJsonString();
            Assert.Equal("LAYER_RELATION_CONFLICT",Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[Relation("X","B","front"),Relation("Y","B","back")])).Code);
            Assert.Equal(before,doc.Root.ToJsonString());
        });
        yield return new("UpgradeOcclusionCommand.InvalidAndLockedRelationRejected",()=>
        {
            var processor=new CommandProcessor();var doc=CommandCases.Scene();
            Assert.Equal("ARGS",Assert.Throws<EditorException>(()=>processor.Plan(doc,[Relation("X","B","above")])).Code);
            Assert.Equal("OBJECT_NOT_FOUND",Assert.Throws<EditorException>(()=>processor.Plan(doc,[Relation("X","missing","front")])).Code);
            doc.Root["balloons"]![0]!["locked"]=true;
            Assert.Equal(5,Assert.Throws<EditorException>(()=>processor.Plan(doc,[Relation("X","B","front")])).ExitCode);
        });
        yield return new("UpgradeOcclusionCommand.SchemaRefsAndRoundtrip",()=>
        {
            foreach(var invalid in new[]{"{\"missing\":\"front\"}","{\"B\":\"above\"}","{\"B\":false}","null"})
            {
                var doc=CommandCases.Scene();doc.Root["balloons"]![0]!["panelOcclusion"]=JsonNode.Parse(invalid);
                Assert.True(ProjectValidator.Validate(doc).Any(i=>i.Severity=="error"));
            }
            var valid=new CommandProcessor().Plan(CommandCases.Scene(),[Relation("X","B","front")]).After;
            var saved=new ProjectStore().Save(valid,Path.Combine(TestFiles.Directory(),"覆盖关系.json"),new());
            var reopened=new ProjectStore().Load(saved.AbsolutePath);
            Assert.Equal("front",reopened.Document.Root["balloons"]![0]!["panelOcclusion"].Text("B"));
        });
    }
}
