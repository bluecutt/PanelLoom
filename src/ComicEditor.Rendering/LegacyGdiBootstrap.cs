using System.Runtime.InteropServices;
namespace ComicEditor.Rendering;
public static class LegacyGdiBootstrap
{
    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInput { public uint Version; public IntPtr Callback; public int SuppressBackgroundThread; public int SuppressExternalCodecs; }
    [DllImport("gdiplus.dll",ExactSpelling=true)]
    private static extern int GdiplusStartup(out UIntPtr token,ref StartupInput input,IntPtr output);
    [DllImport("gdiplus.dll",ExactSpelling=true)]
    private static extern void GdiplusShutdown(UIntPtr token);
    private static readonly object gate=new();
    private static UIntPtr token;
    public static void Initialize()
    {
        lock(gate)
        {
            if(token!=UIntPtr.Zero) return;
            var input=new StartupInput { Version=1 };
            var status=GdiplusStartup(out token,ref input,IntPtr.Zero);
            if(status!=0) throw new InvalidOperationException("Legacy GDI+ initialization failed: "+status);
            AppDomain.CurrentDomain.ProcessExit+=(_,_)=>{ lock(gate) { if(token!=UIntPtr.Zero) { GdiplusShutdown(token); token=UIntPtr.Zero; } } };
        }
    }
}
