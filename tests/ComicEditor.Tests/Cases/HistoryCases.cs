using System.Text.Json.Nodes;
using ComicEditor.Core.History;
using ComicEditor.Core.Commands;
namespace ComicEditor.Tests;
public sealed class HistoryCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("History.BatchUndoRedo",()=>{ var doc=CommandCases.Scene(); var history=new ProjectHistory(doc); var change=new CommandProcessor().Plan(doc,[CommandCases.Op("panel.move","A","{\"dx\":2,\"dy\":3}"),CommandCases.Op("balloon.move","X","{\"dx\":4,\"dy\":5}")]); history.Commit(doc,change); Assert.True(JsonNode.DeepEquals(change.After.Root,history.Snapshot().Root)); Assert.True(JsonNode.DeepEquals(doc.Root,history.Undo().Root)); Assert.True(JsonNode.DeepEquals(change.After.Root,history.Redo().Root)); var snapshot=history.Snapshot(); snapshot.Root["pageId"]="mutated"; Assert.True(!JsonNode.DeepEquals(snapshot.Root,history.Snapshot().Root)); })
    ];
}
