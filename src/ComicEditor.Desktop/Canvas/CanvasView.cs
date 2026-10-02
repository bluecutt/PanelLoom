using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.Controls;
using P=ComicEditor.Core.Geometry.Point;
namespace ComicEditor.Desktop.Canvas;
public sealed class CanvasView : FrameworkElement
{
    private readonly EditorViewModel model;
    private readonly InputRouter input;
    private readonly WheelGesture wheel;
    private readonly System.Windows.Threading.DispatcherTimer wheelIdle;
    private ImageSource? image;
    private P? panStart;
    private string? hoverPanelId;
    public CanvasTools Tools {get;}
    public Func<bool>? ConfirmDelete {get;set;}
    public ViewTransform View { get; }=new();
    public long InteractionEpoch {get;private set;}
    public ImageSource? PreviewImage=>image;
    public EditMode Mode { get=>model.Selection.Current.Tool; set{FinishWheel();model.SetTool(value);} }
    public bool MoveGroup { get=>input.MoveGroup;set=>input.MoveGroup=value; }
    public ProjectDocument PreviewDocument=>input.Preview??wheel.Preview??model.Drafts.Preview(model.Document);
    public event Action? PreviewChanged;
    public event Action<string>? Error;
    public CanvasView(EditorViewModel vm)
    {
        model=vm;input=new(vm);wheel=new(vm);Tools=new(vm);Tools.Changed+=()=>InvalidateVisual();wheelIdle=new(){Interval=TimeSpan.FromMilliseconds(250)};wheelIdle.Tick+=(_,_)=>FinishWheel();Focusable=true;Loaded+=(_,_)=>Fit();LostMouseCapture+=(_,_)=>Cancel();LostKeyboardFocus+=(_,_)=>Cancel();
    }
    public void SetPreview(ImageSource source) {image=source;InvalidateVisual();}
    public void Fit() {var v=new ProjectView(model.Document);View.Fit(v.Width,v.Height,ActualWidth,ActualHeight);InvalidateVisual();}
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,ActualWidth,ActualHeight));dc.PushClip(new RectangleGeometry(new Rect(0,0,ActualWidth,ActualHeight)));
        var v=new ProjectView(PreviewDocument);dc.PushTransform(new MatrixTransform(new Matrix(View.Zoom,0,0,View.Zoom,View.X,View.Y)));
        if(image is not null)dc.DrawImage(image,new Rect(0,0,v.Width,v.Height));
        if(Tools.PickingBalloonId is not null&&hoverPanelId is {} hover)
        {
            var panel=v.Panels.FirstOrDefault(p=>p.Text("id")==hover);
            if(panel is not null){var outline=PolygonOperations.Read(panel["polygon"]!);var highlight=new Pen(Brushes.DeepSkyBlue,3/View.Zoom);for(var i=0;i<outline.Length;i++)dc.DrawLine(highlight,W(outline[i]),W(outline[(i+1)%outline.Length]));}
        }
        var selected=model.SelectedKind=="panel"?v.Panels.FirstOrDefault(p=>p.Text("id")==model.SelectedId):v.Balloons.FirstOrDefault(p=>p.Text("id")==model.SelectedId);
        if(selected is not null)
        {
            var points=model.SelectedKind=="panel"?PolygonOperations.Read(selected["polygon"]!):HandleHitTest.BalloonCorners(selected);
            var pen=new Pen(Brushes.Teal,1.5/View.Zoom){DashStyle=DashStyles.Dash};
            for(var i=0;i<points.Length;i++)dc.DrawLine(pen,W(points[i]),W(points[(i+1)%points.Length]));
            if(Mode==EditMode.Edge&&model.Selection.Current.EdgeIndex is {} edge&&edge<points.Length)
            {
                dc.DrawLine(new Pen(Brushes.DeepSkyBlue,4/View.Zoom),W(points[edge]),W(points[(edge+1)%points.Length]));
                var candidate=EdgeSnap.SnapEdgeDetailed(selected,v.Panels,edge,PreviewDocument.Root["edgeSnap"].Number("tolerance",8));
                var neighbor=v.Panels.FirstOrDefault(p=>p.Text("id")==candidate.TargetPanelId);
                if(neighbor is not null&&candidate.TargetEdgeIndex is {} other){var q=PolygonOperations.Read(neighbor["polygon"]!);dc.DrawLine(new Pen(Brushes.Orange,3/View.Zoom),W(q[other]),W(q[(other+1)%q.Length]));}
            }
            if(Mode==EditMode.Vertex&&model.SelectedKind=="panel")foreach(var point in points)Handle(point);
            else if(Mode is EditMode.Panel or EditMode.FrameOnly&&model.SelectedKind=="panel")foreach(var point in HandleHitTest.Corners(PolygonOperations.Bounds(points)))Handle(point);
            else if(Mode==EditMode.Balloon&&model.SelectedKind=="balloon") {foreach(var point in points)Handle(point);Handle(HandleHitTest.RotationHandle(selected,24/View.Zoom));}
            void Handle(P p)=>dc.DrawEllipse(Brushes.White,new Pen(Brushes.Teal,1.5/View.Zoom),W(p),5/View.Zoom,5/View.Zoom);
        }
        dc.Pop();dc.Pop();
    }
    private static System.Windows.Point W(P p)=>new(p.X,p.Y);
    private P Screen(MouseEventArgs e) {var p=e.GetPosition(this);return new(p.X,p.Y);}
    protected override void OnMouseWheel(MouseWheelEventArgs e) {HandleWheel(e.Delta,Screen(e),Keyboard.Modifiers);e.Handled=true;}
    public void HandleWheel(int delta,P cursor,ModifierKeys modifiers)
    {
        try
        {
            if((modifiers&ModifierKeys.Control)!=0||Mode is EditMode.View or EditMode.Panel or EditMode.Vertex or EditMode.FrameOnly or EditMode.Edge)
            {FinishWheel();View.Wheel(delta,cursor);InvalidateVisual();return;}
            if(model.SelectedId is null)return;
            if(wheel.Active&&!wheel.Matches(model.Selection.Current))FinishWheel();
            if(!wheel.Active){wheel.Begin(model.Selection.Current,model.Document);InteractionEpoch++;}
            wheel.Update(delta);wheelIdle.Stop();wheelIdle.Start();PreviewChanged?.Invoke();InvalidateVisual();
        }
        catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}
    }
    public void FinishWheel()
    {
        wheelIdle.Stop();if(!wheel.Active)return;
        try{wheel.Commit();}catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}finally{InteractionEpoch++;PreviewChanged?.Invoke();InvalidateVisual();}
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);FinishWheel();Focus();var screen=Screen(e);
        if(e.ChangedButton==MouseButton.Right){ShowOverlapMenu(View.ToPage(screen));e.Handled=true;return;}
        if(e.ChangedButton==MouseButton.Left&&Tools.PickingBalloonId is not null)
        {try{Tools.PickPanelAt(View.ToPage(screen));}catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}InvalidateVisual();e.Handled=true;return;}
        if(e.ChangedButton==MouseButton.Middle||(e.ChangedButton==MouseButton.Left&&(Mode==EditMode.View||Keyboard.IsKeyDown(Key.Space)))) {
            if(e.ChangedButton==MouseButton.Left&&Mode==EditMode.View) {
                var hit=HandleHitTest.Find(model,EditMode.Balloon,View.ToPage(screen),8/View.Zoom)??HandleHitTest.Find(model,EditMode.Image,View.ToPage(screen),8/View.Zoom);
                if(hit is not null)model.Select(hit.Kind,hit.Id,false);
            }
            panStart=screen;CaptureMouse();e.Handled=true;return;
        }
        if(e.ChangedButton!=MouseButton.Left)return;
        try {if(model.InputPending||model.Busy)throw new ComicEditor.Core.Validation.EditorException("BUSY","请先应用或放弃尚未提交的输入",6);var hit=HandleHitTest.Find(model,Mode,View.ToPage(screen),8/View.Zoom);if(hit is null)return;model.Select(hit.Kind,hit.Id);if(Mode==EditMode.Edge)model.SelectEdge(hit.Index);if(hit.DragKind==DragKind.Vertex)model.SelectVertex(hit.Index);if(hit.DragKind!=DragKind.Select){input.Begin(hit,View.ToPage(screen));InteractionEpoch++;CaptureMouse();}InvalidateVisual();e.Handled=true;}catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);var p=Screen(e);
        if(Tools.PickingBalloonId is not null){hoverPanelId=Tools.PanelsAt(View.ToPage(p)).FirstOrDefault()?.Text("id");InvalidateVisual();return;}
        if(panStart is {} previous) {View.Pan(p.X-previous.X,p.Y-previous.Y);panStart=p;InvalidateVisual();return;}
        if(!input.Dragging)return;
        try {input.Update(View.ToPage(p),(Keyboard.Modifiers&ModifierKeys.Alt)!=0);PreviewChanged?.Invoke();InvalidateVisual();}catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        try {if(input.Dragging){input.Commit();InteractionEpoch++;}}catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}finally{panStart=null;ReleaseMouseCapture();PreviewChanged?.Invoke();InvalidateVisual();}e.Handled=true;
    }
    public void Cancel() {var had=input.Dragging||wheel.Active;wheelIdle.Stop();input.Cancel();wheel.Cancel();Tools.CancelPick();hoverPanelId=null;panStart=null;if(IsMouseCaptured)ReleaseMouseCapture();if(had){InteractionEpoch++;PreviewChanged?.Invoke();}InvalidateVisual();}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if(e.Key==Key.Escape){Cancel();e.Handled=true;return;}
        if(!FocusCommandPolicy.CanvasShortcutAllowed(Keyboard.FocusedElement))return;
        try {
            if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&e.Key==Key.Z){model.Undo();e.Handled=true;return;}
            if((Keyboard.Modifiers&ModifierKeys.Control)!=0&&e.Key==Key.Y){model.Redo();e.Handled=true;return;}
            if(Mode==EditMode.View)return;
            if(e.Key==Key.Delete){DeleteSelected();e.Handled=true;return;}
            var amount=(Keyboard.Modifiers&ModifierKeys.Shift)!=0?10:1;var dx=e.Key==Key.Left?-amount:e.Key==Key.Right?amount:0;var dy=e.Key==Key.Up?-amount:e.Key==Key.Down?amount:0;
            if((dx!=0||dy!=0)&&model.SelectedId is not null) {FinishWheel();if(Mode is EditMode.Edge or EditMode.Vertex)return;var action=model.SelectedKind=="balloon"?"balloon.move":Mode==EditMode.Image?"panel.image":"panel.move";var args=action=="panel.image"?new System.Text.Json.Nodes.JsonObject{["offsetX"]=model.Selected?["imageTransform"].Number("offsetX")+dx,["offsetY"]=model.Selected?["imageTransform"].Number("offsetY")+dy}:new(){["dx"]=dx,["dy"]=dy};if(action=="balloon.move")args["moveGroup"]=MoveGroup;if(action=="panel.move")args["moveImage"]=Mode!=EditMode.FrameOnly;model.Execute([new(action,model.SelectedId,args)]);e.Handled=true;}
        }catch(Exception ex){Error?.Invoke(UiLabels.Message(ex));}
    }
    private void ShowOverlapMenu(P point)
    {
        var menu=new System.Windows.Controls.ContextMenu();
        foreach(var candidate in Tools.CandidatesAt(point))
        {var row=new System.Windows.Controls.MenuItem{Header=UiLabels.ForValue("kind",candidate.Kind)+" · "+candidate.Id};row.Click+=(_,_)=>model.Select(candidate.Kind,candidate.Id);menu.Items.Add(row);}
        if(menu.Items.Count==0)menu.Items.Add(new System.Windows.Controls.MenuItem{Header="此处没有可选对象",IsEnabled=false});
        ContextMenu=menu;menu.PlacementTarget=this;menu.IsOpen=true;
    }
    public void DeleteSelected() {if(model.SelectedId is null||ConfirmDelete is not null&&!ConfirmDelete())return;if(!model.TryLeaveContext(model.InputPending?model.ResolveDrafts?.Invoke()??DraftDecision.Cancel:DraftDecision.Discard))return;model.Execute([new(model.SelectedKind=="balloon"?"balloon.remove":"panel.remove",model.SelectedId,model.SelectedKind=="panel"?new(){["clipPolicy"]="free"}:new())]);}
}
