using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Core.Commands;
public sealed record Operation(string Op,string? TargetId,JsonObject Args);
public sealed record Change(string ObjectId,string JsonPointer,JsonNode? Before,JsonNode? After);
public sealed record OperationOutcome(int OperationIndex,string Op,string? TargetId,string Code,JsonObject? Data=null);
public sealed record PlannedChange(ProjectDocument After,IReadOnlyList<Change> Changes,IReadOnlyList<Issue> Warnings,IReadOnlyList<OperationOutcome>? Outcomes=null);
