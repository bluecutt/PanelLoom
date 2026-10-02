using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Geometry;
namespace ComicEditor.Rendering;
public static class DrawingGeometry
{
    public static PointF[] Points(JsonNode polygon,double scale=1)=>PolygonOperations.Read(polygon).Select(p=>new PointF((float)((float)p.X*scale),(float)((float)p.Y*scale))).ToArray();
    public static GraphicsPath Path(JsonNode polygon,double scale) { var path=new GraphicsPath(); path.AddPolygon(Points(polygon,scale)); return path; }
    public static RectangleF Bounds(JsonNode polygon)
    {
        var points=Points(polygon); var x=(double)points.Min(p=>p.X); var y=(double)points.Min(p=>p.Y); var right=(double)points.Max(p=>p.X); var bottom=(double)points.Max(p=>p.Y);
        return new((float)x,(float)y,(float)(right-x),(float)(bottom-y));
    }
    public static RectangleF ImageRect(JsonObject panel,int imageWidth,int imageHeight)
    {
        var bounds=Bounds(panel["initialPolygon"]??panel["polygon"]!); var aspect=imageWidth/(double)imageHeight; var target=bounds.Width/(double)bounds.Height;
        double width,height,x,y;
        if(panel.Text("fitMode","Cover")=="Contain")
        {
            if(aspect>target) { width=bounds.Width; height=width/aspect; } else { height=bounds.Height; width=height*aspect; }
            x=bounds.X+((double)bounds.Width-width)/2; y=bounds.Y+((double)bounds.Height-height)/2;
        }
        else if(aspect>target) { height=bounds.Height; width=height*aspect; var overflow=width-bounds.Width; x=bounds.X-overflow/2-overflow/2*panel.Number("fitBiasX"); y=bounds.Y; }
        else { width=bounds.Width; height=width/aspect; var overflow=height-bounds.Height; x=bounds.X; y=bounds.Y-overflow/2-overflow/2*panel.Number("fitBiasY"); }
        var basic=new RectangleF((float)x,(float)y,(float)width,(float)height);
        var scale=panel["imageTransform"].Number("scale",1); width=basic.Width*scale; height=basic.Height*scale;
        var centerX=basic.X+(double)basic.Width/2+panel["imageTransform"].Number("offsetX"); var centerY=basic.Y+(double)basic.Height/2+panel["imageTransform"].Number("offsetY");
        return new((float)(centerX-width/2),(float)(centerY-height/2),(float)width,(float)height);
    }
}
