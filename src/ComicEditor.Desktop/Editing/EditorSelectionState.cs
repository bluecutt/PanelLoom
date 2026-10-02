using ComicEditor.Core.Project;
using ComicEditor.Desktop.Canvas;
namespace ComicEditor.Desktop.Editing;

public sealed record EditorSelection(string Kind, string? Id, EditMode Tool, int? EdgeIndex, int? VertexIndex);
public sealed record EditToolOption(EditMode Value, string Label);

public sealed class EditorSelectionState
{
    private readonly Dictionary<string, string> remembered = new(StringComparer.Ordinal);
    private EditMode panelTool = EditMode.Image;
    private int pointCount;
    public EditorSelection Current { get; private set; } = new("page", null, EditMode.View, null, null);
    public void Select(ProjectDocument document, string kind, string? id, bool activateTool = true)
    {
        if (kind is not ("page" or "panel" or "balloon")) throw new ArgumentException("Unknown selection kind", nameof(kind));
        var selected = Objects(document, kind).FirstOrDefault(o => o.Text("id") == id);
        id = selected?.Text("id");
        var same = kind == Current.Kind && id == Current.Id;
        var tool = activateTool ? kind switch { "panel" => panelTool, "balloon" => EditMode.Balloon, _ => EditMode.View } : Current.Tool;
        pointCount = kind == "panel" ? selected?["polygon"]?.AsArray().Count ?? 0 : 0;
        Current = new(kind, id, tool, same ? ValidIndex(Current.EdgeIndex) : null, same ? ValidIndex(Current.VertexIndex) : null);
        if (id is not null) remembered[kind] = id;
    }
    public void ActivateKind(ProjectDocument document, string kind)
    {
        var objects = Objects(document, kind).ToArray();
        remembered.TryGetValue(kind, out var id);
        if (!objects.Any(o => o.Text("id") == id)) id = objects.FirstOrDefault()?.Text("id");
        Select(document, kind, id);
    }
    public void SetTool(EditMode tool)
    {
        if (!Enum.IsDefined(tool)) throw new ArgumentOutOfRangeException(nameof(tool));
        if (tool is EditMode.Image or EditMode.Panel or EditMode.Vertex or EditMode.FrameOnly or EditMode.Edge) panelTool = tool;
        Current = Current with { Tool = tool };
    }
    public void SelectEdge(int? index) => Current = Current with { EdgeIndex = ValidIndex(index) };
    public void SelectVertex(int? index) => Current = Current with { VertexIndex = ValidIndex(index) };
    public void Rename(string kind,string oldId,string newId)
    {
        if(remembered.GetValueOrDefault(kind)==oldId)remembered[kind]=newId;
        if(Current.Kind==kind&&Current.Id==oldId)Current=Current with{Id=newId};
    }
    public void Reconcile(ProjectDocument document)
    {
        foreach (var kind in remembered.Keys.ToArray())
            if (!Objects(document, kind).Any(o => o.Text("id") == remembered[kind])) remembered.Remove(kind);
        Select(document, Current.Kind, Current.Id, false);
    }
    private int? ValidIndex(int? index) => index is >= 0 && index < pointCount ? index : null;
    private static IEnumerable<System.Text.Json.Nodes.JsonObject> Objects(ProjectDocument document, string kind) =>
        kind == "panel" ? new ProjectView(document).Panels : kind == "balloon" ? new ProjectView(document).Balloons : [];
}
