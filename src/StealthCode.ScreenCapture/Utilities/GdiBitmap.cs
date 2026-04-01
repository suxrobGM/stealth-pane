using System.Runtime.InteropServices;

namespace StealthCode.ScreenCapture.Utilities;

/// <summary>
///     RAII wrapper for a GDI compatible DC + bitmap pair.
///     Creates a memory DC with a compatible bitmap, and disposes both on cleanup.
/// </summary>
internal readonly partial struct GdiBitmap : IDisposable
{
    public IntPtr Hdc { get; }
    private IntPtr HBitmap { get; }
    private IntPtr HOld { get; }
    private int Width { get; }
    private int Height { get; }

    private GdiBitmap(IntPtr hdc, IntPtr hBitmap, IntPtr hOld, int width, int height)
    {
        Hdc = hdc;
        HBitmap = hBitmap;
        HOld = hOld;
        Width = width;
        Height = height;
    }

    public static GdiBitmap Create(IntPtr hdcSource, int width, int height)
    {
        var hdc = CreateCompatibleDC(hdcSource);
        var hBitmap = CreateCompatibleBitmap(hdcSource, width, height);
        var hOld = SelectObject(hdc, hBitmap);
        return new GdiBitmap(hdc, hBitmap, hOld, width, height);
    }

    public void SaveAsPng(string filePath)
    {
        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = Width,
                biHeight = -Height,
                biPlanes = 1,
                biBitCount = 32
            }
        };

        var pixels = new byte[Width * Height * 4];
        var hdcScreen = GetDC(IntPtr.Zero);
        GetDIBits(hdcScreen, HBitmap, 0, (uint)Height, pixels, ref bmi, 0);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        PngWriter.ConvertBgraToRgba(pixels);
        PngWriter.Write(filePath, Width, Height, pixels);
    }

    public void Dispose()
    {
        SelectObject(Hdc, HOld);
        DeleteObject(HBitmap);
        DeleteDC(Hdc);
    }

    #region Win32 API

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize, biSizeImage, biClrUsed, biClrImportant;
        public int biWidth, biHeight, biXPelsPerMeter, biYPelsPerMeter;
        public ushort biPlanes, biBitCount;
        public uint biCompression;
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

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(IntPtr obj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    private static partial int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, byte[] bits,
        ref BITMAPINFO bmi, uint usage);

    #endregion
}
