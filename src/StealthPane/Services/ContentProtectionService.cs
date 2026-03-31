using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace StealthPane.Services;

/// <summary>
/// Provides functionality to enable content protection on the application window to prevent screen capture and recording.
/// It supports both Windows and macOS platforms, using platform-specific APIs to achieve the protection.
/// </summary>
public static partial class ContentProtectionService
{
    public static bool EnableProtection(Window window)
    {
        if (OperatingSystem.IsWindows())
        {
            return EnableWindowsProtection(window);
        }

        if (OperatingSystem.IsMacOS())
        {
            return EnableMacOsProtection(window);
        }

        return false;
    }

    private static bool EnableWindowsProtection(Window window)
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

    private static bool EnableMacOsProtection(Window window)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null or 0)
        {
            return false;
        }

        try
        {
            var sel = sel_registerName("setSharingType:"u8);
            objc_msgSend_Int64(handle.Value, sel, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #region Platform API Imports and Constants

    private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
    private const uint WDA_MONITOR = 0x00000001;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    [LibraryImport("libobjc.dylib", EntryPoint = "sel_registerName")]
    private static partial IntPtr sel_registerName(ReadOnlySpan<byte> name);

    [LibraryImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static partial void objc_msgSend_Int64(IntPtr receiver, IntPtr selector, long arg);

    #endregion
}
