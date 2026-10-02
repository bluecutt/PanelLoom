using System.Windows;
namespace ComicEditor.Desktop.Canvas;
public static class FocusCommandPolicy
{
    public static bool CanvasShortcutAllowed(IInputElement? element)=>element is CanvasView;
}
