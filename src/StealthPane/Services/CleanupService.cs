using StealthPane.Terminal;

namespace StealthPane.Services;

public static class CleanupService
{
    public static void CleanupOldCaptures(string? tempDirectory = null)
    {
        var dir = string.IsNullOrEmpty(tempDirectory)
            ? PlatformHelper.GetBaseDirectory()
            : tempDirectory;

        if (!Directory.Exists(dir))
        {
            return;
        }

        try
        {
            foreach (var file in Directory.GetFiles(dir, "capture_*.*"))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
