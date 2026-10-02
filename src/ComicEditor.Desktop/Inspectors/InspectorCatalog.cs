namespace ComicEditor.Desktop.Inspectors;
public static class InspectorCatalog
{
    public static string[] Actions(string kind)=>kind switch {
        "page"=>["page.canvas","page.border","page.snap","panel.add","balloon.add"],
        "panel"=>["panel.image","panel.bounds","panel.vertex","panel.edge","panel.snap","panel.move","panel.polygon","object.layer","object.reorder","object.lock","object.rename","asset.replace","object.reset","panel.remove"],
        "balloon"=>["balloon.transform","balloon.clip","balloon.panelOcclusion","balloon.group","balloon.move","balloon.visible","balloon.copy","object.layer","object.reorder","object.lock","object.rename","asset.replace","object.reset","balloon.remove"], _=>[] };
    public static string[] Fields(string kind)=>ComicEditor.Core.Commands.CommandRegistry.Actions.Where(a=>Actions(kind).Contains(a.Name)).SelectMany(a=>a.AllowedArgs.Select(f=>a.Name+":"+f)).ToArray();
}
