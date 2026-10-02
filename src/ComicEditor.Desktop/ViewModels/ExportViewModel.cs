using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Rendering;
namespace ComicEditor.Desktop.ViewModels;
public sealed class ExportViewModel(EditorViewModel model)
{
    public async Task<RenderResult> ExportAsync(string path,RenderOptions options,CancellationToken token)
    {
        if(model.Busy||model.InputPending)throw new EditorException("BUSY","请先结束当前操作并处理未提交输入",6);var snapshot=model.Document;model.Busy=true;
        try{return await Task.Run(()=>new PageRenderer().Render(snapshot,path,options,token),token).ConfigureAwait(false);}finally{model.Busy=false;}
    }
}
