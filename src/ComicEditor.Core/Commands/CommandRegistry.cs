namespace ComicEditor.Core.Commands;
public sealed record ActionDefinition(string Name,string? Kind,string[] AllowedArgs);
public static class CommandRegistry
{
    public static IReadOnlyList<ActionDefinition> Actions { get; } = [
        new("panel.add","panel",["object"]),new("panel.remove","panel",["clipPolicy"]),
        new("balloon.add","balloon",["object"]),new("balloon.remove","balloon",[]),
        new("object.rename",null,["kind","newId","label"]),
        new("panel.polygon","panel",["points","edgeVisibility"]),new("panel.vertex","panel",["index","x","y"]),
        new("panel.bounds","panel",["x","y","width","height"]),new("panel.move","panel",["dx","dy","moveImage"]),
        new("panel.image","panel",["offsetX","offsetY","scale","fitMode","fitBiasX","fitBiasY"]),
        new("asset.replace",null,["kind","sourceImage","fitPolicy"]),new("panel.edge","panel",["index","visible"]),new("panel.snap","panel",["index","tolerance"]),
        new("page.border",null,["color","width","mode","outerEnabled","outerWidth"]),new("page.snap",null,["enabled","tolerance"]),new("page.canvas",null,["width","height","background","exportScale"]),
        new("object.layer",null,["kind","zIndex","readingOrder"]),new("object.reorder",null,["kind","direction"]),new("object.lock",null,["kind","locked"]),
        new("balloon.transform","balloon",["x","y","width","height","rotation","flipX","flipY","opacity","lockAspect"]),
        new("balloon.clip","balloon",["clipPanelId"]),new("balloon.panelOcclusion","balloon",["panelId","position","autoOrder"]),new("balloon.group","balloon",["groupId"]),new("balloon.visible","balloon",["visible"]),
        new("balloon.copy","balloon",["newId","dx","dy"]),new("balloon.move","balloon",["dx","dy","moveGroup"]),
        new("object.reset",null,["kind","scope"])
    ];
}
