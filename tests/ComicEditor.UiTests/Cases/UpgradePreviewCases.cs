using System.Collections.Concurrent;
using System.Diagnostics;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradePreviewCases : ITestSuite
{
    private static (object Coordinator,Type Type,object Epoch) Create(Func<int,CancellationToken,Task<int>> render,Action<int> publish,Action<int> release)
    {
        var assembly=typeof(CanvasView).Assembly;var open=assembly.GetType("ComicEditor.Desktop.Canvas.InteractivePreviewCoordinator`2");Assert.True(open is not null,"Continuous sampler required");var type=open!.MakeGenericType(typeof(int),typeof(int));var epochType=assembly.GetType("ComicEditor.Desktop.Canvas.PreviewEpoch")!;
        return (Activator.CreateInstance(type,render,publish,release)!,type,Activator.CreateInstance(epochType,1L,1L,1L)!);
    }
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradePreview.ContinuousInputPublishes",()=>{
            var queue=new ConcurrentQueue<int>();var active=0;var maximum=0;var c=Create(async(n,t)=>{var a=Interlocked.Increment(ref active);maximum=Math.Max(maximum,a);try{await Task.Delay(25,t);return n;}finally{Interlocked.Decrement(ref active);}},queue.Enqueue,_=>{});
            using var owned=(IDisposable)c.Coordinator;var submit=c.Type.GetMethod("Submit")!;var tick=c.Type.GetMethod("Tick")!;
            for(var i=0;i<60;i++){submit.Invoke(c.Coordinator,[c.Epoch,i,false]);tick.Invoke(c.Coordinator,[]);Thread.Sleep(10);}
            Assert.True(queue.Count>=2);submit.Invoke(c.Coordinator,[c.Epoch,60,true]);for(var i=0;i<20;i++){tick.Invoke(c.Coordinator,[]);Thread.Sleep(10);}Assert.Equal(60,queue.Last());Assert.True(maximum<=1);
        });
        yield return new("UiUpgradePreview.InvalidEpochAndFailureRecovery",()=>{
            var queue=new ConcurrentQueue<int>();var held=new TaskCompletionSource<int>();var released=0;var c=Create((n,t)=>n==1?held.Task:n==2?Task.FromException<int>(new IOException("test")):Task.FromResult(n),queue.Enqueue,_=>Interlocked.Increment(ref released));using var owned=(IDisposable)c.Coordinator;
            c.Type.GetMethod("Submit")!.Invoke(c.Coordinator,[c.Epoch,1,false]);c.Type.GetMethod("Tick")!.Invoke(c.Coordinator,[]);Thread.Sleep(10);
            var next=Activator.CreateInstance(c.Epoch.GetType(),2L,2L,2L)!;c.Type.GetMethod("Invalidate")!.Invoke(c.Coordinator,[next]);held.SetResult(1);Thread.Sleep(40);Assert.Equal(0,queue.Count);Assert.Equal(1,released);
            c.Type.GetMethod("Submit")!.Invoke(c.Coordinator,[next,2,false]);c.Type.GetMethod("Tick")!.Invoke(c.Coordinator,[]);Thread.Sleep(30);c.Type.GetMethod("Submit")!.Invoke(c.Coordinator,[next,3,true]);for(var i=0;i<10;i++){c.Type.GetMethod("Tick")!.Invoke(c.Coordinator,[]);Thread.Sleep(10);}Assert.Equal(3,queue.Last());
        });
        yield return new("UiUpgradePreview.ActualWindowPublishesDuringGesture",()=>{
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var window=new ComicEditor.Desktop.MainWindow(vm);window.Show();window.UpdateLayout();
            try{var count=window.GetType().GetProperty("PreviewPublishedFrames");Assert.True(count is not null);var before=(long)count!.GetValue(window)!;
                for(var i=0;i<60;i++){window.PreviewCanvas.HandleWheel(i%2==0?12:-12,new(70,70),System.Windows.Input.ModifierKeys.None);UpgradeGestureCases.Pump(10);}
                Assert.True((long)count.GetValue(window)!-before>=2);Assert.True(vm.Busy);window.PreviewCanvas.Cancel();UpgradeGestureCases.Pump(100);Assert.True(!vm.Busy);Assert.Equal(0L,vm.Revision);
            }finally{window.PreviewCanvas.Cancel();vm.DiscardDrafts();window.Close();}
        });
        yield return new("UiUpgradePreview.PrivateP10FeedbackAndFinalPixels",()=>{
            var doc=new ComicEditor.Core.Project.ProjectStore().Load("artifacts/private-fixtures/P10SingleLine.json").Document;var vm=new EditorViewModel(doc);vm.Select("panel","U01");var window=new ComicEditor.Desktop.MainWindow(vm);window.Show();window.UpdateLayout();
            try{UpgradeGestureCases.Pump(2500);var before=window.PreviewPublishedFrames;var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var clock=Stopwatch.StartNew();var stamps=new List<double>();var count=before;
                for(var i=0;i<60;i++){window.PreviewCanvas.HandleWheel(i%2==0?12:-12,new(70,70),System.Windows.Input.ModifierKeys.None);UpgradeGestureCases.Pump(10);if(window.PreviewPublishedFrames>count){count=window.PreviewPublishedFrames;stamps.Add(clock.Elapsed.TotalMilliseconds);}}
                Assert.True(count>before,"Actual P10 must publish during continuous gesture");Assert.True(((System.Windows.Media.Imaging.BitmapSource)window.PreviewCanvas.PreviewImage!).PixelWidth<=600,"Interactive preview should be bounded separately from final original-source output");window.PreviewCanvas.Cancel();UpgradeGestureCases.Pump(2000);
                using var strict=new ComicEditor.Rendering.PageRenderer().RenderBitmap(doc,new(1));var reference=BitmapSourceConverter.Freeze(strict.Pixels);var image=(System.Windows.Media.Imaging.BitmapSource)window.PreviewCanvas.PreviewImage!;var stride=(reference.PixelWidth*3+3)&~3;var a=new byte[stride*reference.PixelHeight];var b=new byte[a.Length];reference.CopyPixels(a,stride,0);image.CopyPixels(b,stride,0);Assert.True(a.SequenceEqual(b),"Esc terminal frame must equal strict original rendering");
                var export=Stopwatch.StartNew();using var final=new ComicEditor.Rendering.PageRenderer().RenderBitmap(doc,new(2.5));export.Stop();process.Refresh();
                var gaps=stamps.Zip(stamps.Skip(1),(x,y)=>y-x).ToArray();Console.WriteLine($"P10_PROBE first_ms={stamps.First():0} updates={count-before} longest_gap_ms={(gaps.Length==0?0:gaps.Max()):0} input_ms={clock.ElapsedMilliseconds} cpu_ms={(process.TotalProcessorTime-cpu).TotalMilliseconds:0} private_mib={process.PrivateMemorySize64/1048576d:0.0} strict_2_5_ms={export.ElapsedMilliseconds}");
            }finally{window.PreviewCanvas.Cancel();vm.DiscardDrafts();window.Close();}
        });
    }
}
