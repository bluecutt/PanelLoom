param([Parameter(Mandatory=$true)][string]$ScreenshotHelper,[Parameter(Mandatory=$true)][int]$WindowHandle)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class PreviewDesktopProbe {
 [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr value);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr value,out uint processId);
 [DllImport("user32.dll")] public static extern IntPtr GetThreadDesktop(uint thread);
 [DllImport("user32.dll")] public static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
 [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr desktop);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern bool GetUserObjectInformation(IntPtr handle,int index,StringBuilder name,int length,out int required);
 public static string Name(IntPtr handle) { var text=new StringBuilder(256); int n; return GetUserObjectInformation(handle,2,text,512,out n)?text.ToString():"unavailable"; }
}
'@
[PreviewDesktopProbe]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null
$probePid = [uint32]0
$threadId = [PreviewDesktopProbe]::GetWindowThreadProcessId([IntPtr]$WindowHandle,[ref]$probePid)
$targetDesktop = [PreviewDesktopProbe]::Name([PreviewDesktopProbe]::GetThreadDesktop($threadId))
$inputDesktop = [PreviewDesktopProbe]::OpenInputDesktop(0,$false,1)
try {
    [pscustomobject]@{ Visible=[PreviewDesktopProbe]::IsWindowVisible([IntPtr]$WindowHandle); ProcessId=$probePid; WindowDesktop=$targetDesktop; InputDesktop=[PreviewDesktopProbe]::Name($inputDesktop) } | Format-List
} finally { if($inputDesktop -ne [IntPtr]::Zero) { [PreviewDesktopProbe]::CloseDesktop($inputDesktop) | Out-Null } }
& $ScreenshotHelper -Mode temp -WindowHandle $WindowHandle
