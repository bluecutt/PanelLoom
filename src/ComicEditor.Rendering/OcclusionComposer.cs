using System.Drawing;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
using ComicEditor.Rendering.Borders;
namespace ComicEditor.Rendering;
public static class OcclusionComposer
{
    public static void Paint(Graphics g,ProjectDocument doc,RenderOptions options,IReadOnlyDictionary<string,Bitmap> assets,CancellationToken token)
    {
        var view=new ProjectView(doc);var scale=options.Scale==0?view.Scale:options.Scale;var panels=view.Panels.ToDictionary(p=>p.Text("id"));var balloons=view.Balloons.ToDictionary(b=>b.Text("id"));
        var partial=assets.ToDictionary(p=>p.Key,p=>HasPartialAlpha(p.Value));
        var color=ColorTranslator.FromHtml(doc.Root["border"].Text("color","#000000"));var width=doc.Root["border"].Number("width",4);var segments=SingleLineSegments.Build(doc).ToLookup(s=>s.OwnerId);
        using var guard=OwnedStrokeGuard.Build(doc,options,assets);
        foreach(var node in OcclusionOrder.Build(doc))
        {
            token.ThrowIfCancellationRequested();
            var key=(node.Kind=="balloon"?"balloon:":"panel:")+node.Id;
            var explicitBack=node.Kind=="panel-image"&&balloons.Values.Any(b=>b["panelOcclusion"]?[node.Id]?.GetValue<string>()=="back");
            g.CompositingQuality=explicitBack&&partial[key]?System.Drawing.Drawing2D.CompositingQuality.AssumeLinear:System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            if(node.Kind=="panel-image")PanelPainter.Paint(g,panels[node.Id],assets["panel:"+node.Id],scale);
            else if(node.Kind=="panel-border")
            {
                if(view.BorderMode!="SingleLine")LegacyBorders.Paint(g,panels[node.Id],scale,color,width);
                else if(width>0){using var pen=new Pen(color,(float)(width*scale));foreach(var s in segments[node.Id])g.DrawLine(pen,(float)(s.A.X*scale),(float)(s.A.Y*scale),(float)(s.B.X*scale),(float)(s.B.Y*scale));}
            }
            else{using var protectedLines=guard.ForBalloon(balloons[node.Id]);BalloonPainter.Paint(g,balloons[node.Id],assets["balloon:"+node.Id],panels,scale,protectedLines,balloons[node.Id]["panelOcclusion"] is not null);}
        }
    }
    private static bool HasPartialAlpha(Bitmap bitmap)
    {
        if(!System.Drawing.Image.IsAlphaPixelFormat(bitmap.PixelFormat))return false;
        var data=bitmap.LockBits(new(0,0,bitmap.Width,bitmap.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try{var row=new byte[bitmap.Width*4];for(var y=0;y<bitmap.Height;y++){System.Runtime.InteropServices.Marshal.Copy(data.Scan0+y*data.Stride,row,0,row.Length);for(var i=3;i<row.Length;i+=4)if(row[i]>0&&row[i]<255)return true;}return false;}
        finally{bitmap.UnlockBits(data);}
    }
}
