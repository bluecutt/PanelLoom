using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Desktop.Canvas;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public sealed class UpgradeOrderCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeOrder.HitAndListFollowExplicitOrder",()=>
        {
            var doc=ComicEditor.Tests.UpgradeOrderCases.Scene();
            foreach(var p in doc.Root["panels"]!.AsArray())p!["polygon"]=JsonNode.Parse("[[20,20],[140,20],[140,120],[20,120]]");
            doc.Root["objectOrder"]=JsonNode.Parse("{\"panels\":[\"A\",\"C\",\"B\"]}");
            var vm=new EditorViewModel(doc);vm.Select("panel","A");
            Assert.Equal("B",HandleHitTest.Find(vm,EditMode.Image,new(100,50),2)?.Id);
            var window=new ComicEditor.Desktop.MainWindow(vm);window.Refresh();
            try{var list=(System.Windows.Controls.ListBox)window.FindName("PanelsList");Assert.Equal("A,C,B",string.Join(",",list.Items.OfType<ComicEditor.Desktop.ObjectRow>().Select(x=>x.Id)));}
            finally{window.Close();}
        });
    }
}
