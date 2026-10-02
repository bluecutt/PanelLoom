using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeGestureCases : ITestSuite
{
    public static void Pump(int milliseconds)
    {
        var frame=new DispatcherFrame();var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(milliseconds)};
        timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);
    }
    private static void Wheel(CanvasView canvas,int delta)=>canvas.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,delta){RoutedEvent=Mouse.MouseWheelEvent});
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeGesture.WheelRoutesAndUndo",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var canvas=new CanvasView(vm);
            var before=vm.Document.Root.ToJsonString();var polygon=vm.Document.Root["panels"]![0]!["polygon"]!.DeepClone();
            Wheel(canvas,120);Wheel(canvas,120);Wheel(canvas,120);
            Assert.True(Math.Abs(canvas.PreviewDocument.Root["panels"]![0]!["imageTransform"].Number("scale")-1.66375)<0.000001,"Canvas wheel must resize selected image, not just view");
            Assert.True(JsonNode.DeepEquals(polygon,canvas.PreviewDocument.Root["panels"]![0]!["polygon"]));
            Assert.True(vm.Busy&&vm.Revision==0);Pump(310);
            Assert.Equal(1L,vm.Revision);Assert.True(!vm.Busy);
            vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());
            canvas.Cancel();
        });
        yield return new("UiUpgradeGesture.BalloonWheelKeepsCenterAndEscapeRestores",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("balloon","X");var canvas=new CanvasView(vm);
            var before=vm.Document.Root.ToJsonString();Wheel(canvas,120);
            var transform=canvas.PreviewDocument.Root["balloons"]![0]!["transform"];
            Assert.True(Math.Abs(transform.Number("width")-44)<0.000001);
            Assert.True(Math.Abs(transform.Number("height")-44)<0.000001);
            Assert.True(Math.Abs(transform.Number("x")+transform.Number("width")/2-50)<0.000001);
            canvas.Cancel();Pump(310);Assert.Equal(before,vm.Document.Root.ToJsonString());Assert.True(!vm.Busy&&vm.Revision==0);
        });
        yield return new("UiUpgradeGesture.CtrlWheelOnlyChangesView",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var canvas=new CanvasView(vm);var before=vm.Document.Root.ToJsonString();
            var method=typeof(CanvasView).GetMethod("HandleWheel");
            Assert.True(method is not null,"Wheel modifier routing entry is required");
            method!.Invoke(canvas,[120,new ComicEditor.Core.Geometry.Point(50,50),ModifierKeys.Control]);
            Assert.True(canvas.View.Zoom>1);Assert.Equal(before,vm.Document.Root.ToJsonString());Assert.True(!vm.Busy);canvas.Cancel();
        });
        yield return new("UiUpgradeGesture.FrameOnlyMovePreservesImageAndNeighbor",()=>
        {
            Assert.True(Enum.TryParse<DragKind>("FrameOnly",out var kind),"Frame-only drag must be distinct from whole-panel move");
            var vm=new EditorViewModel(CommandCases.Scene());var before=vm.Document;
            var input=new InputRouter(vm);input.Begin(new("panel","A",kind),new(20,20));input.Update(new(25,28));input.Commit();
            Assert.Equal(25d,vm.Document.Root["panels"]![0]!["polygon"]![0]![0]!.GetValue<double>());
            Assert.True(JsonNode.DeepEquals(before.Root["panels"]![0]!["imageTransform"],vm.Document.Root["panels"]![0]!["imageTransform"]));
            Assert.True(JsonNode.DeepEquals(before.Root["panels"]![1],vm.Document.Root["panels"]![1]));
            vm.Undo();Assert.Equal(before.Root.ToJsonString(),vm.Document.Root.ToJsonString());
        });
        yield return new("UiUpgradeGesture.ViewArrowDoesNotChangeProject",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var window=new ComicEditor.Desktop.MainWindow(vm);window.Show();window.UpdateLayout();
            var canvas=window.PreviewCanvas;canvas.Mode=EditMode.View;canvas.Focus();var before=vm.Document.Root.ToJsonString();
            try{canvas.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(canvas)!,Environment.TickCount,Key.Right){RoutedEvent=Keyboard.KeyDownEvent});Assert.Equal(before,vm.Document.Root.ToJsonString());}
            finally{vm.DiscardDrafts();vm.Undo();window.Close();}
        });
        yield return new("UiUpgradeGesture.InactiveModesAndLockedWheelAreSafe",()=>
        {
            foreach(var mode in new[]{EditMode.View,EditMode.Panel,EditMode.Vertex,EditMode.FrameOnly,EditMode.Edge})
            {
                var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var canvas=new CanvasView(vm);canvas.Mode=mode;var before=vm.Document.Root.ToJsonString();
                Wheel(canvas,120);Assert.True(canvas.View.Zoom>1);Assert.Equal(before,vm.Document.Root.ToJsonString());Assert.True(!vm.Busy);canvas.Cancel();
            }
            var doc=CommandCases.Scene();doc.Root["panels"]![0]!["locked"]=true;
            var locked=new EditorViewModel(doc);locked.Select("panel","A");var view=new CanvasView(locked);var error="";view.Error+=message=>error=message;
            var original=locked.Document.Root.ToJsonString();Wheel(view,120);Pump(310);
            Assert.True(error.Length>0&&!locked.Busy);Assert.Equal(original,locked.Document.Root.ToJsonString());view.Cancel();
        });
        yield return new("UiUpgradeGesture.IdleStartsNewUndoAndReturnOriginIsNoop",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");var canvas=new CanvasView(vm);var before=vm.Document.Root.ToJsonString();
            Wheel(canvas,120);Pump(310);Assert.Equal(1L,vm.Revision);
            Wheel(canvas,120);Pump(310);Assert.Equal(2L,vm.Revision);
            vm.Undo();Assert.True(Math.Abs(vm.Document.Root["panels"]![0]!["imageTransform"].Number("scale")-1.375)<0.000001);
            vm.Undo();Assert.Equal(before,vm.Document.Root.ToJsonString());var revision=vm.Revision;
            Wheel(canvas,120);Wheel(canvas,-120);Pump(310);
            Assert.Equal(revision,vm.Revision);Assert.Equal(before,vm.Document.Root.ToJsonString());canvas.Cancel();
        });
    }
}
