using ComicEditor.Core.Assets;
using ComicEditor.Core.Project;
using ComicEditor.Core.Geometry;
namespace ComicEditor.Core.Validation;
public sealed record QualityItem(string Kind,string ObjectId,int? SourceWidth,int? SourceHeight,double? SourcePixelsPerOutputPixel,string Status);
public static class QualityAssessment
{
    public static IReadOnlyList<QualityItem> Assess(ProjectDocument doc,double scale)
    {
        var assets=new AssetResolver().Inspect(doc); var view=new ProjectView(doc); var result=new List<QualityItem>();
        foreach(var asset in assets)
        {
            if(asset.Error is not null) { result.Add(new(asset.Kind,asset.ObjectId,asset.Width,asset.Height,null,asset.Missing?"missing":"invalid")); continue; }
            double width,height;
            if(asset.Kind=="panel")
            {
                var panel=view.Panels.First(p=>p.Text("id")==asset.ObjectId); var bounds=PolygonOperations.Bounds(PolygonOperations.Read(panel["initialPolygon"]??panel["polygon"]!));
                var fit=panel.Text("fitMode","Cover")=="Contain"?Math.Min(bounds.Width/asset.Width!.Value,bounds.Height/asset.Height!.Value):Math.Max(bounds.Width/asset.Width!.Value,bounds.Height/asset.Height!.Value);
                width=asset.Width.Value*fit*panel["imageTransform"].Number("scale",1); height=asset.Height.Value*fit*panel["imageTransform"].Number("scale",1);
            }
            else { var balloon=view.Balloons.First(b=>b.Text("id")==asset.ObjectId); width=balloon["transform"].Number("width",240); height=balloon["transform"].Number("height",150); }
            var ratio=Math.Min(asset.Width!.Value/(width*scale),asset.Height!.Value/(height*scale)); result.Add(new(asset.Kind,asset.ObjectId,asset.Width,asset.Height,ratio,ratio<1?"upsampled":"native-or-downsampled"));
        }
        return result;
    }
}
