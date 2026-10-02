using System.Drawing;
using ComicEditor.Rendering;
namespace ComicEditor.Tests;
public sealed class UpgradePreviewCacheCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UpgradePreviewCache.PreviewPixelsAndStrictIsolation",()=>{
            var doc=CommandCases.Scene();using var cache=new PreviewAssetCache(65536);using var preview=new PageRenderer().RenderPreview(doc,new(),cache,0);using var strict=new PageRenderer().RenderBitmap(doc,new());Assert.Equal(0,Pixels.Differences(strict.Pixels,preview.Pixels));
            var path=ComicEditor.Core.Project.ProjectPaths.Resolve(doc,"art.png");using(var b=new Bitmap(20,20)){using(var g=Graphics.FromImage(b))g.Clear(Color.Green);b.Save(path);}
            cache.Invalidate(path);using var next=new PageRenderer().RenderPreview(doc,new(),cache,1);using var fresh=new PageRenderer().RenderBitmap(doc,new());Assert.Equal(0,Pixels.Differences(fresh.Pixels,next.Pixels));
        });
        yield return new("UpgradePreviewCache.InvalidBudgetAndActiveDispose",()=>{
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PreviewAssetCache(0));var path=Path.Combine(TestFiles.Directory(),"lease.png");using(var b=new Bitmap(8,8))b.Save(path);
            var cache=new PreviewAssetCache(256);var lease=cache.Acquire(path,0);cache.Dispose();Assert.Equal(8,lease.Pixels.Width);lease.Dispose();Assert.Equal(0L,cache.ResidentBytes);
        });
        yield return new("UpgradePreviewCache.BudgetLeaseAndReplacement",()=>{
            var type=typeof(PageRenderer).Assembly.GetType("ComicEditor.Rendering.PreviewAssetCache");Assert.True(type is not null,"Bounded preview cache required");
            var cache=Activator.CreateInstance(type!,65536L)!;var acquire=type!.GetMethod("Acquire")!;var dir=TestFiles.Directory();
            string Art(string name,Color color,int size=100){var path=Path.Combine(dir,name+".png");using var b=new Bitmap(size,size);using(var g=Graphics.FromImage(b))g.Clear(color);b.Save(path);return path;}
            var a=Art("a",Color.Red);var b=Art("b",Color.Blue);var lease=(IDisposable)acquire.Invoke(cache,[a,1L])!;var pixels=(Bitmap)lease.GetType().GetProperty("Pixels")!.GetValue(lease)!;
            using(var same=(IDisposable)acquire.Invoke(cache,[a,1L])!)Assert.True(ReferenceEquals(pixels,same.GetType().GetProperty("Pixels")!.GetValue(same)));
            using(var second=(IDisposable)acquire.Invoke(cache,[b,1L])!)Assert.Equal(Color.Red.ToArgb(),pixels.GetPixel(1,1).ToArgb());
            lease.Dispose();Assert.True((long)type.GetProperty("ResidentBytes")!.GetValue(cache)!<=65536);
            Art("a",Color.Green);using(var updated=(IDisposable)acquire.Invoke(cache,[a,2L])!)Assert.Equal(Color.Green.ToArgb(),((Bitmap)updated.GetType().GetProperty("Pixels")!.GetValue(updated)!).GetPixel(1,1).ToArgb());
            ((IDisposable)cache).Dispose();Assert.Equal(0L,(long)type.GetProperty("ResidentBytes")!.GetValue(cache)!);
        });
        yield return new("UpgradePreviewCache.OversizedAndMissingDoNotReuse",()=>{
            var type=typeof(PageRenderer).Assembly.GetType("ComicEditor.Rendering.PreviewAssetCache");Assert.True(type is not null);var cache=Activator.CreateInstance(type!,64L)!;
            var path=Path.Combine(TestFiles.Directory(),"large.png");using(var b=new Bitmap(100,100))b.Save(path);
            using(var lease=(IDisposable)type!.GetMethod("Acquire")!.Invoke(cache,[path,0L])!)Assert.Equal(0L,(long)type.GetProperty("ResidentBytes")!.GetValue(cache)!);
            File.Move(path,path+".old");try{type.GetMethod("Acquire")!.Invoke(cache,[path,0L]);throw new Exception("Missing must fail");}catch(System.Reflection.TargetInvocationException){}
            ((IDisposable)cache).Dispose();
        });
    }
}
