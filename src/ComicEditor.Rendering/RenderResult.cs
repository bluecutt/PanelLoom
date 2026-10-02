using System.Drawing;
using ComicEditor.Core.Validation;
namespace ComicEditor.Rendering;
public sealed record RenderResult(string Path,int Width,int Height,string Sha256,IReadOnlyList<Issue> Warnings);
public sealed record RenderedPage(Bitmap Pixels,IReadOnlyList<Issue> Warnings) : IDisposable { public void Dispose()=>Pixels.Dispose(); }
