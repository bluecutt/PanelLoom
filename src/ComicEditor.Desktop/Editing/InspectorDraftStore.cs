using System.Globalization;
using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Desktop.ViewModels;
namespace ComicEditor.Desktop.Editing;

public sealed record DraftKey(string Kind, string? Id, string Group);
public enum DraftDecision { Apply, Discard, Cancel }

public sealed class InspectorDraftStore
{
    private readonly Dictionary<DraftKey, Dictionary<string, string>> drafts = [];
    private readonly Dictionary<DraftKey, (JsonObject Defaults, bool PatchOnly)> configurations = [];
    private bool pending;
    public bool HasPending => Volatile.Read(ref pending);
    public event Action? Changed;
    private static readonly HashSet<string> Flags = ["flipX", "flipY", "lockAspect", "moveGroup", "locked", "visible", "outerEnabled", "enabled", "moveImage", "autoOrder"];
    private static readonly HashSet<string> TextFields = ["kind", "label", "newId", "color", "background", "mode", "fitMode", "sourceImage", "fitPolicy", "clipPanelId", "groupId", "clipPolicy", "scope", "direction", "position", "panelId"];
    public static bool PatchOnly(string action) => action is "panel.image" or "page.canvas" or "page.border" or "balloon.transform" or "object.layer" or "object.rename";
    public void Configure(DraftKey key, JsonObject defaults)
    {
        if (!drafts.ContainsKey(key)) configurations[key] = ((JsonObject)defaults.DeepClone(), PatchOnly(key.Group));
    }
    public string? Raw(DraftKey key, string field) => drafts.TryGetValue(key, out var values) ? values.GetValueOrDefault(field) : null;
    public void Stage(DraftKey key, string field, JsonNode? value) => StageRaw(key, field, Display(value));
    public void StageRaw(DraftKey key, string field, string text)
    {
        if (!drafts.TryGetValue(key, out var fields)) drafts[key] = fields = [];
        if (configurations.TryGetValue(key, out var configuration) && Display(configuration.Defaults[field]) == text) fields.Remove(field);
        else fields[field] = text;
        if (fields.Count == 0) drafts.Remove(key);
        Notify();
    }
    public JsonObject Get(DraftKey key)
    {
        var args = new JsonObject();
        if (drafts.TryGetValue(key, out var fields)) foreach (var (name, text) in fields) args[name] = Parse(name, text);
        return args;
    }
    public ProjectDocument Preview(ProjectDocument document)
    {
        try { return new CommandProcessor().Plan(document, drafts.Keys.Select(OperationFor).ToArray()).After; }
        catch (EditorException) { return document; }
    }
    public PlannedChange CommitGroup(DraftKey key, EditorViewModel model) => Commit([key], model);
    public PlannedChange CommitAll(EditorViewModel model) => Commit(drafts.Keys.ToArray(), model);
    private PlannedChange Commit(IReadOnlyList<DraftKey> keys, EditorViewModel model)
    {
        // Draft groups describe the currently selected identity, so commit edits before changing that identity.
        var operations = keys.Select(OperationFor).Where(op => op.Args.Count > 0 || op.Op.EndsWith(".remove", StringComparison.Ordinal)).OrderBy(op=>op.Op=="object.rename"?1:0).ToArray();
        var result = model.CommitDraftOperations(operations);
        foreach (var key in keys) { drafts.Remove(key); configurations.Remove(key); }
        foreach(var op in operations.Where(o=>o.Op=="object.rename"&&o.Args.ContainsKey("newId")))RemapIdentity(op.Args.Text("kind"),op.TargetId!,op.Args.Text("newId"));
        Notify(); model.RefreshAfterDraft();
        return result;
    }
    public void RemapIdentity(string kind,string oldId,string newId)
    {
        if(oldId==newId)return;
        foreach(var key in drafts.Keys.Where(k=>k.Kind==kind&&k.Id==oldId).ToArray()){var next=key with{Id=newId};drafts[next]=drafts[key];drafts.Remove(key);}
        foreach(var key in configurations.Keys.Where(k=>k.Kind==kind&&k.Id==oldId).ToArray()){var next=key with{Id=newId};configurations[next]=configurations[key];configurations.Remove(key);}
        if(kind=="panel")
        {
            foreach(var fields in drafts.Values)foreach(var name in new[]{"clipPanelId","panelId"})if(fields.GetValueOrDefault(name)==oldId)fields[name]=newId;
            foreach(var configuration in configurations.Values)foreach(var name in new[]{"clipPanelId","panelId"})if(configuration.Defaults.Text(name)==oldId)configuration.Defaults[name]=newId;
        }
    }
    private Operation OperationFor(DraftKey key)
    {
        configurations.TryGetValue(key, out var configuration);
        var patch = configuration.Defaults is null ? PatchOnly(key.Group) : configuration.PatchOnly;
        var args = patch ? new JsonObject() : (JsonObject?)configuration.Defaults?.DeepClone() ?? new();
        foreach (var (name, node) in Get(key)) args[name] = node?.DeepClone();
        if (args.Count > 0 && configuration.Defaults?.ContainsKey("kind") == true) args["kind"] = configuration.Defaults["kind"]?.DeepClone();
        return new(key.Group, key.Kind == "page" || key.Group.EndsWith(".add", StringComparison.Ordinal) ? null : key.Id, args);
    }
    public void Discard(DraftKey key) { drafts.Remove(key); configurations.Remove(key); Notify(); }
    public void DiscardAll() { drafts.Clear(); configurations.Clear(); Notify(); }
    private void Notify() { Volatile.Write(ref pending, drafts.Count > 0); Changed?.Invoke(); }
    public static string Display(JsonNode? value) => value is null ? "" : value is JsonValue v && v.TryGetValue<string>(out var text) ? text : value.ToJsonString();
    public static JsonNode? Parse(string field, string text)
    {
        if (TextFields.Contains(field)) return JsonValue.Create(text);
        if (Flags.Contains(field))
        {
            if (bool.TryParse(text, out var flag)) return JsonValue.Create(flag);
            throw new EditorException("ARGS", "该开关需要选择开启或关闭：" + field);
        }
        if (field is "points" or "edgeVisibility" or "object")
        {
            try { return JsonNode.Parse(text); }
            catch (System.Text.Json.JsonException) { throw new EditorException("ARGS", "JSON 内容格式不正确：" + field); }
        }
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number)) return JsonValue.Create(number);
        throw new EditorException("ARGS", "请输入有效的有限数字：" + field);
    }
}
