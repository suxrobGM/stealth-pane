using System.Runtime.InteropServices;
using StealthPane.Models;
using StealthPane.Terminal;

namespace StealthPane.Services;

public sealed partial class ScreenCaptureService
{
    public static string Capture(CaptureSettings settings)
    {
        var tempDir = string.IsNullOrEmpty(settings.TempDirectory)
            ? PlatformHelper.GetTempDirectory()
            : settings.TempDirectory;

        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, $"capture_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.bmp");

        if (OperatingSystem.IsWindows())
        {
            CaptureWindows(filePath, settings);
        }
        else if (OperatingSystem.IsMacOS())
        {
            CaptureMacOs(filePath, settings);
        }

        return filePath;
    }

    private static void CaptureWindows(string filePath, CaptureSettings settings)
    {
        var hdcScreen = GetDC(IntPtr.Zero);
        var screenWidth = GetSystemMetrics(SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SM_CYSCREEN);

        int x = 0, y = 0, width = screenWidth, height = screenHeight;

        if (settings.Mode == CaptureMode.Region &&
            settings.RegionWidth > 0 && settings.RegionHeight > 0)
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
        SaveHBitmapAsBmp(hBitmap, width, height, filePath);

        DeleteObject(hBitmap);
        DeleteDC(hdcMem);
        ReleaseDC(IntPtr.Zero, hdcScreen);
    }

    private static void SaveHBitmapAsBmp(IntPtr hBitmap, int width, int height, string filePath)
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

        using var fs = File.Create(filePath);
        var fileSize = 14 + 40 + pixels.Length;

        fs.Write("BM"u8);
        fs.Write(BitConverter.GetBytes(fileSize));
        fs.Write(BitConverter.GetBytes(0));
        fs.Write(BitConverter.GetBytes(14 + 40));

        fs.Write(BitConverter.GetBytes(40));
        fs.Write(BitConverter.GetBytes(width));
        fs.Write(BitConverter.GetBytes(height));
        fs.Write(BitConverter.GetBytes((short)1));
        fs.Write(BitConverter.GetBytes((short)32));
        fs.Write(new byte[24]);

        var stride = width * 4;
        for (var row = height - 1; row >= 0; row--)
        {
            fs.Write(pixels, row * stride, stride);
        }
    }

    private static void CaptureMacOs(string filePath, CaptureSettings settings)
    {
        var args = settings.Mode switch
        {
            CaptureMode.Region when settings.RegionWidth > 0 =>
                $"-R{settings.RegionX},{settings.RegionY},{settings.RegionWidth},{settings.RegionHeight} -x {filePath}",
            CaptureMode.Interactive => $"-i -x {filePath}",
            _ => $"-x {filePath}"
        };

        var process = System.Diagnostics.Process.Start("screencapture", args);
        process?.WaitForExit(5000);
    }

    #region Platform API Constants and Imports

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const uint SRCCOPY = 0x00CC0020;

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

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetDC(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    private static partial int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    #endregion
}
