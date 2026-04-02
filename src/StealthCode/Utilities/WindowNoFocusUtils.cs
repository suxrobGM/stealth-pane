using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace StealthCode.Utilities;

internal static partial class WindowNoFocusUtils
{
    /// <summary>
    /// Applies or removes the WS_EX_NOACTIVATE style to make the window non-focusable.
    /// </summary>
    public static bool Apply(Window window, bool noFocus)
    {
        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null or 0)
        {
            return false;
        }

        var exStyle = GetWindowLongPtr(handle.Value, GWL_EXSTYLE);

        if (noFocus)
        {
            SetWindowLongPtr(handle.Value, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }
        else
        {
            SetWindowLongPtr(handle.Value, GWL_EXSTYLE, exStyle & ~WS_EX_NOACTIVATE);
        }

        return true;
    }

    #region Win32 API Constants and Imports

    private const int GWL_EXSTYLE = -20;
    private const nint WS_EX_NOACTIVATE = 0x08000000;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static partial nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    #endregion
}
