using ComicEditor.Tests;
using ComicEditor.Core.Commands;
using ComicEditor.Desktop.Inspectors;
namespace ComicEditor.UiTests;
public sealed class InspectorCoverageCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("UiInspector.ContextInspector",()=>{ Assert.True(InspectorCatalog.Actions("panel").Contains("panel.vertex")); Assert.True(!InspectorCatalog.Actions("panel").Contains("balloon.clip")); Assert.True(InspectorCatalog.Actions("balloon").Contains("balloon.clip")); Assert.True(!InspectorCatalog.Actions("balloon").Contains("panel.vertex")); }),
        new("UiInspector.AllCapabilityFieldsHaveControl",()=>{ var vm=new ComicEditor.Desktop.ViewModels.EditorViewModel(CommandCases.Scene()); foreach(var kind in new[]{"page","panel","balloon"}) {vm.Select(kind,kind=="panel"?"A":kind=="balloon"?"X":null); var view=new InspectorView(vm,kind); foreach(var action in CommandRegistry.Actions.Where(a=>InspectorCatalog.Actions(kind).Contains(a.Name))) foreach(var field in action.AllowedArgs.Where(f=>f!="readingOrder"||kind=="panel")) Assert.True(view.Fields.ContainsKey(action.Name+":"+field),"Missing "+action.Name+":"+field); if(kind=="balloon")Assert.True(!view.Fields.ContainsKey("object.layer:readingOrder"),"Panel-only order must not be offered for balloons"); } }),
        new("UiInspector.AllActionsDiscoverable",()=>{ var all=new[]{"page","panel","balloon"}.SelectMany(InspectorCatalog.Actions).ToHashSet(); foreach(var action in CommandRegistry.Actions) Assert.True(all.Contains(action.Name),"Missing "+action.Name); }),
        new("UiInspector.StartupFitsWorkArea",()=>{ var window=new ComicEditor.Desktop.MainWindow(new ComicEditor.Desktop.ViewModels.EditorViewModel(CommandCases.Scene())); Assert.True(window.Width<=System.Windows.SystemParameters.WorkArea.Width&&window.Height<=System.Windows.SystemParameters.WorkArea.Height,"Window exceeds DPI-adjusted work area"); window.Close(); })
    ];
}
