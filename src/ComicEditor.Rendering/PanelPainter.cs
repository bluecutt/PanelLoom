using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
namespace ComicEditor.Rendering;
public static class PanelPainter
{
    public static void Paint(Graphics graphics,JsonObject panel,Bitmap image,double scale)
    {
        using var path=DrawingGeometry.Path(panel["polygon"]!,scale); var state=graphics.Save();
        try
        {
            graphics.SetClip(path); if(panel.Flag("clearFrame",true)) graphics.FillPath(Brushes.White,path);
            var dest=DrawingGeometry.ImageRect(panel,image.Width,image.Height); var scaled=new RectangleF((float)(dest.X*scale),(float)(dest.Y*scale),(float)(dest.Width*scale),(float)(dest.Height*scale));
            graphics.DrawImage(image,scaled,new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);
        }
        finally { graphics.Restore(state); }
    }
}
