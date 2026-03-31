namespace StealthPane.ScreenCapture.Utilities;

public static class CleanupUtils
{
    /// <summary>
    ///     Cleans up old screenshot and audio capture files from previous sessions to free up disk space.
    /// </summary>
    public static void CleanupOldCaptures()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "captures");

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
