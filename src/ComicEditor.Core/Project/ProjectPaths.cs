namespace ComicEditor.Core.Project;
public static class ProjectPaths
{
    public static string AssetBase(ProjectDocument doc)
    {
        var projectDirectory = doc.SourcePath is null ? Environment.CurrentDirectory : Path.GetDirectoryName(Path.GetFullPath(doc.SourcePath))!;
        return Path.GetFullPath(doc.Root.Text("assetBase", "."), projectDirectory);
    }
    public static string Resolve(ProjectDocument doc, string reference) => Path.GetFullPath(reference, AssetBase(doc));
}
