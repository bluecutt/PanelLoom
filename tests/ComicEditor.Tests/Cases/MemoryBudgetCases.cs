using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Rendering;
namespace ComicEditor.Tests;
public sealed class MemoryBudgetCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("Memory.HugeOutputFailsBeforeAllocation",()=>{ var doc=CommandCases.Scene(); doc.Root["canvas"]!["width"]=100000; doc.Root["canvas"]!["height"]=100000; var path=Path.Combine(TestFiles.Directory(),"huge.png"); Assert.Equal("MEMORY_BUDGET",Assert.Throws<EditorException>(()=>new PageRenderer().Render(doc,path,new(2))).Code); Assert.True(!File.Exists(path)); }),
        new("Memory.CancellationLeavesNoFinalFile",()=>{ var token=new CancellationToken(true); var path=Path.Combine(TestFiles.Directory(),"cancelled.png"); Assert.Throws<OperationCanceledException>(()=>new PageRenderer().Render(CommandCases.Scene(),path,new(),token)); Assert.True(!File.Exists(path)); }),
        new("Memory.ExistingOutputConflict",()=>{ var path=Path.Combine(TestFiles.Directory(),"existing.png"); File.WriteAllText(path,"existing"); Assert.Equal("CONFLICT",Assert.Throws<EditorException>(()=>new PageRenderer().Render(CommandCases.Scene(),path,new())).Code); Assert.Equal("existing",File.ReadAllText(path)); })
    ];
}
