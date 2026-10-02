using System.Globalization;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.AppState;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Rendering;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.Controls;
namespace ComicEditor.Desktop;
public sealed record ObjectRow(string Id,string Display);
public partial class MainWindow : Window
{
    public EditorViewModel Model { get; }
    private bool refreshing;
    private bool uiReady;
    private readonly Dictionary<(string Kind,string? Id),InspectorUiState> inspectorStates=[];
    private sealed record PreviewState(ProjectDocument Document,PreviewEpoch Epoch,PreviewAssetCache Cache,bool Interactive);
    private sealed record PreviewFrame(ImageSource Image,PreviewEpoch Epoch);
    private readonly InteractivePreviewCoordinator<PreviewState,PreviewFrame> previewCoordinator;
    private readonly System.Windows.Threading.DispatcherTimer previewTimer;
    private bool closed,lastPending;
    private long interactionVersion,lastCanvasInteraction;
    private PreviewEpoch currentPreviewEpoch;
    public long PreviewPublishedFrames {get;private set;}
    private PreviewAssetCache previewAssets=new();
    private readonly PreviewAssetTracker assetTracker=new();
    private StatePaths? statePaths;
    private RecoveryStore? recovery;
    private readonly string recoveryId=Guid.NewGuid().ToString();
    private long recoveredRevision=-1;
    private System.Windows.Threading.DispatcherTimer? recoveryTimer;
    public ComicEditor.Session.SessionHost? AgentSession {get;private set;}
    public Canvas.CanvasView PreviewCanvas { get; }
    public MainWindow(EditorViewModel model)
    {
        refreshing=true;Model=model; InitializeComponent(); PreviewCanvas=new(model);CanvasHost.Child=PreviewCanvas;PreviewCanvas.Error+=ShowError;PreviewCanvas.PreviewChanged+=UpdatePreview;
        previewCoordinator=new(async(state,token)=>await Task.Run(()=>{using var page=new PageRenderer().RenderPreview(state.Document,new(Math.Min(1,(state.Interactive?600d:1200d)/new ProjectView(state.Document).Width)),state.Cache,state.Epoch.Assets,token);token.ThrowIfCancellationRequested();return new PreviewFrame(BitmapSourceConverter.Freeze(page.Pixels),state.Epoch);},token),frame=>Dispatcher.BeginInvoke(()=>{if(!closed&&frame.Epoch==currentPreviewEpoch){PreviewCanvas.SetPreview(frame.Image);PreviewPublishedFrames++;}}),_=>{});
        previewCoordinator.Failed+=ex=>Dispatcher.BeginInvoke(()=>{if(!closed)ShowError("预览未刷新："+ex.Message);});
        previewTimer=new(){Interval=TimeSpan.FromMilliseconds(33)};previewTimer.Tick+=(_,_)=>previewCoordinator.Tick();previewTimer.Start();
        ModeBox.ItemsSource=new[]{new EditToolOption(EditMode.View,"查看 / 平移"),new(EditMode.Image,"调整图片"),new(EditMode.Panel,"移动整个分镜"),new(EditMode.Vertex,"调整顶点"),new(EditMode.Balloon,"气泡变换"),new(EditMode.FrameOnly,"仅移动框线"),new(EditMode.Edge,"选择边线")};ModeBox.SelectedValue=Model.Selection.Current.Tool;
        refreshing=false;Model.ResolveDrafts=()=>Dialogs.ProjectDialogs.Modal(Model,()=>Dialogs.DraftDecisionDialog.Show(this));Model.PreviewChanged+=UpdatePreview;
        Model.Changed+=Refresh;PreviewCanvas.Tools.Changed+=RefreshContextToolbar;PreviewCanvas.ConfirmDelete=()=>Dialogs.ProjectDialogs.Modal(Model,()=>MessageBox.Show(this,"确认删除所选对象？此操作可撤销。","删除对象",MessageBoxButton.OKCancel,MessageBoxImage.Question))==MessageBoxResult.OK;
        Closing+=(_,e)=>{ if(!ConfirmDiscard())e.Cancel=true; }; Deactivated+=(_,_)=>PreviewCanvas.Cancel();Loaded+=(_,_)=>{uiReady=true;Refresh();};
        Model.AssetsChanged+=assetTracker.Invalidate;assetTracker.Changed+=()=>Dispatcher.BeginInvoke(()=>{previewAssets.Invalidate();UpdatePreview();});
        PanelsList.PreviewKeyDown+=(_,e)=>DeleteFromList("panel",e);BalloonsList.PreviewKeyDown+=(_,e)=>DeleteFromList("balloon",e);Closed+=(_,_)=>{closed=true;previewTimer.Stop();previewCoordinator.Dispose();assetTracker.Dispose();previewAssets.Dispose();};
    }
    public void Refresh()
    {
        if(!IsInitialized||refreshing)return; refreshing=true;
        try {
            var view=new ProjectView(Model.Document);
            assetTracker.Reset(Model.Document);
            PanelsList.ItemsSource=ObjectOrder.Ordered(Model.Document,"panel").Select(p=>new ObjectRow(p.Text("id"),p.Text("id")+" · "+p.Text("label")+(p.Flag("locked")?" 🔒":""))).ToArray();
            BalloonsList.ItemsSource=ObjectOrder.Ordered(Model.Document,"balloon").Select(p=>new ObjectRow(p.Text("id"),p.Text("id")+" · "+UiLabels.ForValue("kind",p.Text("kind","Complete"))+(p.Flag("locked")?" 🔒":""))).ToArray();
            if(Model.SelectedId is {} id) { var list=Model.SelectedKind=="panel"?PanelsList:BalloonsList; list.SelectedItem=list.Items.OfType<ObjectRow>().FirstOrDefault(r=>r.Id==id); }
            if(Model.SelectedKind is "panel" or "balloon")ObjectTabs.SelectedIndex=Model.SelectedKind=="panel"?0:1;
            ModeBox.SelectedValue=Model.Selection.Current.Tool;
            var inspectorKind=Model.Selected is null?"page":Model.SelectedKind;var inspectorId=inspectorKind=="page"?null:Model.SelectedId;
            if(InspectorHost.Child is InspectorView current&&current.Matches(inspectorKind,inspectorId))current.RefreshValues();
            else
            {
                if(InspectorHost.Child is InspectorView previous)inspectorStates[(previous.Kind,previous.ObjectId)]=InspectorUiState.Capture(previous);
                var next=new InspectorView(Model,inspectorKind,ShowError);InspectorHost.Child=next;
                if(inspectorStates.TryGetValue((inspectorKind,inspectorId),out var state))Dispatcher.BeginInvoke(()=>{if(ReferenceEquals(InspectorHost.Child,next))state.Restore(next);});
            }
            Title="漫画组装器 · "+(Model.FilePath is null?"新工程":Path.GetFileName(Model.FilePath))+(Model.Dirty?" *":"")+" · 原生预览版";
            StatusText.Text=$"{Model.Status}  |  {view.Width} × {view.Height}  |  {UiLabels.ForValue("mode",view.BorderMode)}  |  版本 {Model.Revision}  |  Agent {(AgentSession is null?"未连接":Model.AllowAgentWrite?"可写":"只读")}";
            RefreshContextToolbar();
            UpdatePreview();
        } finally { refreshing=false; }
    }
    public void UpdatePreview()
    {
        if(closed)return;
        if(lastCanvasInteraction!=PreviewCanvas.InteractionEpoch||lastPending!=Model.InputPending){interactionVersion++;lastCanvasInteraction=PreviewCanvas.InteractionEpoch;lastPending=Model.InputPending;}
        currentPreviewEpoch=new(Model.Revision,assetTracker.Epoch,interactionVersion);
        previewCoordinator.Submit(currentPreviewEpoch,new(PreviewCanvas.PreviewDocument,currentPreviewEpoch,previewAssets,Model.Busy||Model.InputPending),!Model.Busy&&!Model.InputPending);
    }
    public void ShowError(string message) { StatusText.Text=message; }
    public void AttachState(StatePaths paths,SettingsStore? settings)
    {
        statePaths=paths;recovery=new(paths.Recovery);
        if(settings is not null){try{var budget=settings.PreviewBudgetBytes();if(budget!=previewAssets.BudgetBytes){previewAssets.Dispose();previewAssets=new(budget);}}catch(Exception ex){ShowError("预览缓存设置无效，使用默认预算："+ex.Message);}}
        try{AgentSession=new(new Agent.EditorSessionAdapter(Model,Dispatcher),paths.Sessions);AgentSession.Error+=ex=>Dispatcher.BeginInvoke(()=>ShowError("Agent 会话异常："+ex.Message));Closed+=(_,_)=>AgentSession.Dispose();}catch(Exception ex){ShowError("Agent 会话未启动："+ex.Message);}
        recoveryTimer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
        recoveryTimer.Tick+=(_,_)=>{if(Model.Dirty&&Model.Revision!=recoveredRevision){try{recovery.Write(recoveryId,Model.Document,Model.FileHash,Model.Revision);recoveredRevision=Model.Revision;}catch(Exception ex){ShowError("恢复快照未写入："+ex.Message);}}};recoveryTimer.Start();
        Closed+=(_,_)=>{recoveryTimer.Stop();try{if(settings is not null){var root=settings.Read();root["lastProject"]=Model.FilePath;root["windowState"]=WindowState.ToString();settings.Write(root);}}catch(Exception ex){Log(ex);}};
    }
    private void Log(Exception ex)
    {
        try{if(statePaths is not null)File.AppendAllText(Path.Combine(statePaths.Logs,"desktop.log"),DateTimeOffset.UtcNow.ToString("O")+" "+ex.GetType().Name+" "+ex.Message[..Math.Min(1000,ex.Message.Length)]+Environment.NewLine);}catch{ /* Do not mask the original I/O failure. */ }
    }
    private void RecoveryClick(object sender,RoutedEventArgs e)
    {
        if(recovery is null){ShowError("未配置恢复目录。");return;}var dialog=new Dialogs.RecoveryDialog(recovery){Owner=this};
        if(Dialogs.ProjectDialogs.Modal(Model,()=>dialog.ShowDialog())==true&&dialog.Selected is {} snapshot&&ConfirmDiscard())Try(()=>Model.Restore(snapshot));
    }
    private void Try(Action action) {try{action();}catch(Exception ex){var message=UiLabels.Message(ex);ShowError(message);Dialogs.ProjectDialogs.Modal(Model,()=>MessageBox.Show(this,message,"操作未完成",MessageBoxButton.OK,MessageBoxImage.Warning));}}
    private void RefreshContextToolbar()
    {
        if(!IsInitialized)return;ContextToolbar.Visibility=Model.SelectedId is null?Visibility.Collapsed:Visibility.Visible;
        PanelToolButtons.Visibility=Model.SelectedKind=="panel"?Visibility.Visible:Visibility.Collapsed;BalloonToolButtons.Visibility=Model.SelectedKind=="balloon"?Visibility.Visible:Visibility.Collapsed;
        GroupMoveBox.Visibility=Model.SelectedKind=="balloon"?Visibility.Visible:Visibility.Collapsed;
        LockButton.Content=Model.Selected.Flag("locked")?"解锁":"锁定";
        StatusText.Text=Model.Status;
        SelectionHint.Text=PreviewCanvas.Tools.PickingBalloonId is not null?"点击目标分镜；Esc 取消":UiLabels.ForValue("kind",Model.SelectedKind)+" · "+Model.SelectedId+" · 右键选择被覆盖对象";
    }
    private void ToolButtonClick(object sender,RoutedEventArgs e){if(Enum.TryParse<EditMode>(((Button)sender).Tag.ToString(),out var mode)){PreviewCanvas.Cancel();PreviewCanvas.Mode=mode;}}
    private void SnapSelectedClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();PreviewCanvas.Tools.SnapSelectedEdge();}
    private void LayerUpClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();Try(()=>PreviewCanvas.Tools.Reorder("up"));}
    private void LayerDownClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();Try(()=>PreviewCanvas.Tools.Reorder("down"));}
    private void LockClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();Try(()=>PreviewCanvas.Tools.SetLocked(!Model.Selected.Flag("locked")));}
    private void ClipPickClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();Try(()=>{if(Model.SelectedId is {} id)PreviewCanvas.Tools.BeginClipPick(id);});}
    private void FreeClipClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();Try(()=>PreviewCanvas.Tools.SetClip(""));}
    private void CopyBalloonClick(object sender,RoutedEventArgs e){PreviewCanvas.FinishWheel();Try(()=>PreviewCanvas.Tools.CopySelected());}
    private void OcclusionMenuClick(object sender,RoutedEventArgs e)
    {
        var menu=new ContextMenu();
        foreach(var panel in ObjectOrder.Ordered(Model.Document,"panel"))
        {
            var id=panel.Text("id");var item=new MenuItem{Header=(panel.Text("label").Length==0?"分镜":panel.Text("label"))+" · "+id};
            foreach(var position in new[]{"front","back","inherit"}){var option=new MenuItem{Header=UiLabels.ForValue("position",position)};option.Click+=(_,_)=>Try(()=>PreviewCanvas.Tools.SetPanelOcclusion(id,position));item.Items.Add(option);}
            menu.Items.Add(item);
        }
        if(menu.Items.Count==0)menu.Items.Add(new MenuItem{Header="工程中没有分镜",IsEnabled=false});
        ((Button)sender).ContextMenu=menu;menu.PlacementTarget=(Button)sender;menu.IsOpen=true;
    }
    private void ReplaceSelectedClick(object sender,RoutedEventArgs e)
    {
        PreviewCanvas.FinishWheel();if(Model.SelectedId is null||!Model.TryLeaveContext(Model.InputPending?Model.ResolveDrafts?.Invoke()??DraftDecision.Cancel:DraftDecision.Discard))return;
        var dialog=new Microsoft.Win32.OpenFileDialog{Filter="完整图像素材|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp"};
        if(Dialogs.ProjectDialogs.Modal(Model,()=>dialog.ShowDialog(this))==true)Try(()=>Model.Execute([new("asset.replace",Model.SelectedId,new(){["kind"]=Model.SelectedKind,["sourceImage"]=dialog.FileName,["fitPolicy"]="preserve"})]));
    }
    private bool ConfirmDiscard()
    {
        if(Model.Busy)return false;if(Model.InputPending&&!Model.TryLeaveContext(Model.ResolveDrafts?.Invoke()??DraftDecision.Cancel))return false;if(!Model.Dirty)return true;
        var result=Dialogs.ProjectDialogs.Modal(Model,()=>MessageBox.Show(this,"当前工程有未保存修改。是否先保存？\n是：保存后继续；否：放弃；取消：留在当前工程。","未保存",MessageBoxButton.YesNoCancel,MessageBoxImage.Warning));
        return result==MessageBoxResult.No||result==MessageBoxResult.Yes&&SaveCurrent(false);
    }
    private bool SaveCurrent(bool saveAs)
    {
        try {
            if(Model.InputPending&&!Model.TryLeaveContext(Model.ResolveDrafts?.Invoke()??DraftDecision.Cancel)) {ShowError("尚未提交的属性输入未处理。");return false;}
            var path=Model.FilePath;var hash=Model.FileHash;
            if(saveAs||path is null){var d=new Microsoft.Win32.SaveFileDialog{Filter="漫画工程|*.json",FileName=path is null?"page.json":Path.GetFileName(path),OverwritePrompt=true};if(Dialogs.ProjectDialogs.Modal(Model,()=>d.ShowDialog(this))!=true)return false;path=d.FileName;hash=File.Exists(path)?AtomicFile.Hash(path):null;}
            Model.Save(path,new(File.Exists(path),hash));return true;
        }catch(Exception ex){ShowError(UiLabels.Message(ex));return false;}
    }
    private void OpenClick(object sender,RoutedEventArgs e) {var d=new Microsoft.Win32.OpenFileDialog{Filter="漫画工程|*.json"};if(Dialogs.ProjectDialogs.Modal(Model,()=>d.ShowDialog(this))==true)Try(()=>Model.TryOpen(d.FileName,ConfirmDiscard));}
    private void SaveClick(object sender,RoutedEventArgs e)=>SaveCurrent(false);
    private void SaveAsClick(object sender,RoutedEventArgs e)=>SaveCurrent(true);
    private void UndoClick(object sender,RoutedEventArgs e)=>Model.Undo();
    private void RedoClick(object sender,RoutedEventArgs e)=>Model.Redo();
    private void PageClick(object sender,RoutedEventArgs e)=>Model.Select("page",null);
    private void PanelSelected(object sender,SelectionChangedEventArgs e) { if(!refreshing&&PanelsList.SelectedItem is ObjectRow r)Model.Select("panel",r.Id); }
    private void BalloonSelected(object sender,SelectionChangedEventArgs e) { if(!refreshing&&BalloonsList.SelectedItem is ObjectRow r)Model.Select("balloon",r.Id); }
    private void ObjectTabChanged(object sender,SelectionChangedEventArgs e) { if(uiReady&&IsInitialized&&!refreshing&&e.Source==ObjectTabs)Model.ActivateKind(ObjectTabs.SelectedIndex==1?"balloon":"panel"); }
    private void ModeChanged(object sender,SelectionChangedEventArgs e) { if(uiReady&&!refreshing&&PreviewCanvas is not null&&ModeBox.SelectedItem is EditToolOption option){PreviewCanvas.Cancel();PreviewCanvas.Mode=option.Value;PreviewCanvas.InvalidateVisual();} }
    private void FitClick(object sender,RoutedEventArgs e)=>PreviewCanvas.Fit();
    private void DeleteFromList(string kind,System.Windows.Input.KeyEventArgs e) { if(e.Key!=System.Windows.Input.Key.Delete||Model.SelectedKind!=kind)return;Try(()=>PreviewCanvas.DeleteSelected());e.Handled=true; }
    private void AgentWriteChanged(object sender,RoutedEventArgs e) { if(Model is not null){Model.AllowAgentWrite=AgentWrite.IsChecked==true;if(IsInitialized&&PreviewCanvas is not null)Refresh();} }
    private void GroupMoveChanged(object sender,RoutedEventArgs e) { if(PreviewCanvas is not null)PreviewCanvas.MoveGroup=((CheckBox)sender).IsChecked==true; }
    private void ImportPanelClick(object sender,RoutedEventArgs e)=>Import(false);
    private void ImportBalloonClick(object sender,RoutedEventArgs e)=>Import(true);
    private void Import(bool balloon)
    {
        var d=new Microsoft.Win32.OpenFileDialog{Filter="图像|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp"}; if(Dialogs.ProjectDialogs.Modal(Model,()=>d.ShowDialog(this))!=true)return;
        Try(()=>{
            var fingerprint=AssetResolver.Fingerprint(d.FileName); var id=(balloon?"B":"P")+Guid.NewGuid().ToString("N")[..8];
            JsonObject obj;
            if(balloon) {
                var kind=new ComboBox{ItemsSource=new[]{"Complete","Body","Tail","Lettering"}.Select(value=>new UiOption(value,UiLabels.ForValue("kind",value))).ToArray(),DisplayMemberPath="Label",SelectedValuePath="Value",SelectedIndex=0,Margin=new(12)};
                var clips=new ComboBox{ItemsSource=new[]{new UiOption("","整页自由")}.Concat(new ProjectView(Model.Document).Panels.Select(p=>new UiOption(p.Text("id"),(p.Text("label").Length==0?"分镜":p.Text("label"))+" · "+p.Text("id")))).ToArray(),DisplayMemberPath="Label",SelectedValuePath="Value",SelectedIndex=0,Margin=new(12)};
                var ok=new Button{Content="导入",Margin=new(12)}; var stack=new StackPanel(); stack.Children.Add(new TextBlock{Text="气泡类型 / 限框（空白=自由）",Margin=new(12)}); stack.Children.Add(kind); stack.Children.Add(clips); stack.Children.Add(ok);
                var dialog=new Window{Owner=this,Title="导入独立气泡",SizeToContent=SizeToContent.WidthAndHeight,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=stack}; ok.Click+=(_,_)=>dialog.DialogResult=true; if(Dialogs.ProjectDialogs.Modal(Model,()=>dialog.ShowDialog())!=true)return;
                obj=new(){["id"]=id,["kind"]=kind.SelectedValue?.ToString(),["sourceImage"]=d.FileName,["clipPanelId"]=clips.SelectedValue?.ToString(),["visible"]=true,["lockAspect"]=true,["zIndex"]=1000,["transform"]=new JsonObject{["x"]=100,["y"]=100,["width"]=240,["height"]=240d*fingerprint.Height/fingerprint.Width}};
            } else obj=new(){["id"]=id,["sourceImage"]=d.FileName,["polygon"]=JsonNode.Parse("[[40,40],[400,40],[400,300],[40,300]]"),["zIndex"]=10,["imageTransform"]=new JsonObject{["scale"]=1}};
            Model.Execute([new(balloon?"balloon.add":"panel.add",null,new JsonObject{["object"]=obj})]); Model.Select(balloon?"balloon":"panel",id);
        });
    }
    private void ExportClick(object sender,RoutedEventArgs e)
    {
        if(Model.InputPending&&!Model.TryLeaveContext(Model.ResolveDrafts?.Invoke()??DraftDecision.Cancel))return;
        var dialog=new Dialogs.ExportDialog(Model){Owner=this}; Dialogs.ProjectDialogs.Modal(Model,()=>dialog.ShowDialog());
    }
}
