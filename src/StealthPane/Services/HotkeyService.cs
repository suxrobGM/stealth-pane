using System.Runtime.InteropServices;

namespace StealthPane.Services;

/// <summary>
/// Manages global hotkey registration and handling for the application.
/// Registers a Win32 global hotkey and subclasses the window procedure to
/// intercept WM_HOTKEY messages, invoking the callback when the hotkey is pressed.
/// </summary>
public sealed partial class HotkeyService : IDisposable
{
    private IntPtr hwnd;
    private IntPtr oldWndProc;
    private const int HOTKEY_ID = 0x1;
    private Action? callback;
    private WndProcDelegate? wndProcDelegate; // prevent GC collection
    private bool registered;

    public bool Register(string hotkeyString, IntPtr windowHandle, Action callback)
    {
        if (OperatingSystem.IsWindows())
        {
            return RegisterWindows(hotkeyString, windowHandle, callback);
        }

        return false;
    }

    public void Unregister()
    {
        if (!registered || !OperatingSystem.IsWindows())
        {
            return;
        }

        UnregisterHotKey(hwnd, HOTKEY_ID);

        if (oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(hwnd, GWLP_WNDPROC, oldWndProc);
            oldWndProc = IntPtr.Zero;
        }

        wndProcDelegate = null;
        registered = false;
    }

    private bool RegisterWindows(string hotkeyString, IntPtr windowHandle, Action callback)
    {
        hwnd = windowHandle;
        this.callback = callback;

        ParseHotkey(hotkeyString, out var modifiers, out var vk);
        if (!RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, vk))
        {
            return false;
        }

        oldWndProc = GetWindowLongPtr(windowHandle, GWLP_WNDPROC);
        wndProcDelegate = WndProc;
        SetWindowLongPtr(windowHandle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(wndProcDelegate));

        registered = true;
        return true;
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY && wParam == HOTKEY_ID)
        {
            callback?.Invoke();
        }

        return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
    }

    private static void ParseHotkey(string hotkey, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL": modifiers |= MOD_CONTROL; break;
                case "ALT": modifiers |= MOD_ALT; break;
                case "SHIFT": modifiers |= MOD_SHIFT; break;
                case "WIN": modifiers |= MOD_WIN; break;
                default:
                    if (part.Length == 1)
                    {
                        vk = (uint)char.ToUpper(part[0]);
                    }
                    break;
            }
        }
    }

    public void Dispose()
    {
        Unregister();
    }

    #region Win32 API Constants and Imports

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWLP_WNDPROC = -4;
    private const uint WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static partial IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    #endregion
}
