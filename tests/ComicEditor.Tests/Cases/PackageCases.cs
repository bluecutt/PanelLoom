using System.Diagnostics;
namespace ComicEditor.Tests;
public sealed class PackageCases : ITestSuite
{
    public static string PackagePath=>Environment.GetEnvironmentVariable("COMIC_EDITOR_PACKAGE")??Path.GetFullPath("dist/ComicEditor-0.2.0-preview.2-win-x64");
    public static (int Exit,string Output) Script(string script,params string[] args)
    {
        Assert.True(File.Exists(script),"Required package script missing: "+script);
        var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell/v1.0/powershell.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{"-NoProfile","-ExecutionPolicy","Bypass","-File",Path.GetFullPath(script)}.Concat(args))start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();if(!process.WaitForExit(60000)){process.Kill();throw new Exception("Package script timed out");}return(process.ExitCode,stdout.Result+stderr.Result);
    }
    public IEnumerable<TestCase> Cases()=>[
        new("Package.UpgradeCapabilities",()=>{
            var start=new ProcessStartInfo(Path.Combine(PackagePath,"ComicEditor.Cli.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true};start.ArgumentList.Add("capabilities");using var p=Process.Start(start)!;var text=p.StandardOutput.ReadToEnd();Assert.True(p.WaitForExit(10000));Assert.Equal(0,p.ExitCode);var caps=System.Text.Json.Nodes.JsonNode.Parse(text)!;var actions=caps["data"]!["actions"]!.AsArray();Assert.True(actions.Any(a=>ComicEditor.Core.Project.JsonRead.Text(a,"name")=="object.reorder"),"Portable reorder action required");var occlusion=actions.Single(a=>ComicEditor.Core.Project.JsonRead.Text(a,"name")=="balloon.panelOcclusion");Assert.True(occlusion!["allowedArgs"]!.AsArray().Any(a=>a!.GetValue<string>()=="autoOrder"),"Portable app must support Agent autoOrder input");
        }),
        new("Package.PublicSuiteDoesNotRequirePrivatePages",()=>{
            var method=typeof(TestHarness).GetMethod("IsPublic",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;Assert.True(!(bool)method.Invoke(null,["UpgradeOcclusionRender.PrivateP10ApprovedRoi"])!,"Private-page test must not enter portable Public suite");
        }),
        new("Package.SharedDesktopRuntimeMatches",()=>{var stage=Directory.GetDirectories("artifacts/release-staging").OrderByDescending(Directory.GetLastWriteTimeUtc).First();Assert.Equal(ComicEditor.Core.Project.AtomicFile.Hash(Path.Combine(stage,"Desktop/WindowsBase.dll")),ComicEditor.Core.Project.AtomicFile.Hash(Path.Combine(stage,"Cli/WindowsBase.dll")));}),
        new("Package.TwoEntrypointsWithoutSystemDotnet",()=>{var r=Script("scripts/Test-Package.ps1","-PackageDirectory",PackagePath);Assert.Equal(0,r.Exit);Assert.True(r.Output.Contains("PACKAGE_RUNTIME_PASS"),r.Output);}),
        new("Package.NoPrivateAssetsOrHardcodedPaths",()=>{var r=Script("scripts/Scan-Public-Package.ps1","-Directory",PackagePath);Assert.Equal(0,r.Exit);Assert.True(r.Output.Contains("PUBLIC_SCAN_PASS"),r.Output);}),
        new("Package.ConflictingSharedFileFails",()=>{var dir=TestFiles.Directory();var one=Directory.CreateDirectory(Path.Combine(dir,"one")).FullName;var two=Directory.CreateDirectory(Path.Combine(dir,"two")).FullName;File.WriteAllText(Path.Combine(one,"shared.dll"),"one");File.WriteAllText(Path.Combine(two,"shared.dll"),"two");var output=Path.Combine(dir,"out");var r=Script("scripts/Merge-Publish.ps1","-First",one,"-Second",two,"-Destination",output);Assert.True(r.Exit!=0);Assert.True(!Directory.Exists(output),"Preflight conflict must not create partial destination");})
    ];
}
