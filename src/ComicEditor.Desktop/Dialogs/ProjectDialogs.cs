using ComicEditor.Desktop.ViewModels;
namespace ComicEditor.Desktop.Dialogs;
public static class ProjectDialogs
{
    public static T Modal<T>(EditorViewModel model,Func<T> show) {model.ModalDepth++;try{return show();}finally{model.ModalDepth--;}}
}
