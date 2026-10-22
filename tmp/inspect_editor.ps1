Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class EditorWindowNative {
 [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
'@
$editorProcess = Get-Process Unity | Where-Object { $_.MainWindowTitle -like '*JYU_GameJam_Smoking*' } | Select-Object -First 1
[EditorWindowNative]::ShowWindowAsync($editorProcess.MainWindowHandle, 9)
[EditorWindowNative]::SetForegroundWindow($editorProcess.MainWindowHandle)
Start-Sleep -Milliseconds 700
$screenBounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
$screenImage = New-Object System.Drawing.Bitmap($screenBounds.Width, $screenBounds.Height)
$screenGraphics = [System.Drawing.Graphics]::FromImage($screenImage)
$screenGraphics.CopyFromScreen($screenBounds.Location, [System.Drawing.Point]::Empty, $screenBounds.Size)
$screenImage.Save('D:/GitHub/JYU_GameJam_Smoking/tmp/editor-status.png')
$screenGraphics.Dispose()
$screenImage.Dispose()
