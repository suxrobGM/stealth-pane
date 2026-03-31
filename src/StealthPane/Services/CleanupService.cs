namespace StealthPane.Services;

public static class CleanupService
{
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
