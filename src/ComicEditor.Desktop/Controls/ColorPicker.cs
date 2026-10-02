using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
namespace ComicEditor.Desktop.Controls;
public sealed class ColorPicker : UserControl
{
    private readonly Border swatch=new(){Width=28,Height=20,CornerRadius=new(3),Margin=new(0,0,8,0)};
    private readonly TextBlock label=new(){VerticalAlignment=VerticalAlignment.Center};
    private readonly Popup popup;
    private readonly Slider[] sliders=new Slider[3];
    private readonly TextBox hex=new(){MinWidth=130,Padding=new(6)};
    private readonly Border sample=new(){Height=24,Margin=new(0,6,0,8)};
    private bool syncing;
    private string original="#000000";
    public string CurrentHex {get;private set;}="#000000";
    public bool Active {get;private set;}
    public Func<bool>? CanBegin {get;set;}
    public event Action? Activated;
    public event Action<string>? PreviewChanged;
    public event Action<string>? Confirmed;
    public event Action? Canceled;
    public ColorPicker(string initial="#000000")
    {
        var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(swatch);row.Children.Add(label);
        var open=new Button{Content=row,Padding=new(8,5,8,5),HorizontalContentAlignment=HorizontalAlignment.Left,ToolTip="选择颜色：常用色、自由 RGB 和高级 HEX"};Content=open;
        var body=new StackPanel{Margin=new(14),Width=255};
        body.Children.Add(new TextBlock{Text="颜色预览 · 确认后应用",FontWeight=FontWeights.SemiBold});
        var palette=new WrapPanel{Margin=new(0,10,0,6)};
        foreach(var value in new[]{"#000000","#FFFFFF","#444444","#888888","#CCCCCC","#000022"})
        {var button=new Button{Background=Brush(value),Width=30,Height=26,Margin=new(3),ToolTip=value};button.Click+=(_,_)=>SetColorPreview(value);palette.Children.Add(button);}body.Children.Add(palette);
        foreach(var (name,index) in new[]{("红",0),("绿",1),("蓝",2)})
        {
            body.Children.Add(new TextBlock{Text=name+"（0–255）",Foreground=Brushes.DimGray});
            var slider=new Slider{Minimum=0,Maximum=255,TickFrequency=1,IsSnapToTickEnabled=true,Margin=new(0,4,0,8)};sliders[index]=slider;
            slider.PreviewMouseWheel+=(_,e)=>e.Handled=true;
            slider.ValueChanged+=(_,_)=>{if(!syncing&&Active)SetColorPreview($"#{(int)sliders[0].Value:X2}{(int)sliders[1].Value:X2}{(int)sliders[2].Value:X2}");};body.Children.Add(slider);
        }
        body.Children.Add(sample);body.Children.Add(new Expander{Header="高级：HEX 颜色代码",Content=hex,Margin=new(0,0,0,8)});
        hex.PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Enter){try{SetColorPreview(hex.Text);}catch(FormatException){hex.ToolTip="请输入 #RRGGBB；不支持透明度代码。";}e.Handled=true;}};
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};
        var cancel=new Button{Content="取消",Padding=new(10,5,10,5),Margin=new(3)};var confirm=new Button{Content="确认颜色",Padding=new(10,5,10,5),Margin=new(3)};
        cancel.Click+=(_,_)=>Cancel();confirm.Click+=(_,_)=>Confirm();actions.Children.Add(cancel);actions.Children.Add(confirm);body.Children.Add(actions);
        popup=new(){PlacementTarget=open,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,Child=new Border{Child=body,Background=Brushes.White,BorderBrush=Brushes.SlateGray,BorderThickness=new(1),CornerRadius=new(7)}};
        popup.Closed+=(_,_)=>{if(Active)Cancel();};
        open.Click+=(_,_)=>{Begin(CurrentHex);if(Active)popup.IsOpen=true;};Load(initial);
    }
    public void Begin(string opaqueRgbHex)
    {
        var value=Normalize(opaqueRgbHex);if(Active)return;if(CanBegin is not null&&!CanBegin())return;
        original=value;Load(value);Active=true;Activated?.Invoke();
    }
    public void Load(string opaqueRgbHex)
    {
        CurrentHex=Normalize(opaqueRgbHex);syncing=true;
        try{swatch.Background=Brush(CurrentHex);sample.Background=Brush(CurrentHex);label.Text="选择颜色  "+CurrentHex;hex.Text=CurrentHex;
            for(var i=0;i<3;i++)sliders[i].Value=Convert.ToInt32(CurrentHex.Substring(1+i*2,2),16);}
        finally{syncing=false;}
    }
    public void SetColorPreview(string opaqueRgbHex)
    {if(!Active)throw new InvalidOperationException("请先开始选色");Load(opaqueRgbHex);PreviewChanged?.Invoke(CurrentHex);}
    public void Confirm(){if(!Active)return;Active=false;popup.IsOpen=false;Confirmed?.Invoke(CurrentHex);}
    public void Cancel(){if(!Active)return;Active=false;Load(original);popup.IsOpen=false;Canceled?.Invoke();}
    private static string Normalize(string value)=>Regex.IsMatch(value,"^#[0-9a-fA-F]{6}$")?value.ToUpperInvariant():throw new FormatException("仅支持不透明的 #RRGGBB 颜色");
    private static Brush Brush(string value)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
}
