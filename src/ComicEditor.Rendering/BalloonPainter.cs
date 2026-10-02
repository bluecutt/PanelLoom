using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
namespace ComicEditor.Rendering;
public static class BalloonPainter
{
    public static void Paint(Graphics graphics,JsonObject balloon,Bitmap image,IReadOnlyDictionary<string,JsonObject> panels,double scale,Region? guard=null,bool guardFreeBalloon=false)
    {
        if(!balloon.Flag("visible",true)) return;
        var state=graphics.Save(); GraphicsPath? path=null;
        try
        {
            if(panels.TryGetValue(balloon.Text("clipPanelId"),out var panel)) { path=DrawingGeometry.Path(panel["polygon"]!,scale); graphics.SetClip(path,CombineMode.Intersect); if(guard is not null) graphics.ExcludeClip(guard); }
            else if(guardFreeBalloon&&guard is not null)graphics.ExcludeClip(guard);
            var t=balloon["transform"]; var width=t.Number("width",240)*scale; var height=t.Number("height",150)*scale;
            graphics.TranslateTransform((float)((t.Number("x",100)+t.Number("width",240)/2)*scale),(float)((t.Number("y",100)+t.Number("height",150)/2)*scale));
            graphics.RotateTransform((float)t.Number("rotation")); graphics.ScaleTransform(t.Flag("flipX")?-1:1,t.Flag("flipY")?-1:1);
            using var attributes=new ImageAttributes(); var matrix=new ColorMatrix { Matrix33=(float)Math.Clamp(t.Number("opacity",1),0,1) }; attributes.SetColorMatrix(matrix,ColorMatrixFlag.Default,ColorAdjustType.Bitmap);
            var dest=new Rectangle((int)Math.Round(-width/2),(int)Math.Round(-height/2),Math.Max(1,(int)Math.Round(width)),Math.Max(1,(int)Math.Round(height)));
            graphics.DrawImage(image,dest,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);
        }
        finally { path?.Dispose(); graphics.Restore(state); }
    }
}
