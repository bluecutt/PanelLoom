using System.Text.Json.Nodes;
namespace ComicEditor.Core.Project;
public static class ProjectDefaults
{
    public const double BorderWidth=4, OuterBorderWidth=5, FitBias=0, BalloonPosition=100;
    // Capture on initialization or first geometry edit, never mutate a raw read-only load.
    public static void CaptureInitial(JsonObject item,string kind)
    {
        if(kind=="panel")
        {
            item["initialPolygon"]??=item["polygon"]?.DeepClone();
            item["initialImageTransform"]??=item["imageTransform"]?.DeepClone()??new JsonObject{["scale"]=1};
        }
        else item["initialTransform"]??=item["transform"]?.DeepClone()??new JsonObject();
    }
    public static void CaptureInitial(ProjectDocument doc)
    {
        var view=new ProjectView(doc);
        foreach(var item in view.Panels) CaptureInitial(item,"panel");
        foreach(var item in view.Balloons) CaptureInitial(item,"balloon");
    }
}
