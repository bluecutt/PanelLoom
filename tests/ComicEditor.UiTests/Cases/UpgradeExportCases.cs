using System.Windows.Controls;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeExportCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeExport.DimensionsAndLegacyScale",()=>
        {
            var doc=CommandCases.Scene();doc.Root["canvas"]!["width"]=1024;doc.Root["canvas"]!["height"]=1536;doc.Root["canvas"]!["exportScale"]=1.75;
            var vm=new EditorViewModel(doc);var view=new InspectorView(vm,"page");
            Assert.True(view.Fields["page.canvas:exportScale"] is ComboBox,"Export scale must use shared presets, not manual input");
            var combo=(ComboBox)view.Fields["page.canvas:exportScale"];
            Assert.Equal("1.75",combo.SelectedValue?.ToString());Assert.True(combo.SelectedItem!.ToString()!.Contains("工程当前倍率"));
            Assert.Equal(1.75,vm.Document.Root["canvas"].Number("exportScale"));Assert.Equal(0L,vm.Revision);
            foreach(var text in new[]{"1024 × 1536","2048 × 3072","2560 × 3840","3072 × 4608"})Assert.True(combo.Items.Cast<object>().Any(item=>item.ToString()!.Contains(text)));
        });
        yield return new("UiUpgradeExport.DialogAlsoUsesPresets",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());var dialog=new ComicEditor.Desktop.Dialogs.ExportDialog(vm);
            var combos=((StackPanel)dialog.Content).Children.OfType<ComboBox>().ToArray();
            Assert.Equal(1,combos.Length);Assert.True(combos[0].Items.Count>=4);dialog.Close();
        });
        yield return new("UiUpgradeExport.ActualPngDimensionsAndBudgetRefusal",()=>
        {
            var doc=CommandCases.Scene();doc.Root["canvas"]!["width"]=1024;doc.Root["canvas"]!["height"]=1536;var vm=new EditorViewModel(doc);
            var outputs=new[]{(Scale:1d,W:1024,H:1536),(Scale:2d,W:2048,H:3072),(Scale:2.5d,W:2560,H:3840),(Scale:3d,W:3072,H:4608)};
            foreach(var expected in outputs)
            {
                var path=Path.Combine(TestFiles.Directory(),"page.png");var result=new ExportViewModel(vm).ExportAsync(path,new(expected.Scale),CancellationToken.None).GetAwaiter().GetResult();
                using var image=new System.Drawing.Bitmap(path);Assert.Equal(expected.W,image.Width);Assert.Equal(expected.H,image.Height);Assert.Equal(expected.W,result.Width);Assert.True(!vm.Busy);
            }
            var rejected=Path.Combine(TestFiles.Directory(),"refused.png");
            Assert.Equal("MEMORY_BUDGET",Assert.Throws<ComicEditor.Core.Validation.EditorException>(()=>new ExportViewModel(vm).ExportAsync(rejected,new(3,MemoryLimitBytes:1024),CancellationToken.None).GetAwaiter().GetResult()).Code);
            Assert.True(!File.Exists(rejected)&&!vm.Busy);
        });
        yield return new("UiUpgradeExport.CustomScaleWheelAndDraftProtection",()=>
        {
            var doc=CommandCases.Scene();doc.Root["canvas"]!["exportScale"]=1.75;var vm=new EditorViewModel(doc);var view=new InspectorView(vm,"page");
            var before=vm.Document.Root.ToJsonString();var picker=(ComboBox)view.Fields["page.canvas:exportScale"];
            picker.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(System.Windows.Input.Mouse.PrimaryDevice,Environment.TickCount,120){RoutedEvent=System.Windows.Input.Mouse.PreviewMouseWheelEvent});
            Assert.Equal(before,vm.Document.Root.ToJsonString());
            var path=Path.Combine(TestFiles.Directory(),"custom.png");var result=new ExportViewModel(vm).ExportAsync(path,new(1.75),CancellationToken.None).GetAwaiter().GetResult();Assert.Equal(280,result.Width);Assert.Equal(245,result.Height);
            ((TextBox)view.Fields["page.canvas:width"]).Text="200";
            var blocked=Path.Combine(TestFiles.Directory(),"blocked.png");Assert.Equal(6,Assert.Throws<ComicEditor.Core.Validation.EditorException>(()=>new ExportViewModel(vm).ExportAsync(blocked,new(1),CancellationToken.None).GetAwaiter().GetResult()).ExitCode);Assert.True(!File.Exists(blocked));vm.DiscardDrafts();
        });
    }
}
