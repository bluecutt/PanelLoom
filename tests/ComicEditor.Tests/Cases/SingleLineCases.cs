using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Rendering;
using ComicEditor.Rendering.Borders;
namespace ComicEditor.Tests;
public static class BorderScenes
{
    public static ProjectDocument Scene(params JsonObject[] panels)
    {
        var dir=TestFiles.Directory(); foreach(var pair in new[]{("gray",Color.FromArgb(150,150,150)),("white",Color.White),("blue",Color.Blue),("transparent",Color.Transparent),("red",Color.Red)})
        { using var image=new Bitmap(80,80,System.Drawing.Imaging.PixelFormat.Format32bppArgb); using(var g=Graphics.FromImage(image)) g.Clear(pair.Item2); image.Save(Path.Combine(dir,pair.Item1+".png")); }
        var root=new JsonObject{["format"]="ComicPanelEditorProject",["version"]=2,["assetBase"]=".",["canvas"]=new JsonObject{["width"]=160,["height"]=140,["background"]="#FFFFFF",["exportScale"]=1},["border"]=new JsonObject{["mode"]="SingleLine",["width"]=6},["outerBorder"]=new JsonObject{["enabled"]=false},["panels"]=new JsonArray(panels.Select(p=>(JsonNode)p.DeepClone()).ToArray()),["balloons"]=new JsonArray()};
        return new(root,Path.Combine(dir,"scene.json"));
    }
    public static JsonObject Panel(string id,string points,int z=10,string source="gray")=>new(){["id"]=id,["sourceImage"]=source+".png",["polygon"]=JsonNode.Parse(points),["zIndex"]=z,["readingOrder"]=z};
    public static JsonObject Balloon(string id,string clip,string source="white",int z=1000)=>new(){["id"]=id,["sourceImage"]=source+".png",["clipPanelId"]=clip,["visible"]=true,["zIndex"]=z,["transform"]=new JsonObject{["x"]=5,["y"]=5,["width"]=55,["height"]=55}};
    public static void StripSame(Bitmap before,Bitmap after,int x0,int y0,int x1,int y1)
    {
        for(var y=y0;y<=y1;y++) for(var x=x0;x<=x1;x++) if(before.GetPixel(x,y).R<150) Assert.Equal(before.GetPixel(x,y).ToArgb(),after.GetPixel(x,y).ToArgb());
    }
}
public sealed class SingleLineCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("SingleLine.PartialSharedAndTJunction",()=>{ var doc=BorderScenes.Scene(BorderScenes.Panel("A","[[0,0],[100,0],[100,50],[0,50]]",10),BorderScenes.Panel("B","[[0,50],[40,50],[40,100],[0,100]]",20),BorderScenes.Panel("C","[[40,50],[100,50],[100,100],[40,100]]",30)); var segments=SingleLineSegments.Build(doc).Where(s=>Math.Abs(s.A.Y-50)<0.001&&Math.Abs(s.B.Y-50)<0.001).ToArray(); Assert.Equal(2,segments.Length); Assert.Equal(100d,segments.Sum(s=>Math.Abs(s.B.X-s.A.X))); Assert.True(segments.All(s=>s.OwnerId!="A")); }),
        new("SingleLine.NearParallelNotMerged",()=>{ var doc=BorderScenes.Scene(BorderScenes.Panel("A","[[0,0],[100,0],[100,50],[0,50]]"),BorderScenes.Panel("B","[[0,56],[100,56],[100,100],[0,100]]")); Assert.Equal(8,SingleLineSegments.Build(doc).Count); }),
        new("SingleLine.NoWhiteUnderlay",()=>{ var doc=BorderScenes.Scene(BorderScenes.Panel("A","[[20,20],[80,20],[80,80],[20,80]]",source:"red")); using var page=new PageRenderer().RenderBitmap(doc,new()); Assert.Equal(Color.Red.ToArgb(),page.Pixels.GetPixel(50,25).ToArgb()); }),
        new("SingleLine.HiddenEdge",()=>{ var p=BorderScenes.Panel("A","[[20,20],[80,20],[80,80],[20,80]]",source:"red"); p["edgeVisibility"]=JsonNode.Parse("[false,true,true,true]"); var doc=BorderScenes.Scene(p); var artOnly=doc.DeepClone(); artOnly.Root["border"]!["width"]=0; using var reference=new PageRenderer().RenderBitmap(artOnly,new()); using var page=new PageRenderer().RenderBitmap(doc,new()); Assert.Equal(reference.Pixels.GetPixel(50,20).ToArgb(),page.Pixels.GetPixel(50,20).ToArgb()); Assert.Equal(Color.Black.ToArgb(),page.Pixels.GetPixel(80,50).ToArgb()); }),
        new("SingleLine.P10ExactGolden",()=>{ var doc=new ProjectStore().Load("artifacts/private-fixtures/P10SingleLine.json").Document; using var page=new PageRenderer().RenderBitmap(doc,new()); using var golden=new Bitmap("artifacts/private-fixtures/P10SingleLine.png"); Assert.Equal(0,Pixels.Differences(golden,page.Pixels)); })
    ];
}
