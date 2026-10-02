using System.Text.Json.Nodes;
namespace ComicEditor.Core.Project;
public sealed record ProjectDocument(JsonObject Root, string? SourcePath)
{
    public ProjectDocument DeepClone() => new((JsonObject)Root.DeepClone(), SourcePath);
}
public sealed record LoadedProject(ProjectDocument Document, string AbsolutePath, string Sha256);
public sealed record SaveOptions(bool Overwrite = false, string? ExpectedDestinationHash = null);
