using System.Runtime.InteropServices;
using StealthPane.ScreenCapture.Models;
using StealthPane.ScreenCapture.Utilities;

namespace StealthPane.ScreenCapture.Services;

public static partial class ScreenCaptureService
{
    public static string Capture(CaptureSettings settings)
    {
        var capturesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");

        Directory.CreateDirectory(capturesDir);
        var filePath = Path.Combine(capturesDir, $"capture_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png");

        CaptureWindows(filePath, settings);

        return filePath;
    }

    private static void CaptureWindows(string filePath, CaptureSettings settings)
    {
        if (settings is { Mode: CaptureMode.Window, WindowHandle: not 0 })
        {
            CaptureWindowWindows(filePath, settings.WindowHandle);
            return;
        }

        var hdcScreen = GetDC(IntPtr.Zero);
        var screenWidth = GetSystemMetrics(SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SM_CYSCREEN);

        int x = 0, y = 0, width = screenWidth, height = screenHeight;

        if (settings is { Mode: CaptureMode.Region, RegionWidth: > 0, RegionHeight: > 0 })
        {
            x = settings.RegionX;
            y = settings.RegionY;
            width = settings.RegionWidth;
            height = settings.RegionHeight;
        }

        var hdcMem = CreateCompatibleDC(hdcScreen);
        var hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        var hOld = SelectObject(hdcMem, hBitmap);

        BitBlt(hdcMem, 0, 0, width, height, hdcScreen, x, y, SRCCOPY);

        SelectObject(hdcMem, hOld);
        SaveHBitmapAsPng(hBitmap, width, height, filePath);

        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);
    }

    private static void CaptureWindowWindows(string filePath, nint windowHandle)
    {
        if (!WindowEnumerationService.IsWindow(windowHandle))
        {
            return;
        }

        GetWindowRect(windowHandle, out var rect);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        var hdcScreen = GetDC(IntPtr.Zero);
        var hdcMem = CreateCompatibleDC(hdcScreen);
        var hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
        var hOld = SelectObject(hdcMem, hBitmap);

        if (!PrintWindow(windowHandle, hdcMem, PW_RENDERFULLCONTENT))
        {
            // Fallback: BitBlt from screen at window position
            BitBlt(hdcMem, 0, 0, width, height, hdcScreen, rect.Left, rect.Top, SRCCOPY);
        }

        SelectObject(hdcMem, hOld);
        SaveHBitmapAsPng(hBitmap, width, height, filePath);

        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);
    }

    private static void SaveHBitmapAsPng(IntPtr hBitmap, int width, int height, string filePath)
    {
        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0
            }
        };

        var pixels = new byte[width * height * 4];
        var hdcScreen = GetDC(IntPtr.Zero);
        GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixels, ref bmi, 0);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        PngWriter.ConvertBgraToRgba(pixels);
        PngWriter.Write(filePath, width, height, pixels);
    }

    #region Win32 API Constants and Imports

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const uint SRCCOPY = 0x00CC0020;
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetDC(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(nint hwnd, nint hdcBlt, uint nFlags);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc,
        int xSrc, int ySrc, uint rop);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    private static partial int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, byte[] lpvBits,
        ref BITMAPINFO lpbi, uint uUsage);

    #endregion
}
