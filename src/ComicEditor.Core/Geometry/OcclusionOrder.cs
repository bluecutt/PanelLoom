using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Geometry;
public sealed record OcclusionNode(string Kind, string Id);

public static class OcclusionOrder
{
    public static IReadOnlyList<OcclusionNode> Build(ProjectDocument document)
    {
        var graph = CreateGraph(document);
        var result = Sort(graph, out _);
        if (result.Count != graph.Nodes.Count) throw new EditorException("LAYER_RELATION_CONFLICT", "相对遮挡与现有层级冲突；请调整所选气泡的层级或覆盖关系，不会自动重排其他对象。");
        return result;
    }
    // Keep every other object's ordering edge, releasing only the selected balloon's slot.
    // Its ancestors/successors determine the complete legal insertion interval in one graph pass.
    internal static int PlaceSelectedBalloon(ProjectDocument document, string id)
    {
        try { _ = Build(document); return 0; }
        catch (EditorException ex) when (ex.Code == "LAYER_RELATION_CONFLICT") { }
        var graph = CreateGraph(document, id);
        if (Sort(graph, out var blocked).Count != graph.Nodes.Count)
        {
            var involved = string.Join("、", blocked.Select(n => (n.Kind == "balloon" ? "气泡 " : "分镜 ") + n.Id).Distinct().Take(8));
            throw new EditorException("LAYER_RELATION_CONFLICT", $"气泡 {id} 没有可用的层级位置：现有前后关系互相矛盾（涉及 {involved}）。请检查这些对象的相对分镜遮挡，恢复冲突关系后再试；其他对象未改动。");
        }
        var node = graph.Indexes[new OcclusionNode("balloon", id)];
        var after = Reachable(graph.Successors, node);
        var reverse = Enumerable.Range(0, graph.Nodes.Count).Select(_ => new HashSet<int>()).ToArray();
        for (var i = 0; i < graph.Nodes.Count; i++) foreach (var next in graph.Successors[i]) reverse[next].Add(i);
        var before = Reachable(reverse, node);
        var original = ObjectOrder.Read(document, "balloon").ToList(); var oldIndex = original.IndexOf(id);
        var others = original.Where(other => other != id).ToArray(); var lower = 0; var upper = others.Length;
        for (var i = 0; i < others.Length; i++)
        {
            if (!graph.Indexes.TryGetValue(new OcclusionNode("balloon", others[i]), out var other)) continue;
            if (before.Contains(other)) lower = Math.Max(lower, i + 1);
            if (after.Contains(other)) upper = Math.Min(upper, i);
        }
        var newIndex = Math.Clamp(oldIndex, lower, upper);
        ObjectOrder.MoveTo(document, "balloon", id, newIndex);
        _ = Build(document); // Final strict graph check also protects future changes to edge construction.
        return newIndex - oldIndex;
    }
    private static HashSet<int> Reachable(HashSet<int>[] edges, int start)
    {
        var seen = new HashSet<int>(); var pending = new Stack<int>(); pending.Push(start);
        while (pending.Count > 0) foreach (var next in edges[pending.Pop()]) if (seen.Add(next)) pending.Push(next);
        return seen;
    }
    private sealed record Graph(List<OcclusionNode> Nodes, Dictionary<OcclusionNode, int> Indexes, HashSet<int>[] Successors, int[] Indegrees);
    private static Graph CreateGraph(ProjectDocument document, string? movableBalloonId = null)
    {
        var panels = ObjectOrder.Ordered(document, "panel");
        var balloons = ObjectOrder.Ordered(document, "balloon").Where(b => b.Flag("visible", true)).ToArray();
        var nodes = new List<OcclusionNode>();
        foreach (var group in ObjectOrder.PaintGroups(document, panels))
        {
            var members = group.ToArray();
            nodes.AddRange(members.Select(p => new OcclusionNode("panel-image", p.Text("id"))));
            nodes.AddRange(members.Select(p => new OcclusionNode("panel-border", p.Text("id"))));
        }
        var panelCount = nodes.Count;
        nodes.AddRange(balloons.Select(b => new OcclusionNode("balloon", b.Text("id"))));
        var indexes = nodes.Select((node, index) => (node, index)).ToDictionary(x => x.node, x => x.index);
        var successors = Enumerable.Range(0, nodes.Count).Select(_ => new HashSet<int>()).ToArray();
        var indegrees = new int[nodes.Count];
        void Edge(OcclusionNode before, OcclusionNode after)
        {
            var a = indexes[before]; var b = indexes[after];
            if (successors[a].Add(b)) indegrees[b]++;
        }
        for (var i = 1; i < panelCount; i++) Edge(nodes[i - 1], nodes[i]);
        var fixedBalloons = nodes.Skip(panelCount).Where(n => n.Id != movableBalloonId).ToArray();
        for (var i = 1; i < fixedBalloons.Length; i++) Edge(fixedBalloons[i - 1], fixedBalloons[i]);
        foreach (var balloon in balloons)
        {
            var node = new OcclusionNode("balloon", balloon.Text("id"));
            var rules = balloon["panelOcclusion"] as JsonObject;
            var bounds = BalloonBounds(balloon);
            var clip = balloon.Text("clipPanelId");
            if (clip.Length > 0) bounds = Intersection(bounds, PolygonOperations.Bounds(PolygonOperations.Read(panels.First(p => p.Text("id") == clip)["polygon"]!)));
            foreach (var panel in panels)
            {
                var id = panel.Text("id"); var image = new OcclusionNode("panel-image", id); var border = new OcclusionNode("panel-border", id);
                var position = rules?[id]?.GetValue<string>();
                if (position == "front") { Edge(image, node); Edge(border, node); }
                else if (position == "back") { Edge(node, image); Edge(node, border); }
                else if (Intersects(bounds, PolygonOperations.Bounds(PolygonOperations.Read(panel["polygon"]!)))) Edge(image, node);
            }
        }
        return new(nodes, indexes, successors, indegrees);
    }
    private static IReadOnlyList<OcclusionNode> Sort(Graph graph, out IReadOnlyList<OcclusionNode> blocked)
    {
        var nodes = graph.Nodes; var successors = graph.Successors; var indegrees = (int[])graph.Indegrees.Clone();
        var ready = new SortedSet<int>(Enumerable.Range(0, nodes.Count).Where(i => indegrees[i] == 0));
        var result = new List<OcclusionNode>();
        while (ready.Count > 0)
        {
            var index = ready.Min; ready.Remove(index); result.Add(nodes[index]);
            foreach (var next in successors[index]) if (--indegrees[next] == 0) ready.Add(next);
        }
        blocked = Enumerable.Range(0, nodes.Count).Where(i => indegrees[i] > 0).Select(i => nodes[i]).ToArray();
        return result;
    }
    private static Bounds BalloonBounds(JsonObject balloon)
    {
        var t = balloon["transform"]; var width = t.Number("width", 240); var height = t.Number("height", 150);
        var angle = t.Number("rotation") * Math.PI / 180; var c = Math.Abs(Math.Cos(angle)); var s = Math.Abs(Math.Sin(angle));
        var w = c * width + s * height; var h = s * width + c * height;
        return new(t.Number("x", 100) + width / 2 - w / 2, t.Number("y", 100) + height / 2 - h / 2, w, h);
    }
    private static bool Intersects(Bounds a, Bounds b) => a.Width > 0 && a.Height > 0 && a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
    private static Bounds Intersection(Bounds a, Bounds b)
    { var x = Math.Max(a.X, b.X); var y = Math.Max(a.Y, b.Y); return new(x, y, Math.Max(0, Math.Min(a.X + a.Width, b.X + b.Width) - x), Math.Max(0, Math.Min(a.Y + a.Height, b.Y + b.Height) - y)); }
}
