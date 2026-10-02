namespace ComicEditor.Desktop;
public static class App
{
    [STAThread] public static void Main(string[] args)
    {
        ComicEditor.Rendering.LegacyGdiBootstrap.Initialize();
        var app=new System.Windows.Application();
        var document=new ComicEditor.Core.Project.ProjectDocument(System.Text.Json.Nodes.JsonNode.Parse("{\"format\":\"ComicPanelEditorProject\",\"version\":2,\"assetBase\":\".\",\"canvas\":{\"width\":1024,\"height\":1536},\"panels\":[],\"balloons\":[]}")!.AsObject(),null);
        var model=new ViewModels.EditorViewModel(document);
        ComicEditor.Core.AppState.StatePaths paths;
        try{paths=ComicEditor.Core.AppState.StatePaths.Resolve(AppContext.BaseDirectory,args.Contains("--portable-data"));}
        catch(Exception ex){System.Windows.MessageBox.Show("状态目录不可写；未自动改用其他位置。\n"+ex.Message,"启动未完成");return;}
        ComicEditor.Core.AppState.SettingsStore? settings=new(paths.Settings);System.Text.Json.Nodes.JsonObject savedSettings;
        try{savedSettings=settings.Read();}catch(Exception ex){settings=null;savedSettings=new();System.Windows.MessageBox.Show("设置无法读取，保留原文件，暂用默认设置。\n"+ex.Message,"设置读取失败");}
        try {var index=Array.IndexOf(args,"--project");if(index>=0&&index+1<args.Length)model.Open(args[index+1]);else if(savedSettings["lastProject"]?.GetValue<string>() is {} recent&&File.Exists(recent))model.Open(recent);}
        catch(Exception ex){System.Windows.MessageBox.Show(ex.Message,"工程未打开");}
        var window=new MainWindow(model);window.AttachState(paths,settings);if(savedSettings["windowState"]?.GetValue<string>()=="Maximized")window.WindowState=System.Windows.WindowState.Maximized;
        app.Run(window);
    }
}
