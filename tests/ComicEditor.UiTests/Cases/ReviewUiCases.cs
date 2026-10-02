using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComicEditor.Tests;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Desktop.Inspectors;
namespace ComicEditor.UiTests;
public sealed class ReviewUiCases : ITestSuite
{
    private static void Apply(InspectorView view,string field)
    {var control=(TextBox)view.Fields[field];control.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(control)!,Environment.TickCount,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});}
    public IEnumerable<TestCase> Cases()=>[
        new("UiReview.UnchangedInspectorFieldsPreserveSemantics",()=>{var doc=CommandCases.Scene();doc.Root["panels"]![0]!.AsObject().Remove("fitBiasX");doc.Root["panels"]![0]!.AsObject().Remove("fitBiasY");var vm=new EditorViewModel(doc);vm.Select("panel","A");var inspector=new InspectorView(vm,"panel");var window=new Window{Content=inspector};window.Show();try{((TextBox)inspector.Fields["panel.image:offsetX"]).Text="3";Apply(inspector,"panel.image:offsetX");Assert.Equal(0d,vm.Document.Root["panels"]![0].Number("fitBiasX"));Assert.Equal(0d,vm.Document.Root["panels"]![0].Number("fitBiasY"));Assert.True(!vm.Document.Root["panels"]![0]!.AsObject().ContainsKey("fitMode"),"Unedited fields must not be materialized");}finally{window.Close();}}),
        new("UiReview.InspectorEffectiveDefaults",()=>{var vm=new EditorViewModel(TestFiles.Neutral());var page=new InspectorView(vm,"page");Assert.Equal("4",((TextBox)page.Fields["page.border:width"]).Text);Assert.Equal("5",((TextBox)page.Fields["page.border:outerWidth"]).Text);var doc=CommandCases.Scene();doc.Root["balloons"]![0]!["transform"]!.AsObject().Remove("x");doc.Root["balloons"]![0]!["transform"]!.AsObject().Remove("y");vm=new(doc);vm.Select("balloon","X");var balloon=new InspectorView(vm,"balloon");Assert.Equal("100",((TextBox)balloon.Fields["balloon.transform:x"]).Text);Assert.Equal("100",((TextBox)balloon.Fields["balloon.transform:y"]).Text);}),
        new("UiReview.IndexSelectReloadsCoordinateAndEdge",()=>{var doc=CommandCases.Scene();doc.Root["panels"]![0]!["edgeVisibility"]=new JsonArray(true,false,true,true);var vm=new EditorViewModel(doc);vm.Select("panel","A");var view=new InspectorView(vm,"panel");var index=view.Fields["panel.vertex:index"];if(index is ComboBox cb)cb.SelectedIndex=1;else((TextBox)index).Text="1";Assert.Equal("140",((TextBox)view.Fields["panel.vertex:x"]).Text);var edge=view.Fields["panel.edge:index"];if(edge is ComboBox ec)ec.SelectedIndex=1;else((TextBox)edge).Text="1";Assert.True(((CheckBox)view.Fields["panel.edge:visible"]).IsChecked==false);}),
        new("UiReview.HitUsesReversePaintOrder",()=>{var doc=CommandCases.Scene();doc.Root["panels"]![1]!["polygon"]=doc.Root["panels"]![0]!["polygon"]!.DeepClone();var vm=new EditorViewModel(doc);Assert.Equal("B",HandleHitTest.Find(vm,EditMode.Image,new(70,40),1)!.Id);var balloon=doc.Root["balloons"]![0]!.DeepClone();balloon!["id"]="Z";doc.Root["balloons"]!.AsArray().Add(balloon);vm=new(doc);Assert.Equal("Z",HandleHitTest.Find(vm,EditMode.Balloon,new(45,45),1)!.Id);}),
        new("UiReview.ReturnDragToOriginNoCommit",()=>{foreach(var kind in new[]{DragKind.Panel,DragKind.Image,DragKind.Vertex,DragKind.Balloon}){var vm=new EditorViewModel(CommandCases.Scene());var before=vm.Document.Root.ToJsonString();var router=new InputRouter(vm);router.Begin(new(kind==DragKind.Balloon?"balloon":"panel",kind==DragKind.Balloon?"X":"A",kind,0),new(40,40));router.Update(new(55,60));router.Update(new(40,40));router.Commit();Assert.Equal(before,vm.Document.Root.ToJsonString());Assert.Equal(0L,vm.Revision);}})
    ];
}
