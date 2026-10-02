using ComicEditor.Core.Project;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Validation;
using ComicEditor.Rendering.Borders;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace ComicEditor.Rendering;
public sealed class PageRenderer
{
    public PageRenderer() { LegacyGdiBootstrap.Initialize(); }
    public RenderedPage RenderBitmap(ProjectDocument doc,RenderOptions options,CancellationToken token=default)
        =>RenderCore(doc,options,null,0,token);
    public RenderedPage RenderPreview(ProjectDocument doc,RenderOptions options,PreviewAssetCache cache,long assetEpoch,CancellationToken token=default)
        =>RenderCore(doc,options,cache,assetEpoch,token);
    private sealed class PreviewSources : IDisposable
    {
        public readonly Dictionary<string,BitmapLease> Leases=[];
        public void Dispose(){foreach(var lease in Leases.Values)lease.Dispose();Leases.Clear();}
        public AssetInspection[] Read(ProjectDocument doc,PreviewAssetCache cache,long epoch)
        {var view=new ProjectView(doc);return view.Panels.Select(p=>(Kind:"panel",Object:p)).Concat(view.Balloons.Where(b=>b.Flag("visible",true)).Select(b=>(Kind:"balloon",Object:b))).Select(pair=>{var path=ProjectPaths.Resolve(doc,pair.Object.Text("sourceImage"));if(!Leases.TryGetValue(path,out var lease))Leases[path]=lease=cache.Acquire(path,epoch);return new AssetInspection(pair.Kind,pair.Object.Text("id"),pair.Object.Text("sourceImage"),path,lease.Pixels.Width,lease.Pixels.Height,null,false,null);}).ToArray();}
    }
    private RenderedPage RenderCore(ProjectDocument doc,RenderOptions options,PreviewAssetCache? previewCache,long assetEpoch,CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); var issues=ProjectValidator.Validate(doc).ToList();
        if(issues.Any(i=>i.Severity=="error")) throw new EditorException("PROJECT_INVALID",string.Join(";",issues.Select(i=>i.Code+" "+i.Path)));
        var view=new ProjectView(doc); var scale=options.Scale==0?view.Scale:options.Scale;
        if(!double.IsFinite(scale)||scale<=0||!double.IsFinite(options.Padding)||options.Padding<0) throw new EditorException("RENDER_OPTIONS","Invalid scale or padding");
        var widthValue=Math.Round(view.Width*scale); var heightValue=Math.Round(view.Height*scale);
        if(widthValue<1||heightValue<1||widthValue>int.MaxValue||heightValue>int.MaxValue) throw new EditorException("MEMORY_BUDGET","Output dimensions exceed limits");
        var width=(int)widthValue; var height=(int)heightValue;
        var outputBytes=(double)width*height*8;
        if(options.MemoryLimitBytes<=0||outputBytes>options.MemoryLimitBytes) throw new EditorException("MEMORY_BUDGET","Output exceeds memory budget; choose a lower scale explicitly");
        var panels=ObjectOrder.Ordered(doc,"panel").ToArray();
        var balloons=ObjectOrder.Ordered(doc,"balloon").Where(b=>b.Flag("visible",true)).ToArray();
        using var previewSources=new PreviewSources();
        var assets=previewCache is null?new AssetResolver().Inspect(doc).Where(a=>a.Kind=="panel"||balloons.Any(b=>b.Text("id")==a.ObjectId)).ToArray():previewSources.Read(doc,previewCache,assetEpoch);
        foreach(var asset in assets.Where(a=>a.Error is not null)) throw new EditorException("ASSET_IO",asset.Kind+":"+asset.ObjectId+": "+asset.Error,3);
        var sourceBytes=assets.DistinctBy(a=>a.AbsolutePath,StringComparer.OrdinalIgnoreCase).Sum(a=>(double)a.Width!.Value*a.Height!.Value*8);
        if(outputBytes+sourceBytes>options.MemoryLimitBytes) throw new EditorException("MEMORY_BUDGET","Original-source decode and output exceed memory budget");
        if(width>65535||height>65535) throw new EditorException("DIMENSION_LIMIT","GDI+ dimension exceeds 65535");
        var map=assets.ToDictionary(a=>a.Kind+":"+a.ObjectId); using var cache=new AssetBitmapCache();
        Bitmap Get(AssetInspection asset)=>previewCache is null?cache.Get(asset):previewSources.Leases[asset.AbsolutePath].Pixels;
        var singleLine=view.BorderMode=="SingleLine";
        var segments=singleLine?SingleLineSegments.Build(doc).ToLookup(s=>s.OwnerId):null;
        using var guard=singleLine&&balloons.Any(b=>b.Text("clipPanelId").Length>0)?new StrokeGuard():null;
        var bitmap=new Bitmap(width,height,PixelFormat.Format24bppRgb); var complete=false;
        try
        {
            using(var graphics=Graphics.FromImage(bitmap))
            {
                graphics.Clear(ColorTranslator.FromHtml(doc.Root["canvas"].Text("background","#FFFFFF")));
                graphics.CompositingMode=CompositingMode.SourceOver; graphics.CompositingQuality=CompositingQuality.HighQuality;
                graphics.InterpolationMode=InterpolationMode.HighQualityBicubic; graphics.SmoothingMode=SmoothingMode.AntiAlias; graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
                var borderColor=ColorTranslator.FromHtml(doc.Root["border"].Text("color","#000000")); var borderWidth=doc.Root["border"].Number("width",4);
                if(doc.Root["objectOrder"] is not null||balloons.Any(b=>b["panelOcclusion"] is not null))
                    OcclusionComposer.Paint(graphics,doc,options,assets.ToDictionary(a=>a.Kind+":"+a.ObjectId,a=>Get(a)),token);
                else
                {
                foreach(var group in ObjectOrder.PaintGroups(doc,panels))
                {
                    foreach(var panel in group) { token.ThrowIfCancellationRequested(); var asset=map["panel:"+panel.Text("id")]; var image=Get(asset); guard?.RemoveCoverage(panel,image,asset.AbsolutePath,scale,token); PanelPainter.Paint(graphics,panel,image,scale); }
                    foreach(var panel in group)
                    {
                        if(!singleLine) LegacyBorders.Paint(graphics,panel,scale,borderColor,borderWidth);
                        else if(borderWidth>0)
                        {
                            using var pen=new Pen(borderColor,(float)(borderWidth*scale)); var owned=segments![panel.Text("id")];
                            foreach(var segment in owned) graphics.DrawLine(pen,new PointF((float)(segment.A.X*scale),(float)(segment.A.Y*scale)),new PointF((float)(segment.B.X*scale),(float)(segment.B.Y*scale)));
                            guard?.Add(owned,scale,borderWidth);
                        }
                    }
                }
                var panelMap=panels.ToDictionary(p=>p.Text("id"));
                foreach(var balloon in balloons) { token.ThrowIfCancellationRequested(); BalloonPainter.Paint(graphics,balloon,Get(map["balloon:"+balloon.Text("id")]),panelMap,scale,guard?.Region); }
                }
                if(doc.Root["outerBorder"].Flag("enabled",true)&&doc.Root["outerBorder"].Number("width",5)>0)
                {
                    var outer=doc.Root["outerBorder"].Number("width",5); using var pen=new Pen(borderColor,(float)(outer*scale)); var inset=(float)(outer/2*scale);
                    graphics.DrawRectangle(pen,inset,inset,(float)(width-2*(double)inset-1),(float)(height-2*(double)inset-1));
                }
            }
            token.ThrowIfCancellationRequested(); if(previewCache is null)cache.VerifySources(assets);
            if(previewCache is null)foreach(var q in QualityAssessment.Assess(doc,scale).Where(q=>q.Status=="upsampled")) issues.Add(new("warning","SOURCE_UPSAMPLED",q.ObjectId,"/sourceImage",$"Source pixels per output pixel: {q.SourcePixelsPerOutputPixel:0.###}"));
            if(options.ObjectId is {Length:>0} id)
            {
                var matches=panels.Concat(balloons).Where(p=>p.Text("id")==id).ToArray(); if(matches.Length!=1) throw new EditorException("OBJECT_NOT_FOUND","Object ID missing or ambiguous");
                var target=matches[0]; RectangleF bounds;
                if(target["polygon"] is not null) bounds=DrawingGeometry.Bounds(target["polygon"]!); else { var t=target["transform"]; var diagonal=Math.Sqrt(Math.Pow(t.Number("width",240),2)+Math.Pow(t.Number("height",150),2)); bounds=new((float)(t.Number("x",100)+t.Number("width",240)/2-diagonal/2),(float)(t.Number("y",100)+t.Number("height",150)/2-diagonal/2),(float)diagonal,(float)diagonal); }
                bounds.Inflate((float)options.Padding,(float)options.Padding); var crop=Rectangle.Intersect(new(0,0,width,height),Rectangle.FromLTRB((int)Math.Floor(bounds.Left*scale),(int)Math.Floor(bounds.Top*scale),(int)Math.Ceiling(bounds.Right*scale),(int)Math.Ceiling(bounds.Bottom*scale)));
                if(crop.Width<=0||crop.Height<=0) throw new EditorException("OBJECT_OUTSIDE","Object is outside page"); var cropped=bitmap.Clone(crop,PixelFormat.Format24bppRgb); bitmap.Dispose(); bitmap=cropped;
            }
            complete=true; return new(bitmap,issues);
        }
        finally { if(!complete) bitmap.Dispose(); }
    }
    public RenderResult Render(ProjectDocument doc,string outputPath,RenderOptions options,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested(); outputPath=Path.GetFullPath(outputPath); AtomicFile.CheckDestination(outputPath,options.Overwrite,options.ExpectedDestinationHash);
        using var rendered=RenderBitmap(doc,options,token); token.ThrowIfCancellationRequested();
        AtomicFile.Write(outputPath,options.Overwrite,options.ExpectedDestinationHash,stream=>rendered.Pixels.Save(stream,ImageFormat.Png),_=>token.ThrowIfCancellationRequested());
        return new(outputPath,rendered.Pixels.Width,rendered.Pixels.Height,AtomicFile.Hash(outputPath),rendered.Warnings);
    }
}
