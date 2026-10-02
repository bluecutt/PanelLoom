using System.Text.Json;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
namespace ComicEditor.Core.AppState;
public sealed class SettingsStore(string path)
{
    public long PreviewBudgetBytes(){var value=Read()["previewCacheMiB"]?.GetValue<double>()??256;if(!double.IsFinite(value)||value<=0||value>4096)throw new ArgumentOutOfRangeException("previewCacheMiB");return checked((long)(value*1024*1024));}
    public JsonObject Read()=>File.Exists(path)?JsonNode.Parse(File.ReadAllText(path))!.AsObject():new();
    public void Write(JsonObject settings)=>AtomicFile.Write(path,File.Exists(path),File.Exists(path)?AtomicFile.Hash(path):null,s=>JsonSerializer.Serialize(s,settings));
}
