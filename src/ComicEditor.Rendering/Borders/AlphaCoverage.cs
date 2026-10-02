using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ComicEditor.Core.Validation;
namespace ComicEditor.Rendering.Borders;
public static class AlphaCoverage
{
    public static Region Opaque(Bitmap source,CancellationToken token)
    {
        var region=new Region(); region.MakeEmpty();
        try
        {
            if((source.Flags&2)==0&&(source.PixelFormat&PixelFormat.Alpha)==0) { region.Union(new Rectangle(0,0,source.Width,source.Height)); return region; }
            using var copy=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(copy)) { g.CompositingMode=CompositingMode.SourceCopy; g.DrawImageUnscaled(source,0,0); }
            var data=copy.LockBits(new Rectangle(0,0,copy.Width,copy.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try
            {
                var bytes=new byte[checked(copy.Width*copy.Height*4)];
                for(var y=0;y<copy.Height;y++) { token.ThrowIfCancellationRequested(); Marshal.Copy(data.Scan0+y*data.Stride,bytes,y*copy.Width*4,copy.Width*4); }
                var opaque=true; for(var i=3;i<bytes.Length;i+=4) if(bytes[i]!=255) { opaque=false; break; }
                if(opaque) { region.Union(new Rectangle(0,0,copy.Width,copy.Height)); return region; }
                var runs=0;
                for(var y=0;y<copy.Height;y++)
                {
                    token.ThrowIfCancellationRequested(); var start=-1;
                    for(var x=0;x<=copy.Width;x++)
                    {
                        var covered=x<copy.Width&&bytes[(y*copy.Width+x)*4+3]==255;
                        if(covered&&start<0) start=x;
                        if(!covered&&start>=0) { if(++runs>500000) throw new EditorException("MEMORY_BUDGET","Alpha region is too complex for safe GDI allocation"); region.Union(new Rectangle(start,y,x-start,1)); start=-1; }
                    }
                }
            }
            finally { copy.UnlockBits(data); }
            return region;
        }
        catch { region.Dispose(); throw; }
    }
}
