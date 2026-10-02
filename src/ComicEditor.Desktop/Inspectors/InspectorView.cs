using System.Globalization;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Desktop.Editing;
using ComicEditor.Desktop.Controls;
namespace ComicEditor.Desktop.Inspectors;
public sealed class InspectorView : ScrollViewer
{
    public Dictionary<string,FrameworkElement> Fields { get; }=[];
    private readonly EditorViewModel model;
    private readonly Action<string> error;
    private bool syncing;
    public string Kind { get; }
    public string? ObjectId { get; }
    private static readonly Dictionary<string,string> Labels=new() {
        ["object.reorder"]="相邻层级",["balloon.panelOcclusion"]="相对分镜遮挡",
        ["panel.image"]="原图取景",["panel.bounds"]="分镜位置与大小",["panel.vertex"]="顶点",["panel.edge"]="逐边显示",["panel.snap"]="边线吸附",["panel.move"]="移动整个分镜",["panel.polygon"]="多边形与分割形状",["object.layer"]="层级",["object.lock"]="锁定",["object.rename"]="名称与稳定 ID",["asset.replace"]="更换完整素材",["object.reset"]="恢复初始状态",["panel.remove"]="删除分镜",["balloon.remove"]="删除气泡",["panel.add"]="添加分镜（JSON）",["balloon.add"]="添加气泡（JSON）",["page.canvas"]="页面与高清倍率",["page.border"]="边框与单线",["page.snap"]="自动吸附",["balloon.transform"]="气泡变换",["balloon.clip"]="限框／跨分镜",["balloon.group"]="气泡分组",["balloon.move"]="移动气泡／整组",["balloon.visible"]="气泡可见性",["balloon.copy"]="复制气泡"
    };
    private static readonly Dictionary<string,string> Names=new() {
        ["direction"]="相邻层级方向",["panelId"]="目标分镜",["position"]="前后遮挡关系",["autoOrder"]="自动调整当前气泡层级",
        ["x"]="横向位置",["y"]="纵向位置",["width"]="宽",["height"]="高",["offsetX"]="图片横向偏移",["offsetY"]="图片纵向偏移",["scale"]="原图倍率",["rotation"]="旋转角度",["opacity"]="不透明度",["flipX"]="水平翻转",["flipY"]="垂直翻转",["lockAspect"]="保持比例",["clipPanelId"]="显示范围",["groupId"]="分组编号",["moveGroup"]="同时移动同组",["zIndex"]="精确层级数值",["readingOrder"]="阅读顺序（同层）",["kind"]="对象类型",["label"]="显示名称",["newId"]="新编号",["locked"]="锁定",["visible"]="显示",["index"]="选中的边或顶点",["color"]="线色",["background"]="底色",["exportScale"]="导出倍率",["mode"]="边框模式",["outerEnabled"]="显示页面外框",["outerWidth"]="外框线宽",["enabled"]="启用",["tolerance"]="吸附距离（基础画布单位）",["fitMode"]="适配",["fitBiasX"]="横向偏重",["fitBiasY"]="纵向偏重",["dx"]="横向移动量",["dy"]="纵向移动量",["moveImage"]="移动原图",["sourceImage"]="素材路径",["fitPolicy"]="更换策略",["points"]="顶点坐标 JSON",["edgeVisibility"]="逐边显隐 JSON",["object"]="对象 JSON",["clipPolicy"]="引用气泡处理",["scope"]="重置范围"
    };
    private static readonly HashSet<string> Flags=["flipX","flipY","lockAspect","moveGroup","locked","visible","outerEnabled","enabled","moveImage","autoOrder"];
    private static readonly HashSet<string> Texts=["kind","label","newId","color","background","mode","fitMode","sourceImage","fitPolicy","clipPanelId","groupId","clipPolicy","scope"];
    public InspectorView(EditorViewModel vm,string kind,Action<string>? showError=null)
    {
        model=vm;Kind=kind;ObjectId=kind=="page"?null:vm.SelectedId; error=showError??(_=>{}); VerticalScrollBarVisibility=ScrollBarVisibility.Auto; HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled; Padding=new(14);
        PreviewMouseWheel+=(s,e)=>{ ScrollToVerticalOffset(VerticalOffset-e.Delta); e.Handled=true; };
        var stack=new StackPanel(); Content=stack;
        stack.Children.Add(new TextBlock{Text=kind switch{"panel"=>"分镜属性","balloon"=>"气泡属性",_=>"页面设置"},FontSize=18,FontWeight=FontWeights.SemiBold,Margin=new(0,0,0,12)});
        var discard=new Button{Content="放弃未提交的输入",HorizontalAlignment=HorizontalAlignment.Left,Margin=new(0,0,0,12),Padding=new(8,5,8,5)};discard.Click+=(_,_)=>model.DiscardDrafts();stack.Children.Add(discard);
        foreach(var action in InspectorCatalog.Actions(kind))
        {
            if(action=="panel.image"||action=="balloon.transform"||action=="page.canvas")stack.Children.Add(new TextBlock{Text=kind=="panel"?"图片取景":kind=="balloon"?"位置与外观":"页面与输出",FontWeight=FontWeights.SemiBold,Foreground=System.Windows.Media.Brushes.SlateGray,Margin=new(0,10,0,8)});
            if(action=="panel.bounds"||action=="balloon.clip"||action=="page.border")stack.Children.Add(new TextBlock{Text=kind=="panel"?"框线与形状":kind=="balloon"?"范围与遮挡":"边框与布局",FontWeight=FontWeights.SemiBold,Foreground=System.Windows.Media.Brushes.SlateGray,Margin=new(0,14,0,8)});
            if(action=="object.layer"||action=="panel.add")stack.Children.Add(new TextBlock{Text="高级精确调整",ToolTip="保留全部精确参数与 JSON 操作；常用动作可在画布工具条完成。",FontWeight=FontWeights.SemiBold,Foreground=System.Windows.Media.Brushes.SlateGray,Margin=new(0,14,0,8)});
            var definition=CommandRegistry.Actions.First(a=>a.Name==action); var group=new StackPanel{Margin=new(0,8,0,10)}; var inputs=new Dictionary<string,FrameworkElement>(); var initial=new Dictionary<string,string>();var loading=false;
            var key=new DraftKey(kind,kind=="page"?null:model.SelectedId,action);var defaults=new JsonObject();
            foreach(var field in definition.AllowedArgs)
            {
                if(field=="readingOrder"&&kind!="panel")continue;
                var value=Current(action,field,kind); FrameworkElement control;
                if(field is "color" or "background")control=new ColorPicker(Display(value));
                else if(Flags.Contains(field)) control=new CheckBox{IsChecked=value?.GetValue<bool>()??false,Content=Names.GetValueOrDefault(field,field),Margin=new(0,4,0,4)};
                else
                {
                    group.Children.Add(new TextBlock{Text=Names.GetValueOrDefault(field,field),Foreground=System.Windows.Media.Brushes.DimGray,Margin=new(0,4,0,3)});
                    var choices=field=="index"&&model.Selected is {} indexed?Enumerable.Range(0,indexed["polygon"]!.AsArray().Count).Select(i=>i.ToString(CultureInfo.InvariantCulture)).ToArray():Choices(field,kind);
                    if(field=="exportScale")control=new ComboBox{ItemsSource=ExportOptions(),DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=Display(value),MinHeight=30};
                    else if(field is "clipPanelId" or "panelId")control=new ComboBox{ItemsSource=PanelOptions(field=="clipPanelId"),DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=Display(value),MinHeight=30};
                    else if(choices.Length>0) control=new ComboBox{ItemsSource=choices.Select(choice=>Option(action,field,choice)).ToArray(),DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=value is null?choices[0]:Display(value),MinHeight=30};
                    else control=new TextBox{Text=Display(value),MinHeight=30,Padding=new(6,4,6,4),AcceptsReturn=field is "object" or "points" or "edgeVisibility",TextWrapping=TextWrapping.Wrap};
                }
                control.Tag=action+":"+field; Fields[(string)control.Tag]=control; inputs[field]=control; group.Children.Add(control);
                initial[field]=ControlText(control);
                defaults[field]=InspectorDraftStore.Parse(field,initial[field]);
                if(model.Drafts.Raw(key,field) is {} raw){if(control is TextBox rawBox)rawBox.Text=raw;else if(control is ComboBox rawCombo)rawCombo.SelectedValue=raw;else if(control is CheckBox rawCheck&&bool.TryParse(raw,out var flag))rawCheck.IsChecked=flag;}
                void Stage(){if(!loading&&!syncing)model.Drafts.StageRaw(key,field,ControlText(control));}
                if(control is TextBox tb) { tb.TextChanged+=(_,_)=>Stage(); tb.PreviewKeyDown+=(_,e)=>{ if(e.Key==Key.Enter&&!tb.AcceptsReturn){ Commit(); e.Handled=true; }else if(e.Key==Key.Escape){model.Drafts.Discard(key);model.RefreshAfterDraft();e.Handled=true;} }; }
                void Discrete()
                {
                    if(loading||syncing)return;var requested=ControlText(control);
                    if(!model.TryLeaveContext(model.InputPending?model.ResolveDrafts?.Invoke()??DraftDecision.Cancel:DraftDecision.Discard)){RefreshValues();return;}
                    try
                    {
                        var args=new JsonObject{[field]=InspectorDraftStore.Parse(field,requested)};
                        if(action.StartsWith("object.",StringComparison.Ordinal))args["kind"]=kind;
                        if(action=="balloon.panelOcclusion"){args["panelId"]=ControlText(inputs["panelId"]);args["autoOrder"]=((CheckBox)inputs["autoOrder"]).IsChecked==true;}
                        if(action=="panel.edge")args["index"]=InspectorDraftStore.Parse("index",ControlText(inputs["index"]));
                        model.Execute([new(action,action.StartsWith("page.",StringComparison.Ordinal)?null:model.SelectedId,args)]);
                    }catch(Exception ex){error(UiLabels.Message(ex));RefreshValues();}
                }
                if(control is ComboBox cb) cb.SelectionChanged+=(_,_)=>
                {
                    if(loading||syncing)return;
                    if(field=="index"){if(int.TryParse(ControlText(control),out var selectedIndex)){if(action=="panel.vertex")model.SelectVertex(selectedIndex);else model.SelectEdge(selectedIndex);}ReloadIndex();RefreshValues();return;}
                    if(field=="panelId"){RefreshValues();return;}
                    if(field is "clipPanelId" or "fitMode" or "mode" or "position" or "exportScale")Discrete();
                    else if(field!="kind"){Stage();if(field=="index")ReloadIndex();}
                };
                if(control is CheckBox ck) {void ChangeFlag(){if(field=="autoOrder")return;if(field is "moveGroup" or "moveImage")Stage();else Discrete();}ck.Checked+=(_,_)=>ChangeFlag(); ck.Unchecked+=(_,_)=>ChangeFlag(); }
                if(control is ColorPicker picker)
                {
                    var owned=false;
                    picker.CanBegin=()=>model.ModalDepth==0&&model.TryLeaveContext(model.InputPending?model.ResolveDrafts?.Invoke()??DraftDecision.Cancel:DraftDecision.Discard);
                    picker.Activated+=()=>{owned=true;model.ModalDepth++;};
                    picker.PreviewChanged+=selected=>model.Drafts.StageRaw(key,field,selected);
                    void Release(){if(owned){owned=false;model.ModalDepth--;}}
                    picker.Confirmed+=selected=>{try{model.Drafts.StageRaw(key,field,selected);Release();model.Drafts.CommitGroup(key,model);}catch(Exception ex){Release();error(UiLabels.Message(ex));}};
                    picker.Canceled+=()=>{Release();model.Drafts.Discard(key);model.RefreshAfterDraft();};
                }
            }
            model.Drafts.Configure(key,defaults);
            void ReloadIndex()
            {
                if(model.Selected is not {} selected||!inputs.TryGetValue("index",out var control)||!int.TryParse(ControlText(control),out var index))return;
                loading=true;
                try
                {
                    var points=PolygonOperations.Read(selected["polygon"]!);
                    if(action=="panel.vertex")foreach(var name in new[]{"x","y"}){((TextBox)inputs[name]).Text=(name=="x"?points[index].X:points[index].Y).ToString(CultureInfo.InvariantCulture);initial[name]=ControlText(inputs[name]);}
                    if(action=="panel.edge"){((CheckBox)inputs["visible"]).IsChecked=ProjectView.EdgeVisible(selected,index);initial["visible"]=ControlText(inputs["visible"]);}
                }
                finally{loading=false;}
            }
            void Commit()
            {
                try {
                    if(action.EndsWith(".remove",StringComparison.Ordinal)&&MessageBox.Show("确认删除当前对象？可撤销。","删除",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;
                    if(!InspectorDraftStore.PatchOnly(action))foreach(var (name,control) in inputs)model.Drafts.StageRaw(key,name,ControlText(control));
                    model.Drafts.CommitGroup(key,model);
                } catch(Exception ex) { error(UiLabels.Message(ex)); }
            }
            var apply=new Button{Content="应用",Margin=new(0,10,0,0),Padding=new(8,5,8,5),HorizontalAlignment=HorizontalAlignment.Right}; apply.Click+=(_,_)=>Commit(); group.Children.Add(apply);
            if(action=="panel.edge") {var all=new Button{Content="显示此分镜所有边",Margin=new(0,8,0,0),Padding=new(8,5,8,5)};all.Click+=(_,_)=>{try{model.Execute(Enumerable.Range(0,model.Selected!["polygon"]!.AsArray().Count).Select(i=>new Operation("panel.edge",model.SelectedId,new(){["index"]=i,["visible"]=true})).ToArray());}catch(Exception ex){error(ex.Message);}};group.Children.Add(all);}
            stack.Children.Add(new Expander{Header=Labels.GetValueOrDefault(action,action),Tag=action,Content=group,IsExpanded=action==InspectorCatalog.Actions(kind).First(),Margin=new(0,0,0,10)});
        }
    }
    public bool Matches(string kind,string? id)=>Kind==kind&&ObjectId==(kind=="page"?null:id);
    public void RefreshValues()
    {
        syncing=true;
        try
        {
            var groupDefaults=new Dictionary<string,JsonObject>();
            foreach(var (tag,control) in Fields)
            {
                var parts=tag.Split(':',2);var action=parts[0];var field=parts[1];
                var key=new DraftKey(Kind,ObjectId,action);
                var value=Current(action,field,Kind);
                if(field is "clipPanelId" or "panelId"&&control is ComboBox panels)
                {
                    var desired=field=="clipPanelId"?model.Drafts.Raw(key,field)??Display(value):ControlText(control);var options=PanelOptions(field=="clipPanelId");
                    if(!options.Any(o=>o.Value==desired))desired=options.FirstOrDefault()?.Value??"";
                    panels.ItemsSource=options;panels.SelectedValue=desired;value=JsonValue.Create(desired);
                }
                if(field=="index"&&control is ComboBox indexControl&&model.Selected is {} selected)
                {
                    var count=selected["polygon"]!.AsArray().Count;
                    var index=action=="panel.vertex"?model.Selection.Current.VertexIndex??0:model.Selection.Current.EdgeIndex??0;
                    var choices=Enumerable.Range(0,count).Select(i=>Option(action,field,i.ToString(CultureInfo.InvariantCulture))).ToArray();
                    if(indexControl.Items.Count!=count)indexControl.ItemsSource=choices;
                    value=JsonValue.Create(index);
                }
                if(field is "direction" or "panelId" or "scope")value=JsonValue.Create(ControlText(control));
                if(field=="exportScale"&&control is ComboBox scaleControl){var desired=model.Drafts.Raw(key,field)??Display(value);scaleControl.ItemsSource=ExportOptions();scaleControl.SelectedValue=desired;}
                if(!groupDefaults.TryGetValue(action,out var defaults))groupDefaults[action]=defaults=new();
                defaults[field]=value?.DeepClone();
                var display=model.Drafts.Raw(key,field)??Display(value);
                if(control is ColorPicker picker&&!picker.Active&&picker.CurrentHex!=display)picker.Load(display);
                else if(control is TextBox box&&box.Text!=display)
                {var caret=box.SelectionStart;var length=box.SelectionLength;box.Text=display;box.Select(Math.Min(caret,box.Text.Length),Math.Min(length,Math.Max(0,box.Text.Length-caret)));}
                else if(control is ComboBox combo&&ControlText(combo)!=display)combo.SelectedValue=display;
                else if(control is CheckBox check&&bool.TryParse(display,out var flag)&&check.IsChecked!=flag)check.IsChecked=flag;
            }
            foreach(var (action,defaults) in groupDefaults)model.Drafts.Configure(new(Kind,ObjectId,action),defaults);
        }
        finally{syncing=false;}
    }
    private static string Display(JsonNode? value)=>value is null?"":value is JsonValue v&&v.TryGetValue<string>(out var text)?text:value is JsonValue numeric&&numeric.TryGetValue<double>(out var number)?number.ToString(CultureInfo.InvariantCulture):value.ToJsonString();
    private static string ControlText(FrameworkElement control)=>control is ColorPicker picker?picker.CurrentHex:control is TextBox tb?tb.Text:control is ComboBox cb?cb.SelectedValue?.ToString()??"":((CheckBox)control).IsChecked==true?"true":"false";
    private static UiOption Option(string action,string field,string value)=>new(value,field=="index"&&int.TryParse(value,out var index)?(action=="panel.vertex"?"点":"边")+(index+1):UiLabels.ForValue(field,value));
    private UiOption[] PanelOptions(bool free)=> (free?new[]{new UiOption("","整页自由")}:Array.Empty<UiOption>()).Concat(new ProjectView(model.Document).Panels.Select(p=>new UiOption(p.Text("id"),(p.Text("label").Length==0?"分镜":p.Text("label"))+" · "+p.Text("id")))).ToArray();
    private UiOption[] ExportOptions()=>ExportPreset.For(model.Document).Select(p=>new UiOption(p.Scale.ToString(CultureInfo.InvariantCulture),p.Label)).ToArray();
    private static string[] Choices(string name,string kind)=>name switch {"kind"=>[kind=="page"?"panel":kind],"direction"=>["up","down"],"position"=>["front","back","inherit"],"fitMode"=>["Cover","Contain"],"mode"=>["SingleLine","Legacy"],"fitPolicy"=>["preserve","refit"],"clipPolicy"=>["free"],"scope"=>kind=="panel"?["image","frame"]:["balloon"],_=>[]};
    private JsonNode? Current(string action,string field,string kind)
    {
        var root=model.Document.Root; var obj=model.Selected; var t=obj?["transform"];
        if(field=="kind")return JsonValue.Create(kind);
        if(field=="direction")return JsonValue.Create("up");
        if(field=="autoOrder")return JsonValue.Create(!Fields.TryGetValue(action+":autoOrder",out var control)||((CheckBox)control).IsChecked==true);
        if(field=="panelId")return JsonValue.Create(new ProjectView(model.Document).Panels.FirstOrDefault()?.Text("id")??"");
        if(field=="position"){var target=Fields.TryGetValue("balloon.panelOcclusion:panelId",out var selector)?ControlText(selector):new ProjectView(model.Document).Panels.FirstOrDefault()?.Text("id")??"";return JsonValue.Create(obj?["panelOcclusion"].Text(target,"inherit")??"inherit");}
        if(field=="index")return JsonValue.Create(action=="panel.vertex"?model.Selection.Current.VertexIndex??0:model.Selection.Current.EdgeIndex??0);
        if(field=="dx"||field=="dy")return JsonValue.Create(0);
        if(field=="moveImage")return JsonValue.Create(true);
        if(field=="moveGroup")return JsonValue.Create(false);
        if(field=="fitPolicy")return JsonValue.Create("preserve");
        if(field=="clipPolicy")return JsonValue.Create("free");
        if(field=="scope")return JsonValue.Create(kind=="panel"?"image":"balloon");
        if(field=="newId")return JsonValue.Create(action=="balloon.copy"?(model.SelectedId+"_copy"):model.SelectedId??"");
        if(action=="page.canvas")return root["canvas"]?[field]?.DeepClone()??JsonValue.Create(field=="background"?"#FFFFFF":field=="exportScale"?"2":field=="width"?"1024":"1536");
        if(action=="page.border")return field=="outerEnabled"?JsonValue.Create(root["outerBorder"].Flag("enabled",true)):field=="outerWidth"?JsonValue.Create(root["outerBorder"].Number("width",ProjectDefaults.OuterBorderWidth)):root["border"]?[field]?.DeepClone()??JsonValue.Create(field=="color"?"#000000":field=="mode"?"Legacy":ProjectDefaults.BorderWidth.ToString(CultureInfo.InvariantCulture));
        if(action=="page.snap")return field=="enabled"?JsonValue.Create(root["edgeSnap"].Flag("enabled")):JsonValue.Create(root["edgeSnap"].Number("tolerance",8));
        if(action=="panel.bounds"&&obj is not null) { var b=PolygonOperations.Bounds(PolygonOperations.Read(obj["polygon"]!)); return JsonValue.Create(field switch{"x"=>b.X,"y"=>b.Y,"width"=>b.Width,_=>b.Height}); }
        if(action=="panel.vertex"&&obj is not null) { var points=PolygonOperations.Read(obj["polygon"]!);var index=Fields.TryGetValue(action+":index",out var selector)&&int.TryParse(ControlText(selector),out var i)&&i>=0&&i<points.Length?i:0;var point=points[index];return JsonValue.Create(field=="x"?point.X:point.Y); }
        if(action=="panel.polygon"&&obj is not null)return field=="points"?obj["polygon"]!.DeepClone():new JsonArray(Enumerable.Range(0,obj["polygon"]!.AsArray().Count).Select(i=>(JsonNode)JsonValue.Create(ProjectView.EdgeVisible(obj,i))!).ToArray());
        if(action=="panel.edge"&&field=="visible"){var index=Fields.TryGetValue(action+":index",out var selector)&&int.TryParse(ControlText(selector),out var i)?i:0;return JsonValue.Create(obj is not null&&ProjectView.EdgeVisible(obj,index));}
        if(field=="tolerance")return JsonValue.Create(root["edgeSnap"].Number("tolerance",8));
        if(action=="panel.image")return field is "offsetX" or "offsetY" or "scale"?JsonValue.Create(obj?["imageTransform"].Number(field,field=="scale"?1:0)??0):field=="fitMode"?JsonValue.Create(obj.Text(field,"Cover")):JsonValue.Create(obj.Number(field,ProjectDefaults.FitBias));
        if(action=="balloon.transform")return field=="lockAspect"?JsonValue.Create(obj.Flag(field,true)):Flags.Contains(field)?JsonValue.Create(t.Flag(field)):JsonValue.Create(t.Number(field,field=="width"?240:field=="height"?150:field=="opacity"?1:field is "x" or "y"?ProjectDefaults.BalloonPosition:0));
        if(field=="zIndex")return JsonValue.Create(obj.Number(field,kind=="panel"?0:1000));
        if(Flags.Contains(field))return JsonValue.Create(obj.Flag(field,field=="visible"));
        if(field=="object")return JsonNode.Parse(kind=="page"&&action=="panel.add"?"{\"id\":\"PNEW\",\"sourceImage\":\"\",\"polygon\":[[10,10],[200,10],[200,200],[10,200]]}":"{\"id\":\"BNEW\",\"kind\":\"Complete\",\"sourceImage\":\"\",\"transform\":{\"x\":100,\"y\":100,\"width\":240,\"height\":150}}");
        return obj?[field]?.DeepClone()??(Texts.Contains(field)?JsonValue.Create(""):JsonValue.Create(0));
    }
}
