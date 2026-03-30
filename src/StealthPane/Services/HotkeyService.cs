using System.Runtime.InteropServices;

namespace StealthPane.Services;

public sealed partial class HotkeyService : IDisposable
{
    private IntPtr hwnd;
    private const int HOTKEY_ID = 0x1;
    private Action? callback;
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
        if (registered && OperatingSystem.IsWindows())
        {
            UnregisterHotKey(hwnd, HOTKEY_ID);
            registered = false;
        }
    }

    private bool RegisterWindows(string hotkeyString, IntPtr windowHandle, Action callback)
    {
        hwnd = windowHandle;
        this.callback = callback;

        ParseHotkey(hotkeyString, out var modifiers, out var vk);
        if (RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, vk))
        {
            registered = true;
            return true;
        }

        return false;
    }

    public void HandleWindowMessage(uint msg, IntPtr wParam)
    {
        if (msg == 0x0312 && wParam == HOTKEY_ID)
        {
            callback?.Invoke();
        }
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
}
