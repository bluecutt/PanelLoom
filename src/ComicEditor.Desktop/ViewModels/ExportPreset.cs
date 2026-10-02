using System.Globalization;
using ComicEditor.Core.Project;
namespace ComicEditor.Desktop.ViewModels;
public sealed record ExportPreset(double Scale,int Width,int Height,string Label)
{
    public override string ToString()=>Label;
    public static IReadOnlyList<ExportPreset> For(ProjectDocument document)
    {
        var view=new ProjectView(document);var presets=new List<ExportPreset>();
        foreach(var scale in new[]{1d,2d,2.5d,3d})presets.Add(Create(scale,false));
        if(!presets.Any(p=>p.Scale==view.Scale))presets.Add(Create(view.Scale,true));
        return presets;
        ExportPreset Create(double scale,bool current)
        {
            var width=checked((int)Math.Round(view.Width*scale));var height=checked((int)Math.Round(view.Height*scale));
            var purpose=current?"工程当前倍率":scale==1?"基础预览":scale==2?"较大成稿":scale==2.5?"常用高清":"更大输出";
            var warning=scale>1?"；需核对原素材像素":"";
            return new(scale,width,height,$"{scale.ToString(current?"R":"0.###",CultureInfo.InvariantCulture)}× · {width} × {height} · {purpose}{warning}");
        }
    }
}
