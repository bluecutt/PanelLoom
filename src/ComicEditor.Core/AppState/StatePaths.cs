namespace ComicEditor.Core.AppState;
public sealed record StatePaths(string Root)
{
    public string Recovery=>Path.Combine(Root,"Recovery");
    public string Sessions=>Path.Combine(Root,"Sessions");
    public string Settings=>Path.Combine(Root,"settings.json");
    public string Logs=>Path.Combine(Root,"Logs");
    public static string RootFor(string appDirectory,bool portable,string? userDirectory=null)=>Path.GetFullPath(portable?Path.Combine(appDirectory,"Data"):Path.Combine(userDirectory??Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ComicEditor.Native","0.1"));
    public static StatePaths Resolve(string appDirectory,bool portable,string? userDirectory=null)
    {
        var root=RootFor(appDirectory,portable,userDirectory);
        root=Path.GetFullPath(root);Directory.CreateDirectory(root);var probe=Path.Combine(root,".write-probe-"+Guid.NewGuid().ToString("N"));
        try {using var stream=new FileStream(probe,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.WriteThrough);stream.WriteByte(1);stream.Flush(true);}finally{if(File.Exists(probe))File.Delete(probe);}
        var paths=new StatePaths(root);Directory.CreateDirectory(paths.Recovery);Directory.CreateDirectory(paths.Sessions);Directory.CreateDirectory(paths.Logs);return paths;
    }
}
