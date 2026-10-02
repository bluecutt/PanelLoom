using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Rendering;
using ComicEditor.Desktop.Controls;
namespace ComicEditor.Desktop.Dialogs;
public sealed class ExportDialog : Window
{
    public ExportDialog(EditorViewModel model)
    {
        Title="高清导出"; Width=520; SizeToContent=SizeToContent.Height; WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var stack=new StackPanel{Margin=new(22)}; Content=stack;
        stack.Children.Add(new TextBlock{Text="从完整原图渲染 PNG",FontSize=19,FontWeight=FontWeights.SemiBold});
        stack.Children.Add(new TextBlock{Text="倍率影响实际输出尺寸；预览缩放不影响成稿。",TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,14)});
        var scale=new ComboBox{ItemsSource=ExportPreset.For(model.Document),DisplayMemberPath="Label",SelectedValuePath="Scale",SelectedValue=new ProjectView(model.Document).Scale,MinHeight=34,Margin=new(0,4,0,10)}; stack.Children.Add(scale);
        var quality=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,12)}; stack.Children.Add(quality);
        void UpdateQuality()
        {
            if(scale.SelectedItem is not ExportPreset preset)return;
            try{var enlarged=QualityAssessment.Assess(model.Document,preset.Scale).Count(a=>a.Status=="upsampled");quality.Text=$"输出 {preset.Width} × {preset.Height}；{enlarged} 个素材需要放大。\n更大文件不保证增加超出原图的真实细节。";}
            catch(Exception ex){quality.Text=UiLabels.Message(ex);}
        }
        scale.PreviewMouseWheel+=(_,e)=>e.Handled=true;scale.SelectionChanged+=(_,_)=>UpdateQuality();UpdateQuality();
        var button=new Button{Content="选择位置并导出",Padding=new(12,8,12,8)}; stack.Children.Add(button);
        CancellationTokenSource? active=null;string? output=null;
        var cancel=new Button{Content="取消导出",IsEnabled=false,Margin=new(0,8,0,0),Padding=new(12,6,12,6)};stack.Children.Add(cancel);cancel.Click+=(_,_)=>active?.Cancel();
        var folder=new Button{Content="打开输出文件夹",IsEnabled=false,Margin=new(0,8,0,0),Padding=new(12,6,12,6)};stack.Children.Add(folder);folder.Click+=(_,_)=>{if(output is not null)System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetDirectoryName(output)!){UseShellExecute=true});};
        Closing+=(_,e)=>{if(active is not null){active.Cancel();e.Cancel=true;quality.Text="正在取消，完成后可关闭。";}};
        button.Click+=async (_,_)=>{
            try {
                var multiplier=(scale.SelectedItem as ExportPreset)?.Scale??throw new EditorException("ARGS","请选择导出倍率"); var view=new ProjectView(model.Document);
                quality.Text=$"预计 {view.Width*multiplier:0} × {view.Height*multiplier:0}；"+QualityAssessment.Assess(model.Document,multiplier).Count(a=>a.Status=="upsampled")+" 个素材需要放大。";
                var d=new Microsoft.Win32.SaveFileDialog{Filter="PNG|*.png",FileName="page.png",OverwritePrompt=true}; if(d.ShowDialog(this)!=true)return;
                active=new();button.IsEnabled=false;cancel.IsEnabled=true;quality.Text="正在读取完整原图并渲染……";
                var r=await new ExportViewModel(model).ExportAsync(d.FileName,new(multiplier,Overwrite:File.Exists(d.FileName),ExpectedDestinationHash:File.Exists(d.FileName)?AtomicFile.Hash(d.FileName):null),active.Token);output=r.Path;folder.IsEnabled=true;quality.Text=$"已导出 {r.Width} × {r.Height}\n{r.Path}";
            }catch(OperationCanceledException){quality.Text="导出已取消，未提交半截 PNG。";}
            catch(Exception ex){quality.Text=UiLabels.Message(ex);}
            finally{active?.Dispose();active=null;button.IsEnabled=true;cancel.IsEnabled=false;}
        };
    }
}
