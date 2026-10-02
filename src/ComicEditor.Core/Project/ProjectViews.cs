using System.Text.Json.Nodes;
using System.Text.Json;
namespace ComicEditor.Core.Project;
public static class JsonRead
{
    public static double Number(this JsonNode? node, string key, double fallback = 0)
    {
        var value=node?[key]?.Deserialize<double>()??fallback;
        if(!double.IsFinite(value)) throw new FormatException("Expected finite number: "+key);
        return value;
    }
    public static string Text(this JsonNode? node, string key, string fallback = "") => node?[key]?.GetValue<string>() ?? fallback;
    public static bool Flag(this JsonNode? node, string key, bool fallback = false) => node?[key]?.GetValue<bool>() ?? fallback;
}
public sealed class ProjectView(ProjectDocument document)
{
    public JsonObject Root => document.Root;
    public IEnumerable<JsonObject> Panels => (Root["panels"]?.AsArray() ?? []).Select(n => n!.AsObject());
    public IEnumerable<JsonObject> Balloons => (Root["balloons"]?.AsArray() ?? []).Select(n => n!.AsObject());
    public int Width => (int)Root["canvas"].Number("width",1024);
    public int Height => (int)Root["canvas"].Number("height",1536);
    public double Scale => Root["canvas"].Number("exportScale",2);
    public string BorderMode => Root["border"].Text("mode","Legacy");
    public bool SnapEnabled => Root["edgeSnap"].Flag("enabled");
    public static bool EdgeVisible(JsonObject panel,int index) => panel["edgeVisibility"] is not JsonArray array || index >= array.Count || array[index]!.GetValue<bool>();
}
