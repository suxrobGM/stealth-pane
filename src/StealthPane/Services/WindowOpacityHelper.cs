using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace StealthPane.Services;

internal static partial class WindowOpacityHelper
{
    private const int GWL_EXSTYLE = -20;
    private const nint WS_EX_LAYERED = 0x00080000;
    private const uint LWA_ALPHA = 0x02;

    public static void Apply(Window window, double opacity)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null or 0)
        {
            return;
        }

        var exStyle = GetWindowLongPtr(handle.Value, GWL_EXSTYLE);
        if ((exStyle & WS_EX_LAYERED) == 0)
        {
            SetWindowLongPtr(handle.Value, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);
        }

        byte alpha = (byte)(Math.Clamp(opacity, 0.0, 1.0) * 255);
        SetLayeredWindowAttributes(handle.Value, 0, alpha, LWA_ALPHA);
    }

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static partial nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);
}
