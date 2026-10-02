using System.Text.Json.Nodes;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Project;

public static class ObjectOrder
{
    private static string Key(string kind) => kind switch { "panel" => "panels", "balloon" => "balloons", _ => throw new EditorException("KIND", "Expected panel or balloon") };
    public static bool HasExplicit(ProjectDocument doc, string kind) => doc.Root["objectOrder"]?[Key(kind)] is not null;
    public static IReadOnlyList<string> Read(ProjectDocument doc, string kind)
    {
        if (doc.Root["objectOrder"]?[Key(kind)] is {} order) return order.AsArray().Select(n => n!.GetValue<string>()).ToArray();
        return Legacy(Objects(doc, kind), kind).Select(o => o.Text("id")).ToArray();
    }
    public static IReadOnlyList<JsonObject> Ordered(ProjectDocument doc, string kind)
    {
        var map = Objects(doc, kind).ToDictionary(o => o.Text("id"), StringComparer.Ordinal);
        return Read(doc, kind).Select(id => map.TryGetValue(id, out var item) ? item : throw new EditorException("OBJECT_ORDER", "Order references missing object: " + id)).ToArray();
    }
    public static IEnumerable<IEnumerable<JsonObject>> PaintGroups(ProjectDocument doc, IReadOnlyList<JsonObject> panels) =>
        HasExplicit(doc, "panel") ? panels.Select(p => (IEnumerable<JsonObject>)new[] { p }) : panels.GroupBy(p => p.Number("zIndex"));
    internal static bool Reorder(ProjectDocument doc, string kind, string id, string direction)
    {
        if (direction is not ("up" or "down")) throw new EditorException("ARGS", "direction must be up or down");
        var order = Read(doc, kind).ToList(); var index = order.IndexOf(id);
        if (index < 0) throw new EditorException("OBJECT_NOT_FOUND", "Order target missing");
        var next = index + (direction == "up" ? 1 : -1);
        if (next < 0 || next >= order.Count) return false;
        (order[index], order[next]) = (order[next], order[index]); Write(doc, kind, order); return true;
    }
    internal static void Add(ProjectDocument doc, string kind, string id)
    { if (HasExplicit(doc, kind)) doc.Root["objectOrder"]![Key(kind)]!.AsArray().Add(id); }
    internal static void MoveTo(ProjectDocument doc, string kind, string id, int index)
    {
        var order = Read(doc, kind).ToList(); var current = order.IndexOf(id);
        if (current < 0) throw new EditorException("OBJECT_NOT_FOUND", "Order target missing");
        if (index < 0 || index >= order.Count) throw new EditorException("INDEX", "Order index out of range");
        if (current == index) return;
        order.RemoveAt(current); order.Insert(index, id); Write(doc, kind, order);
    }
    internal static void Remove(ProjectDocument doc, string kind, string id)
    {
        if (!HasExplicit(doc, kind)) return;
        var array = doc.Root["objectOrder"]![Key(kind)]!.AsArray();
        var node = array.FirstOrDefault(n => n!.GetValue<string>() == id); if (node is not null) array.Remove(node);
    }
    internal static void Rename(ProjectDocument doc, string kind, string oldId, string newId)
    {
        if (!HasExplicit(doc, kind)) return;
        var array = doc.Root["objectOrder"]![Key(kind)]!.AsArray();
        for (var i = 0; i < array.Count; i++) if (array[i]!.GetValue<string>() == oldId) array[i] = newId;
    }
    internal static void Reinsert(ProjectDocument doc, string kind, JsonObject target)
    {
        if (!HasExplicit(doc, kind)) return;
        var objects = Objects(doc, kind).ToDictionary(o => o.Text("id"), StringComparer.Ordinal);
        var order = Read(doc, kind).Where(id => id != target.Text("id")).ToList();
        var at = order.FindIndex(id => Compare(objects[id], target, kind) > 0);
        order.Insert(at < 0 ? order.Count : at, target.Text("id")); Write(doc, kind, order);
    }
    private static void Write(ProjectDocument doc, string kind, IEnumerable<string> ids)
    { if (doc.Root["objectOrder"] is null) doc.Root["objectOrder"] = new JsonObject(); doc.Root["objectOrder"]![Key(kind)] = new JsonArray(ids.Select(id => (JsonNode)JsonValue.Create(id)!).ToArray()); }
    private static IEnumerable<JsonObject> Objects(ProjectDocument doc, string kind) => kind == "panel" ? new ProjectView(doc).Panels : new ProjectView(doc).Balloons;
    private static IEnumerable<JsonObject> Legacy(IEnumerable<JsonObject> objects, string kind) => kind == "panel"
        ? objects.OrderBy(o => o.Number("zIndex")).ThenBy(o => o.Number("readingOrder"))
        : objects.OrderBy(o => o.Number("zIndex", 1000)).ThenBy(o => o.Text("id"), StringComparer.CurrentCultureIgnoreCase);
    private static int Compare(JsonObject a, JsonObject b, string kind)
    { var z = a.Number("zIndex", kind == "panel" ? 0 : 1000).CompareTo(b.Number("zIndex", kind == "panel" ? 0 : 1000)); return z != 0 ? z : kind == "panel" ? a.Number("readingOrder").CompareTo(b.Number("readingOrder")) : StringComparer.CurrentCultureIgnoreCase.Compare(a.Text("id"), b.Text("id")); }
}
