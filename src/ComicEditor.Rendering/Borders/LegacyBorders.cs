using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
namespace ComicEditor.Rendering.Borders;
public static class LegacyBorders
{
    public static void Paint(Graphics graphics,JsonObject panel,double scale,Color color,double width)
    {
        if(width<=0) return;
        var points=DrawingGeometry.Points(panel["polygon"]!,scale); var visible=Enumerable.Range(0,points.Length).Select(i=>ProjectView.EdgeVisible(panel,i)).ToArray();
        using var pen=new Pen(color,(float)(width*scale)) { LineJoin=LineJoin.Miter };
        if(visible.All(v=>v)) { using var white=new Pen(Color.White,(float)((width+7)*scale)) { LineJoin=LineJoin.Miter }; graphics.DrawPolygon(white,points); graphics.DrawPolygon(pen,points); }
        else for(var i=0;i<points.Length;i++) if(visible[i]) { using var white=new Pen(Color.White,(float)((width+7)*scale)); graphics.DrawLine(white,points[i],points[(i+1)%points.Length]); graphics.DrawLine(pen,points[i],points[(i+1)%points.Length]); }
    }
}
