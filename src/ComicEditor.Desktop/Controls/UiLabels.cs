using ComicEditor.Core.Validation;
namespace ComicEditor.Desktop.Controls;
public sealed record UiOption(string Value,string Label)
{public override string ToString()=>Label;}
public static class UiLabels
{
    private static readonly IReadOnlyDictionary<string,string> Values=new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["SingleLine"]="简洁单线",["Legacy"]="原版边框",["Cover"]="铺满分镜",["Contain"]="完整显示",["preserve"]="保留当前取景",["refit"]="重新适配",
        ["Complete"]="完整气泡",["Body"]="气泡主体",["Tail"]="气泡尾巴",["Lettering"]="独立文字",["panel"]="分镜",["balloon"]="气泡",["page"]="页面",
        ["free"]="解除相关引用",["image"]="图片取景",["frame"]="分镜框线",["up"]="上移一层",["down"]="下移一层",["front"]="置于该分镜前",["back"]="置于该分镜后",["inherit"]="恢复原行为"
    };
    public static string ForValue(string field,string value)=>field=="clipPanelId"&&value.Length==0?"整页自由":Values.GetValueOrDefault(value,value);
    public static string ForError(string code)=>code switch
    {
        "LOCKED"=>"对象已锁定，请先解锁。","BUSY"=>"请先结束当前操作或处理未提交输入。","CONFLICT"=>"工程或文件已发生变化，请刷新后重试。",
        "OBJECT_NOT_FOUND" or "TARGET"=>"找不到目标对象，请重新选择。","ARGS" or "NUMBER" or "INDEX"=>"输入值不符合要求，请检查参数。",
        "LAYER_RELATION_CONFLICT"=>"前后遮挡与现有层级冲突，请调整所选对象，不会自动重排其它对象。",
        "CLIP_REFERENCE" or "OCCLUSION_REFERENCE"=>"该分镜仍被气泡引用，请先解除引用。","ASPECT_LOCK"=>"请先关闭保持比例，再分别调整宽高。",
        "MEMORY_BUDGET" or "DIMENSION_LIMIT"=>"输出超出内存或尺寸限制，请明确选择较低倍率。","ASSET_IO" or "ASSET_DECODE"=>"素材读取失败，请检查图片路径或格式。",
        "ASSET_CHANGED"=>"原素材已变化，请重新载入后再导出。","PROJECT_INVALID" or "OBJECT_ORDER"=>"工程数据不完整或存在不合法引用，请检查工程。",
        "SOURCE"=>"请指定完整原图素材。","DUPLICATE_ID"=>"对象编号为空或重复，请换一个编号。",_=>"操作未完成，请检查输入与工程状态。"
    };
    public static string Message(Exception error)=>error is EditorException ex?(ex.Code=="LAYER_RELATION_CONFLICT"?ex.Message:ForError(ex.Code))+"（"+ex.Code+"）":error is FormatException?"输入格式不正确，请检查数值。":"操作未完成："+error.Message;
}
