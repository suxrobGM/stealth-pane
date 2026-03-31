using System.Runtime.InteropServices;

namespace StealthPane.Services;

public sealed record WindowInfo(nint Handle, string Title);

public static partial class WindowEnumerationService
{
    public static List<WindowInfo> GetVisibleWindows(nint excludeHandle = 0)
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var results = new List<WindowInfo>();
        EnumWindows((hWnd, _) =>
        {
            if (hWnd == excludeHandle || !IsWindowVisible(hWnd))
            {
                return true;
            }

            var length = GetWindowTextLengthW(hWnd);
            if (length == 0)
            {
                return true;
            }

            unsafe
            {
                var buffer = stackalloc char[length + 1];
                GetWindowTextW(hWnd, buffer, length + 1);
                var title = new string(buffer, 0, length);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    results.Add(new WindowInfo(hWnd, title));
                }
            }

            return true;
        }, IntPtr.Zero);

        return results;
    }

    #region Win32 API

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
    private static unsafe partial int GetWindowTextW(nint hWnd, char* lpString, int nMaxCount);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
    private static partial int GetWindowTextLengthW(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint hWnd);

    #endregion
}
