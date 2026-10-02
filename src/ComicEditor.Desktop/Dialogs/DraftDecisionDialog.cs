using System.Windows;
using ComicEditor.Desktop.Editing;
namespace ComicEditor.Desktop.Dialogs;
public static class DraftDecisionDialog
{
    public static DraftDecision Show(Window owner) => MessageBox.Show(owner,
        "还有尚未应用的属性输入。\n是：应用这些输入后继续；否：放弃输入；取消：留在当前对象。",
        "处理未提交输入", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) switch
    { MessageBoxResult.Yes => DraftDecision.Apply, MessageBoxResult.No => DraftDecision.Discard, _ => DraftDecision.Cancel };
}
