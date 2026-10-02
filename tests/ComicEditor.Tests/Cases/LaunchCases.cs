using ComicEditor.Cli;
using ComicEditor.Core.Project;
namespace ComicEditor.Tests;
public sealed class LaunchCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("Launch.SafeExactExeAndArguments",()=>{var dir=TestFiles.Directory();var file=Path.Combine(dir,"中文 空格 &.json");new ProjectStore().Save(CommandCases.Scene(),file,new());File.WriteAllText(Path.Combine(dir,"ComicEditor.exe"),"test fixture");var result=DesktopLauncher.Open(file,dir,true,start=>{Assert.Equal(Path.Combine(dir,"ComicEditor.exe"),start.FileName);Assert.True(start.UseShellExecute&&!start.CreateNoWindow,"GUI must detach from CLI standard handles");Assert.Equal("--project",start.ArgumentList[0]);Assert.Equal(file,start.ArgumentList[1]);Assert.Equal("--portable-data",start.ArgumentList[2]);return 123;});Assert.Equal("LaunchRequested",result.State);Assert.True(!result.Visible);Assert.Equal(123,result.ProcessId);}),
        new("Launch.MissingGuiDoesNotUsePath",()=>{var dir=TestFiles.Directory();var file=Path.Combine(dir,"project.json");new ProjectStore().Save(CommandCases.Scene(),file,new());Assert.Throws<ComicEditor.Core.Validation.EditorException>(()=>DesktopLauncher.Open(file,dir,false,_=>throw new Exception("Must not launch")));})
    ];
}
