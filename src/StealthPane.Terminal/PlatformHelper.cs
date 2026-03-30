namespace StealthPane.Terminal;

public static class PlatformHelper
{
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMacOS => OperatingSystem.IsMacOS();

    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "stealthpane");

    public static string GetTempDirectory()
    {
        Directory.CreateDirectory(TempDir);
        return TempDir;
    }
}
