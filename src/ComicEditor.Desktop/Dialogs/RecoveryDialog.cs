using System.Windows;
using System.Windows.Controls;
using ComicEditor.Core.AppState;
namespace ComicEditor.Desktop.Dialogs;
public sealed class RecoveryDialog : Window
{
    public RecoverySnapshot? Selected { get; private set; }
    public RecoveryDialog(RecoveryStore store)
    {
        Title="独立恢复快照";Width=620;Height=430;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new DockPanel{Margin=new(18)};Content=panel;
        var explanation=new TextBlock{Text="恢复只载入工作状态，不覆盖原文件。原文件若已外改，保存时仍会核对哈希。",TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,12)};DockPanel.SetDock(explanation,Dock.Top);panel.Children.Add(explanation);
        var button=new Button{Content="载入选中快照",Padding=new(12,8,12,8),Margin=new(0,12,0,0)};DockPanel.SetDock(button,Dock.Bottom);panel.Children.Add(button);
        var snapshots=new List<RecoverySnapshot>();foreach(var path in store.List()){try{snapshots.Add(store.Read(path));}catch{ /* Corrupt snapshots are retained on disk, never auto-deleted. */ }}
        var list=new ListBox{ItemsSource=snapshots.Select(s=>$"{s.CreatedUtc.LocalDateTime:g} · {Path.GetFileName(s.Document.SourcePath)??"未命名工程"} · revision {s.Revision}").ToArray()};panel.Children.Add(list);
        button.Click+=(_,_)=>{if(list.SelectedIndex>=0){Selected=snapshots[list.SelectedIndex];DialogResult=true;}};
    }
}
