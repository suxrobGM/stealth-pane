using System.Runtime.InteropServices;
using StealthPane.ScreenCapture.Models;
using StealthPane.ScreenCapture.Utilities;

namespace StealthPane.ScreenCapture.Services;

public sealed partial class ScreenCaptureService
{
    private static readonly string CapturesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");

    public string Capture(CaptureSettings settings)
    {
        Directory.CreateDirectory(CapturesDir);
        var filePath = Path.Combine(CapturesDir, $"capture_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png");

        switch (settings.Mode)
        {
            case CaptureMode.Window when settings.WindowHandle is not 0:
                CaptureWindow(filePath, settings.WindowHandle);
                break;
            case CaptureMode.Region when settings is { RegionWidth: > 0, RegionHeight: > 0 }:
                CaptureRect(filePath, settings.RegionX, settings.RegionY, settings.RegionWidth, settings.RegionHeight);
                break;
            default:
                CaptureRect(filePath, 0, 0, GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
                break;
        }

        return filePath;
    }

    private static void CaptureRect(string filePath, int x, int y, int width, int height)
    {
        var hdcScreen = GetDC(IntPtr.Zero);
        try
        {
            using var bmp = GdiBitmap.Create(hdcScreen, width, height);
            BitBlt(bmp.Hdc, 0, 0, width, height, hdcScreen, x, y, SRCCOPY);
            bmp.SaveAsPng(filePath);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    private static void CaptureWindow(string filePath, nint windowHandle)
    {
        if (!WindowEnumerationService.IsWindow(windowHandle))
        {
            return;
        }

        GetWindowRect(windowHandle, out var rect);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return;

        var hdcScreen = GetDC(IntPtr.Zero);
        try
        {
            using var bmp = GdiBitmap.Create(hdcScreen, width, height);
            if (!PrintWindow(windowHandle, bmp.Hdc, PW_RENDERFULLCONTENT))
            {
                BitBlt(bmp.Hdc, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);
            }

            bmp.SaveAsPng(filePath);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }
    }

    #region Win32 API

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const uint SRCCOPY = 0x00CC0020;
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [LibraryImport("user32.dll")] private static partial IntPtr GetDC(IntPtr hWnd);
    [LibraryImport("user32.dll")] private static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [LibraryImport("user32.dll")] private static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(nint hwnd, nint hdcBlt, uint nFlags);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BitBlt(IntPtr dst, int xD, int yD, int w, int h, IntPtr src, int xS, int yS, uint rop);

    #endregion
}
