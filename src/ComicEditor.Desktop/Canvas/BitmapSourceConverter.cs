using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace ComicEditor.Desktop.Canvas;
public static class BitmapSourceConverter
{
    public static BitmapSource Freeze(Bitmap bitmap)
    {
        var data=bitmap.LockBits(new(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        try{var stride=(bitmap.Width*3+3)&~3;var bytes=new byte[stride*bitmap.Height];for(var y=0;y<bitmap.Height;y++)Marshal.Copy(data.Scan0+y*data.Stride,bytes,y*stride,bitmap.Width*3);var source=BitmapSource.Create(bitmap.Width,bitmap.Height,96,96,PixelFormats.Bgr24,null,bytes,stride);source.Freeze();return source;}
        finally{bitmap.UnlockBits(data);}
    }
}
