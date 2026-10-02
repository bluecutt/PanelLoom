using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
namespace ComicEditor.Rendering.Borders;
public sealed class StrokeGuard : IDisposable
{
    public Region Region { get; }=new();
    private readonly Dictionary<string,Region> opaqueCache=new(StringComparer.OrdinalIgnoreCase);
    public StrokeGuard() { Region.MakeEmpty(); }
    public void RemoveCoverage(JsonObject panel,Bitmap image,string sourcePath,double scale,CancellationToken token)
    {
        using var path=DrawingGeometry.Path(panel["polygon"]!,scale);
        if(panel.Flag("clearFrame",true)) { Region.Exclude(path); return; }
        if(!opaqueCache.TryGetValue(sourcePath,out var cached)) { cached=AlphaCoverage.Opaque(image,token); opaqueCache[sourcePath]=cached; }
        using var opaque=cached.Clone(); var dest=DrawingGeometry.ImageRect(panel,image.Width,image.Height);
        using var matrix=new Matrix((float)(dest.Width*scale/image.Width),0,0,(float)(dest.Height*scale/image.Height),(float)(dest.X*scale),(float)(dest.Y*scale));
        opaque.Transform(matrix); opaque.Intersect(path); Region.Exclude(opaque);
    }
    public void Add(IEnumerable<Segment> segments,double scale,double width)
    {
        if(width<=0) return;
        using var path=new GraphicsPath(); using var pen=new Pen(Color.Black,(float)(width*scale+2));
        foreach(var segment in segments) { path.StartFigure(); path.AddLine((float)(segment.A.X*scale),(float)(segment.A.Y*scale),(float)(segment.B.X*scale),(float)(segment.B.Y*scale)); }
        if(path.PointCount>0) { path.Widen(pen); Region.Union(path); }
    }
    public void Dispose() { Region.Dispose(); foreach(var item in opaqueCache.Values) item.Dispose(); opaqueCache.Clear(); }
}
