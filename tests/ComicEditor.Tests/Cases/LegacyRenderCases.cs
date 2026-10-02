using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ComicEditor.Core.Project;
using ComicEditor.Rendering;
namespace ComicEditor.Tests;
public static class Pixels
{
    [DllImport("msvcrt.dll",EntryPoint="_controlfp")]
    private static extern uint Control(uint next,uint mask);
    public static uint FpState()=>Control(0,0);
    public static int Differences(Bitmap a,Bitmap b)
    {
        Assert.Equal(a.Width,b.Width); Assert.Equal(a.Height,b.Height); var rect=new Rectangle(0,0,a.Width,a.Height);
        var x=a.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb); var y=b.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
        try { var rowA=new byte[a.Width*3]; var rowB=new byte[b.Width*3]; var count=0; for(var r=0;r<a.Height;r++) { Marshal.Copy(x.Scan0+r*x.Stride,rowA,0,rowA.Length); Marshal.Copy(y.Scan0+r*y.Stride,rowB,0,rowB.Length); for(var c=0;c<rowA.Length;c+=3) if(rowA[c]!=rowB[c]||rowA[c+1]!=rowB[c+1]||rowA[c+2]!=rowB[c+2]) count++; } return count; }
        finally { a.UnlockBits(x); b.UnlockBits(y); }
    }
}
public sealed class LegacyRenderCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        foreach(var name in new[]{"P03Legacy","P05Legacy","P09Legacy","P10Legacy"}) yield return new("LegacyRender.MatchesGolden."+name,()=>{
            var doc=new ProjectStore().Load("artifacts/private-fixtures/"+name+".json").Document;
            using var rendered=new PageRenderer().RenderBitmap(doc,new()); using var expected=new Bitmap("artifacts/private-fixtures/"+name+".png");
            var differences=Pixels.Differences(expected,rendered.Pixels);
            if(differences>0)
            {
                Console.WriteLine("NATIVE_FP "+Pixels.FpState().ToString("X"));
                foreach(System.Diagnostics.ProcessModule module in System.Diagnostics.Process.GetCurrentProcess().Modules) if(module.ModuleName.Equals("gdiplus.dll",StringComparison.OrdinalIgnoreCase)) Console.WriteLine("NATIVE_GDIPLUS "+module.FileName+" "+module.FileVersionInfo.FileVersion);
                rendered.Pixels.Save("artifacts/private-fixtures/"+name+"-native-diagnostic.png");
                var old=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("artifacts/private-fixtures/"+name+".geometry.json"))!.AsArray();
                foreach(var panel in new ProjectView(doc).Panels)
                {
                    var path=ProjectPaths.Resolve(doc,panel.Text("sourceImage")); using var image=new Bitmap(path); var rect=DrawingGeometry.ImageRect(panel,image.Width,image.Height);
                    var expectedRect=old.First(p=>p!.Text("id")==panel.Text("id"))!["rect"]!.AsArray(); var values=new[]{rect.X,rect.Y,rect.Width,rect.Height};
                    for(var i=0;i<4;i++) if(values[i]!=expectedRect[i]!.GetValue<double>()) Console.WriteLine($"RECT_DIFF {name} {panel.Text("id")} component={i} old={expectedRect[i]} new={values[i]:R}");
                    if((name=="P05Legacy"&&panel.Text("id")=="U01")||(name=="P09Legacy"&&panel.Text("id")=="A01"))
                    {
                        using var isolated=new Bitmap(expected.Width,expected.Height,PixelFormat.Format24bppRgb); using(var g=Graphics.FromImage(isolated)) { g.Clear(Color.White); g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceOver; g.CompositingQuality=System.Drawing.Drawing2D.CompositingQuality.HighQuality; g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic; g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality; PanelPainter.Paint(g,panel,image,new ProjectView(doc).Scale); }
                        using var oldIsolated=new Bitmap("artifacts/private-fixtures/"+name+"-"+panel.Text("id")+"-isolated.png"); Console.WriteLine($"ISOLATED_DIFF {name} {Pixels.Differences(oldIsolated,isolated)}");
                        using var frameworkCsharp=new Bitmap("artifacts/private-fixtures/"+name+"-"+panel.Text("id")+"-csharp-framework.png"); Console.WriteLine($"FRAMEWORK_CSHARP_DIFF {name} oldToCs={Pixels.Differences(oldIsolated,frameworkCsharp)} csToNative={Pixels.Differences(frameworkCsharp,isolated)}");
                    }
                    var bits=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                    try { var bytes=new byte[Math.Abs(bits.Stride)*image.Height]; Marshal.Copy(bits.Scan0,bytes,0,bytes.Length); var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)); var oldImage=old.First(p=>p!.Text("id")==panel.Text("id"))!; if(hash!=oldImage.Text("pixelHash")) Console.WriteLine($"DECODE_DIFF {name} {panel.Text("id")} old={oldImage["pixelFormat"]} {oldImage["flags"]} new={image.PixelFormat} {image.Flags}"); }
                    finally { image.UnlockBits(bits); }
                }
                var samples=0; for(var y=0;y<expected.Height&&samples<12;y++) for(var x=0;x<expected.Width&&samples<12;x++) if(expected.GetPixel(x,y).ToArgb()!=rendered.Pixels.GetPixel(x,y).ToArgb()) { samples++; Console.WriteLine($"DIFF {name} ({x},{y}) {expected.GetPixel(x,y)} => {rendered.Pixels.GetPixel(x,y)}"); }
            }
            Assert.Equal(0,differences);
        });
        yield return new("LegacyRender.P10Scale2",()=>{ var doc=new ProjectStore().Load("artifacts/private-fixtures/P10Legacy.json").Document; using var rendered=new PageRenderer().RenderBitmap(doc,new(2)); Assert.Equal(2048,rendered.Pixels.Width); Assert.Equal(3072,rendered.Pixels.Height); });
        yield return new("LegacyRender.P10Scale2_5",()=>{ var doc=new ProjectStore().Load("artifacts/private-fixtures/P10Legacy.json").Document; using var rendered=new PageRenderer().RenderBitmap(doc,new(2.5)); Assert.Equal(2560,rendered.Pixels.Width); Assert.Equal(3840,rendered.Pixels.Height); });
        yield return new("LegacyRender.NoHandlesInExport",()=>{ var doc=CommandCases.Scene(); var path=Path.Combine(TestFiles.Directory(),"render.png"); var result=new PageRenderer().Render(doc,path,new(2)); using var rendered=new PageRenderer().RenderBitmap(doc,new(2)); using var saved=new Bitmap(path); Assert.Equal(0,Pixels.Differences(rendered.Pixels,saved)); Assert.Equal(320,result.Width); });
    }
}
