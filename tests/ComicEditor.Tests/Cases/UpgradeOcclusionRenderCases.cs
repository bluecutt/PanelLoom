using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Rendering;
namespace ComicEditor.Tests;
public sealed class UpgradeOcclusionRenderCases : ITestSuite
{
    public static ProjectDocument Scene(bool clear=false)
    {
        var p=BorderScenes.Panel("P","[[5,5],[155,5],[155,135],[5,135]]",10,"blue");
        var q=BorderScenes.Panel("Q","[[40,40],[110,40],[110,110],[40,110]]",20,"red");q["clearFrame"]=clear;
        var doc=BorderScenes.Scene(p,q);var bubble=BorderScenes.Balloon("X","P","white");bubble["transform"]=new JsonObject{["x"]=20,["y"]=20,["width"]=100,["height"]=100};doc.Root["balloons"]!.AsArray().Add(bubble);return doc;
    }
    private static ProjectDocument Rule(ProjectDocument d,string position)=>new CommandProcessor().Plan(d,[new("balloon.panelOcclusion","X",new(){["panelId"]="Q",["position"]=position})]).After;
    public IEnumerable<TestCase> Cases()
    {
        foreach(var scale in new[]{1d,2d,2.5d})yield return new("UpgradeOcclusionRender.FrontBackGuard."+scale,()=>{
            var doc=Scene();using var front=new PageRenderer().RenderBitmap(Rule(doc,"front"),new(scale));using var back=new PageRenderer().RenderBitmap(Rule(doc,"back"),new(scale));
            Assert.Equal(Color.White.ToArgb(),front.Pixels.GetPixel((int)(40*scale),(int)(75*scale)).ToArgb());
            Assert.Equal(Color.Black.ToArgb(),back.Pixels.GetPixel((int)(40*scale),(int)(75*scale)).ToArgb());
            Assert.Equal(Color.Red.ToArgb(),back.Pixels.GetPixel((int)(70*scale),(int)(70*scale)).ToArgb());
            using var original=new PageRenderer().RenderBitmap(doc,new(scale));for(var y=0;y<front.Pixels.Height;y++)for(var x=0;x<front.Pixels.Width;x++)if(x<34*scale||x>116*scale||y<34*scale||y>116*scale)Assert.Equal(original.Pixels.GetPixel(x,y).ToArgb(),front.Pixels.GetPixel(x,y).ToArgb());
        });
        yield return new("UpgradeOcclusionRender.RealAlphaAndHole",()=>{
            var doc=Scene();var path=ProjectPaths.Resolve(doc,"red.png");using(var b=new Bitmap(80,80,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(128,255,0,0));g.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;g.FillRectangle(Brushes.Transparent,20,20,15,15);}b.Save(path);}
            using var page=new PageRenderer().RenderBitmap(Rule(doc,"back"),new());var color=page.Pixels.GetPixel(85,85);Assert.True(Math.Abs(color.R-255)<=1&&Math.Abs(color.G-127)<=1&&Math.Abs(color.B-127)<=1,$"Actual {color}");Assert.Equal(Color.White.ToArgb(),page.Pixels.GetPixel(65,65).ToArgb());
        });
        yield return new("UpgradeOcclusionRender.SharedOwnerAndRotation",()=>{
            var doc=Scene();doc.Root["panels"]![0]!["polygon"]=JsonNode.Parse("[[5,5],[155,5],[155,40],[5,40]]");doc.Root["balloons"]![0]!["clipPanelId"]="";doc.Root["balloons"]![0]!["transform"]!["rotation"]=15;
            using var page=new PageRenderer().RenderBitmap(Rule(doc,"front"),new());Assert.Equal(Color.Black.ToArgb(),page.Pixels.GetPixel(75,40).ToArgb());
        });
        yield return new("UpgradeOcclusionRender.PrivateP10ApprovedRoi",()=>{
            var doc=new ProjectStore().Load("artifacts/private-fixtures/P10SingleLine.json").Document;
            var view=new ProjectView(doc);var balloon=view.Balloons.Single(b=>b.Text("id")=="C08");var panel=view.Panels.Single(p=>p.Text("id")=="A03");var clip=balloon.Text("clipPanelId");
            var revised=new CommandProcessor().Plan(doc,[new("balloon.panelOcclusion","C08",new(){["panelId"]="A03",["position"]="front"})]).After;
            Assert.Equal(clip,new ProjectView(revised).Balloons.Single(b=>b.Text("id")=="C08").Text("clipPanelId"));
            using var before=new PageRenderer().RenderBitmap(doc,new(1));using var after=new PageRenderer().RenderBitmap(revised,new(1));
            var box=DrawingGeometry.Bounds(panel["polygon"]!);box.Inflate((float)doc.Root["border"].Number("width",4)+3,(float)doc.Root["border"].Number("width",4)+3);
            var changed=0;var outside=0;for(var y=0;y<before.Pixels.Height;y++)for(var x=0;x<before.Pixels.Width;x++)if(before.Pixels.GetPixel(x,y).ToArgb()!=after.Pixels.GetPixel(x,y).ToArgb()){changed++;if(!box.Contains(x,y))outside++;}
            Assert.True(changed>0);Assert.Equal(0,outside);
            after.Pixels.Save("artifacts/usability-upgrade/P10-front-A03-preview.png");
        });
    }
}
