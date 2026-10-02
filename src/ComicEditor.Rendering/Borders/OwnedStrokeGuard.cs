using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Geometry;
using ComicEditor.Core.Project;
namespace ComicEditor.Rendering.Borders;
public sealed class OwnedStrokeGuard : IDisposable
{
    private readonly Dictionary<string,StrokeGuard> owners=[];
    private readonly StrokeGuard legacy=new();
    private Region outer=new();
    private OwnedStrokeGuard(){outer.MakeEmpty();}
    public static OwnedStrokeGuard Build(ProjectDocument doc,RenderOptions options,IReadOnlyDictionary<string,Bitmap> assets)
    {
        var result=new OwnedStrokeGuard();var view=new ProjectView(doc);var scale=options.Scale==0?view.Scale:options.Scale;var panels=ObjectOrder.Ordered(doc,"panel").ToArray();
        try
        {
            if(view.BorderMode=="SingleLine")
            {
                var merged=SingleLineSegments.Build(doc).ToLookup(s=>s.OwnerId);
                foreach(var group in ObjectOrder.PaintGroups(doc,panels))
                {
                    foreach(var p in group)legacyRemove(p);
                    foreach(var p in group)result.legacy.Add(merged[p.Text("id")],scale,doc.Root["border"].Number("width",4));
                }
                void legacyRemove(JsonObject p)=>result.legacy.RemoveCoverage(p,assets["panel:"+p.Text("id")],p.Text("sourceImage"),scale,CancellationToken.None);
            }
            if(view.BorderMode=="SingleLine")for(var i=0;i<panels.Length;i++)
            {
                var p=panels[i];var guard=new StrokeGuard();result.owners[p.Text("id")]=guard;var points=PolygonOperations.Read(p["polygon"]!);
                var contributed=Enumerable.Range(0,points.Length).Where(e=>ProjectView.EdgeVisible(p,e)).Select(e=>new Segment(points[e],points[(e+1)%points.Length],p.Text("id"),p.Number("zIndex"),e)).ToArray();
                guard.Add(contributed,scale,doc.Root["border"].Number("width",4));
                // Each shared-edge contributor stays independent; a front exception skips only its named owner.
                for(var j=i+1;j<panels.Length;j++)
                {
                    var q=panels[j];if(!ObjectOrder.HasExplicit(doc,"panel")&&q.Number("zIndex")==p.Number("zIndex"))continue;
                    guard.RemoveCoverage(q,assets["panel:"+q.Text("id")],q.Text("sourceImage"),scale,CancellationToken.None);
                    var boundary=PolygonOperations.Read(q["polygon"]!);
                    foreach(var s in contributed)for(var edge=0;edge<boundary.Length;edge++)
                    {
                        if(!ProjectView.EdgeVisible(q,edge))continue;var a=boundary[edge];var b=boundary[(edge+1)%boundary.Length];
                        var dx=s.B.X-s.A.X;var dy=s.B.Y-s.A.Y;var length=dx*dx+dy*dy;
                        if(length<.0001||Math.Abs((a.X-s.A.X)*dy-(a.Y-s.A.Y)*dx)>.0001||Math.Abs((b.X-s.A.X)*dy-(b.Y-s.A.Y)*dx)>.0001)continue;
                        var ta=((a.X-s.A.X)*dx+(a.Y-s.A.Y)*dy)/length;var tb=((b.X-s.A.X)*dx+(b.Y-s.A.Y)*dy)/length;var lo=Math.Max(0,Math.Min(ta,tb));var hi=Math.Min(1,Math.Max(ta,tb));
                        if(hi>lo)guard.Add([s with{A=new(s.A.X+lo*dx,s.A.Y+lo*dy),B=new(s.A.X+hi*dx,s.A.Y+hi*dy)}],scale,doc.Root["border"].Number("width",4));
                    }
                }
            }
            return result;
        }
        catch{result.Dispose();throw;}
    }
    public Region ForBalloon(JsonObject balloon)
    {
        var region=legacy.Region.Clone();var rules=balloon["panelOcclusion"] as JsonObject;
        foreach(var (owner,guard) in owners)if(rules?[owner]?.GetValue<string>()=="front")
        {
            using var removable=guard.Region.Clone();
            foreach(var (other,contribution) in owners)if(other!=owner&&rules?[other]?.GetValue<string>()!="front")removable.Exclude(contribution.Region);
            region.Exclude(removable);
        }
        return region;
    }
    public void Dispose(){foreach(var guard in owners.Values)guard.Dispose();owners.Clear();outer.Dispose();legacy.Dispose();}
}
