namespace ComicEditor.Core.Validation;
public sealed record Issue(string Severity, string Code, string? ObjectId, string Path, string Message);
public sealed class EditorException(string code, string message, int exitCode = 2) : Exception(message)
{
    public string Code { get; } = code;
    public int ExitCode { get; } = exitCode;
}
