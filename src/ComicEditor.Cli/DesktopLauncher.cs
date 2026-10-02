using System.Diagnostics;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Cli;
public sealed record LaunchResult(int ProcessId,string State,bool Visible,string ProjectPath);
public static class DesktopLauncher
{
    public static LaunchResult Open(string project,string directory,bool portableData,Func<ProcessStartInfo,int>? starter=null)
    {
        var source=new ProjectStore().Load(project);directory=Path.GetFullPath(directory);var executable=Path.Combine(directory,"ComicEditor.exe");if(!File.Exists(executable))throw new EditorException("GUI_NOT_FOUND","GUI executable missing beside CLI; copy the whole release directory",3);
        // ShellExecute the exact executable (never cmd.exe) so GUI lifetime cannot
        // hold the CLI caller's redirected standard handles open.
        var start=new ProcessStartInfo(executable){UseShellExecute=true,CreateNoWindow=false,WorkingDirectory=directory};start.ArgumentList.Add("--project");start.ArgumentList.Add(source.AbsolutePath);if(portableData)start.ArgumentList.Add("--portable-data");
        try {var pid=starter is null?Process.Start(start)?.Id??throw new IOException("Process did not start"):starter(start);return new(pid,"LaunchRequested",false,source.AbsolutePath);}
        catch(System.ComponentModel.Win32Exception ex){throw new EditorException("LAUNCH",ex.Message,3);}
    }
}
