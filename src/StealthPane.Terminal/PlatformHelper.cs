namespace StealthPane.Terminal;

public static class PlatformHelper
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    private static readonly string BaseDir = Path.Combine(AppContext.BaseDirectory, "stealthpane");

    public static string GetBaseDirectory()
    {
        Directory.CreateDirectory(BaseDir);
        return BaseDir;
    }
}
