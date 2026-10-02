using ComicEditor.Core.AppState;
using ComicEditor.Core.Project;
namespace ComicEditor.Tests;
public sealed class RecoveryCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("Recovery.CrashSnapshotDoesNotOverwrite",()=>{var doc=CommandCases.Scene();var dir=Path.GetDirectoryName(doc.SourcePath)!;var saved=new ProjectStore().Save(doc,Path.Combine(dir,"formal.json"),new());var edited=doc.DeepClone();edited.Root["balloons"]![0]!["transform"]!["x"]=999;var store=new RecoveryStore(Path.Combine(dir,"recovery"));var snapshot=store.Write(Guid.NewGuid().ToString(),edited,saved.Sha256,7);Assert.Equal(saved.Sha256,AtomicFile.Hash(saved.AbsolutePath));var restored=store.Read(snapshot.Path);Assert.Equal(7L,restored.Revision);Assert.Equal(999d,restored.Document.Root["balloons"]![0]!["transform"].Number("x"));}),
        new("Recovery.InvalidIdCannotEscape",()=>{var store=new RecoveryStore(TestFiles.Directory());Assert.Throws<ComicEditor.Core.Validation.EditorException>(()=>store.Write("../formal",CommandCases.Scene(),null,0));}),
        new("Recovery.PortableDoesNotFallback",()=>{var dir=TestFiles.Directory();File.WriteAllText(Path.Combine(dir,"Data"),"blocked");Assert.Throws<IOException>(()=>StatePaths.Resolve(dir,true,Path.Combine(dir,"unexpected")));Assert.True(!Directory.Exists(Path.Combine(dir,"unexpected")));}),
        new("Recovery.ReadOnlyDataDirectory",()=>{var dir=TestFiles.Directory();var data=new DirectoryInfo(Path.Combine(dir,"Data"));data.Create();var original=data.GetAccessControl();var secured=data.GetAccessControl();secured.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(System.Security.Principal.WindowsIdentity.GetCurrent().User!,System.Security.AccessControl.FileSystemRights.CreateFiles,System.Security.AccessControl.AccessControlType.Deny));data.SetAccessControl(secured);try{Assert.Throws<UnauthorizedAccessException>(()=>StatePaths.Resolve(dir,true));Assert.Equal(0,data.GetFiles().Length);}finally{data.SetAccessControl(original);}}),
        new("Recovery.UserAndPortableAreExplicit",()=>{var dir=TestFiles.Directory();var user=StatePaths.Resolve(dir,false,Path.Combine(dir,"user"));var portable=StatePaths.Resolve(dir,true);Assert.True(user.Root!=portable.Root);Assert.True(Directory.Exists(portable.Sessions));})
    ];
}
