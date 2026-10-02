using ComicEditor.Core.Project;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeAssetEpochCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeAssetEpoch.NoChangeReplaceInvalidates",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());var property=typeof(EditorViewModel).GetProperty("AssetEpoch");Assert.True(property is not null);var before=(long)property!.GetValue(vm)!;
            vm.Execute([CommandCases.Op("asset.replace","A","{\"kind\":\"panel\",\"sourceImage\":\"art.png\",\"fitPolicy\":\"preserve\"}")]);Assert.True((long)property.GetValue(vm)!>before);
        });
        yield return new("UiUpgradeAssetEpoch.FrozenPixelsOutliveBitmap",()=>{
            System.Windows.Media.Imaging.BitmapSource frozen;using(var b=new System.Drawing.Bitmap(20,10)){using(var g=System.Drawing.Graphics.FromImage(b))g.Clear(System.Drawing.Color.Red);frozen=BitmapSourceConverter.Freeze(b);}Assert.True(frozen.IsFrozen);var pixels=new byte[20*10*3];frozen.CopyPixels(pixels,60,0);Assert.Equal((byte)255,pixels[2]);
        });
    }
}
