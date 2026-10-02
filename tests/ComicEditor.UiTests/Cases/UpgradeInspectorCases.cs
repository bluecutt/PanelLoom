using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ComicEditor.Desktop;
using ComicEditor.Desktop.Inspectors;
using ComicEditor.Desktop.ViewModels;
using ComicEditor.Tests;
namespace ComicEditor.UiTests;

public sealed class UpgradeInspectorCases : ITestSuite
{
    private static InspectorView Inspector(MainWindow window)=>(InspectorView)((Border)window.FindName("InspectorHost")).Child;
    private static void Layout(MainWindow window){window.UpdateLayout();window.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);window.UpdateLayout();}
    public IEnumerable<TestCase> Cases()
    {
        yield return new("UiUpgradeInspector.ApplyKeepsContext",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");vm.SelectEdge(2);
            var window=new MainWindow(vm);window.Show();Layout(window);
            try
            {
                var before=Inspector(window);
                var expanders=((StackPanel)before.Content).Children.OfType<Expander>().ToArray();
                foreach(var group in expanders.Take(3))group.IsExpanded=true;
                Layout(window);before.ScrollToVerticalOffset(125);Layout(window);
                var box=(TextBox)before.Fields["panel.image:offsetX"];box.Focus();box.Text="25";box.Select(1,1);
                Layout(window);var offset=before.VerticalOffset;
                UpgradeDraftCases.Apply(before,"原图取景");Layout(window);
                var after=Inspector(window);
                Assert.True(((StackPanel)after.Content).Children.OfType<Expander>().Take(3).All(x=>x.IsExpanded),"Apply must not collapse expanded groups");
                Assert.True(Math.Abs(offset-after.VerticalOffset)<=1,"Apply must keep scroll position");
                var updated=(TextBox)after.Fields["panel.image:offsetX"];
                Assert.True(updated.IsKeyboardFocusWithin,"Apply must preserve focus");
                Assert.Equal(1,updated.SelectionStart);Assert.Equal(1,updated.SelectionLength);
                Assert.Equal<int?>(2,vm.Selection.Current.EdgeIndex);
            }
            finally{vm.DiscardDrafts();vm.Undo();window.Close();}
        });
        yield return new("UiUpgradeInspector.ReadRefreshKeepsOtherDraftAndFocus",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");
            var window=new MainWindow(vm);window.Show();Layout(window);
            try
            {
                var before=Inspector(window);var group=((StackPanel)before.Content).Children.OfType<Expander>().First(x=>x.Header.ToString()=="层级");group.IsExpanded=true;
                var box=(TextBox)before.Fields["object.layer:zIndex"];box.BringIntoView();Layout(window);box.Focus();box.Text="7";box.Select(1,0);
                vm.Select("panel","A");Layout(window);
                var after=Inspector(window);var updated=(TextBox)after.Fields["object.layer:zIndex"];
                Assert.Equal("7",updated.Text);Assert.True(updated.IsKeyboardFocusWithin);
                Assert.True(((StackPanel)after.Content).Children.OfType<Expander>().First(x=>x.Header.ToString()=="层级").IsExpanded);
                Assert.True(vm.InputPending);
            }
            finally{vm.DiscardDrafts();window.Close();}
        });
        yield return new("UiUpgradeInspector.RebuildRestoresSeparateObjectState",()=>
        {
            var vm=new EditorViewModel(CommandCases.Scene());vm.Select("panel","A");
            var window=new MainWindow(vm);window.Show();Layout(window);
            try
            {
                var a=Inspector(window);
                var layer=((StackPanel)a.Content).Children.OfType<Expander>().First(x=>x.Header.ToString()=="层级");layer.IsExpanded=true;
                Layout(window);a.ScrollToVerticalOffset(100);Layout(window);var offset=a.VerticalOffset;
                vm.Select("panel","B");Layout(window);
                Assert.True(!((StackPanel)Inspector(window).Content).Children.OfType<Expander>().First(x=>x.Header.ToString()=="层级").IsExpanded);
                vm.Select("panel","A");Layout(window);
                var restored=Inspector(window);
                Assert.True(((StackPanel)restored.Content).Children.OfType<Expander>().First(x=>x.Header.ToString()=="层级").IsExpanded);
                Assert.True(Math.Abs(offset-restored.VerticalOffset)<=1);
                Assert.True(!vm.InputPending&&vm.Revision==0);
            }
            finally{window.Close();}
        });
    }
}
