using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace StealthPane.Services;

public static partial class ContentProtectionService
{
    public static bool EnableProtection(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null or 0)
        {
            return false;
        }

        if (SetWindowDisplayAffinity(handle.Value, WDA_EXCLUDEFROMCAPTURE))
        {
            return true;
        }

        return SetWindowDisplayAffinity(handle.Value, WDA_MONITOR);
    }

    #region Win32 API

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
    private const uint WDA_MONITOR = 0x00000001;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    #endregion
}
