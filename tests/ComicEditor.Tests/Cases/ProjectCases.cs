using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using System.Globalization;
using System.Security.AccessControl;
using System.Security.Principal;
namespace ComicEditor.Tests;
public static class TestFiles
{
    public static string Directory() { var path = Path.GetFullPath(Path.Combine("artifacts", "test-runs", Guid.NewGuid().ToString("N"))); System.IO.Directory.CreateDirectory(path); return path; }
    public static ProjectDocument Neutral() => new(JsonNode.Parse("""{"format":"ComicPanelEditorProject","version":2,"assetBase":".","canvas":{"width":160,"height":140,"exportScale":1},"panels":[{"id":"A","sourceImage":"art.png","polygon":[[20,20],[140,20],[140,120],[20,120]]}],"balloons":[]}""")!.AsObject(), null);
}
public sealed class ProjectCases : ITestSuite
{
    public IEnumerable<TestCase> Cases() => [
        new("Project.P10RoundTrip", () => {
            var source="artifacts/private-fixtures/P10SingleLine.json"; var hash=File.ReadAllBytes(source);
            var loaded=new ProjectStore().Load(source); var v=new ProjectView(loaded.Document);
            Assert.Equal(6,v.Panels.Count()); Assert.Equal(10,v.Balloons.Count()); Assert.Equal(1024,v.Width); Assert.Equal(1536,v.Height); Assert.Equal(2d,v.Scale);
            loaded.Document.Root["vendor"]=new JsonObject { ["note"]="keep" };
            var saved=new ProjectStore().Save(loaded.Document,Path.Combine(TestFiles.Directory(),"往返 test.json"),new());
            Assert.Equal("keep",saved.Document.Root["vendor"]!.Text("note"));
            Assert.True(JsonNode.DeepEquals(loaded.Document.Root["panels"],saved.Document.Root["panels"]));
            Assert.True(JsonNode.DeepEquals(loaded.Document.Root["balloons"],saved.Document.Root["balloons"]));
            Assert.True(hash.SequenceEqual(File.ReadAllBytes(source)),"Baseline source changed");
        }),
        new("Project.LegacyDefaults", () => {
            var doc=TestFiles.Neutral(); var v=new ProjectView(doc); Assert.Equal("Legacy",v.BorderMode); Assert.True(!v.SnapEnabled);
            for(var i=0;i<4;i++) Assert.True(ProjectView.EdgeVisible(v.Panels.First(),i));
        }),
        new("Project.ChangedDestinationConflicts", () => {
            var store=new ProjectStore(); var path=Path.Combine(TestFiles.Directory(),"conflict.json"); var saved=store.Save(TestFiles.Neutral(),path,new());
            File.AppendAllText(path," "); var before=File.ReadAllBytes(path);
            Assert.Equal("CONFLICT",Assert.Throws<EditorException>(()=>store.Save(saved.Document,path,new(true,saved.Sha256))).Code);
            Assert.True(before.SequenceEqual(File.ReadAllBytes(path)));
        }),
        new("Project.InterruptedSaveKeepsOriginal", () => {
            var path=Path.Combine(TestFiles.Directory(),"interrupt.json"); var saved=new ProjectStore().Save(TestFiles.Neutral(),path,new());
            var before=File.ReadAllBytes(path); var faulty=new ProjectStore(_=>throw new IOException("simulated before-commit fault"));
            Assert.Throws<IOException>(()=>faulty.Save(saved.Document,path,new(true,saved.Sha256)));
            Assert.True(before.SequenceEqual(File.ReadAllBytes(path))); Assert.Equal(1,System.IO.Directory.GetFiles(Path.GetDirectoryName(path)!).Length);
        }),
        new("Project.PreCommitRaceRejected", () => {
            var path=Path.Combine(TestFiles.Directory(),"race.json"); var saved=new ProjectStore().Save(TestFiles.Neutral(),path,new());
            var faulty=new ProjectStore(_=>File.AppendAllText(path," "));
            Assert.Equal("CONFLICT",Assert.Throws<EditorException>(()=>faulty.Save(saved.Document,path,new(true,saved.Sha256))).Code);
        }),
        new("Project.ReadOnlyDestinationFailsWithoutFallback", () => {
            var path=Path.Combine(TestFiles.Directory(),"readonly.json"); var saved=new ProjectStore().Save(TestFiles.Neutral(),path,new()); var before=File.ReadAllBytes(path);
            File.SetAttributes(path,FileAttributes.ReadOnly);
            try { Assert.Throws<UnauthorizedAccessException>(()=>new ProjectStore().Save(saved.Document,path,new(true,saved.Sha256))); Assert.True(before.SequenceEqual(File.ReadAllBytes(path))); }
            finally { File.SetAttributes(path,FileAttributes.Normal); }
        }),
        new("Project.RejectBadFormatAndVersion", () => {
            var doc=TestFiles.Neutral(); doc.Root["version"]=3;
            Assert.True(ProjectValidator.Validate(doc).Any(x=>x.Code=="VERSION"));
            Assert.Throws<EditorException>(()=>new ProjectStore().Save(doc,Path.Combine(TestFiles.Directory(),"bad.json"),new()));
        }),
        new("Project.SaveAsRebasesAssetsAndDecimalCulture", () => {
            var previous=CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
                var doc=TestFiles.Neutral(); var source=ProjectPaths.Resolve(doc,"art.png"); doc.Root["panels"]![0]!["imageTransform"]=new JsonObject{["scale"]=1.25};
                var saved=new ProjectStore().Save(doc,Path.Combine(TestFiles.Directory(),"中文 path.json"),new());
                Assert.Equal(source,ProjectPaths.Resolve(saved.Document,"art.png")); Assert.True(File.ReadAllText(saved.AbsolutePath).Contains("1.25"));
            } finally { CultureInfo.CurrentCulture=previous; }
        }),
        new("Project.ReadOnlyDirectoryFailsWithoutFallback", () => {
            var dir=new DirectoryInfo(TestFiles.Directory()); var original=dir.GetAccessControl();
            var secured=dir.GetAccessControl(); var sid=WindowsIdentity.GetCurrent().User!;
            secured.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.CreateFiles,AccessControlType.Deny)); dir.SetAccessControl(secured);
            try { Assert.Throws<UnauthorizedAccessException>(()=>new ProjectStore().Save(TestFiles.Neutral(),Path.Combine(dir.FullName,"no-fallback.json"),new())); Assert.Equal(0,dir.GetFiles().Length); }
            finally { dir.SetAccessControl(original); }
        }),
        new("Project.BackupAndSourcePreserved", () => {
            var path=Path.Combine(TestFiles.Directory(),"backup.json"); var saved=new ProjectStore().Save(TestFiles.Neutral(),path,new()); var before=File.ReadAllBytes(path);
            var clone=saved.Document.DeepClone(); clone.Root["projectName"]="changed"; new ProjectStore().Save(clone,path,new(true,saved.Sha256));
            var backup=System.IO.Directory.GetFiles(Path.GetDirectoryName(path)!,"*.backup-*").Single(); Assert.True(before.SequenceEqual(File.ReadAllBytes(backup)));
        }),
        new("Project.InvalidIdsClipAndPolygon", () => {
            var doc=TestFiles.Neutral(); doc.Root["panels"]!.AsArray().Add(doc.Root["panels"]![0]!.DeepClone());
            doc.Root["balloons"]!.AsArray().Add(JsonNode.Parse("""{"id":"B","sourceImage":"b.png","clipPanelId":"missing","transform":{"width":10,"height":10}}"""));
            Assert.True(ProjectValidator.Validate(doc).Any(x=>x.Code=="DUPLICATE_ID")); Assert.True(ProjectValidator.Validate(doc).Any(x=>x.Code=="CLIP_REFERENCE"));
            doc.Root["panels"]![0]!["polygon"]=JsonNode.Parse("[[0,0],[1,1],[2,2]]"); Assert.True(ProjectValidator.Validate(doc).Any(x=>x.Code=="POLYGON_DEGENERATE"));
        })
    ];
}
