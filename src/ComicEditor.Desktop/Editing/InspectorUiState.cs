using System.Windows.Controls;
using ComicEditor.Desktop.Inspectors;
namespace ComicEditor.Desktop.Editing;
public sealed record InspectorUiState(IReadOnlyDictionary<string,bool> Expanded,double Offset,string? FocusField,int Caret,int SelectionLength)
{
    public static InspectorUiState Capture(InspectorView view)
    {
        var expanded=((StackPanel)view.Content).Children.OfType<Expander>().ToDictionary(e=>e.Tag.ToString()!,e=>e.IsExpanded,StringComparer.Ordinal);
        var focus=view.Fields.FirstOrDefault(p=>p.Value.IsKeyboardFocusWithin);
        var text=focus.Value as TextBox;
        return new(expanded,view.VerticalOffset,focus.Key,text?.SelectionStart??0,text?.SelectionLength??0);
    }
    public void Restore(InspectorView view)
    {
        foreach(var group in ((StackPanel)view.Content).Children.OfType<Expander>())if(Expanded.TryGetValue(group.Tag.ToString()!,out var open))group.IsExpanded=open;
        view.UpdateLayout();
        if(FocusField is not null&&view.Fields.TryGetValue(FocusField,out var field)&&field.IsVisible&&field.IsEnabled)
        {field.Focus();if(field is TextBox text){var at=Math.Min(Caret,text.Text.Length);text.Select(at,Math.Min(SelectionLength,text.Text.Length-at));}}
        view.ScrollToVerticalOffset(Offset);
    }
}
