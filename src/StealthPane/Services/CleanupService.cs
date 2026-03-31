using StealthPane.Terminal;

namespace StealthPane.Services;

/// <summary>
/// Periodically cleans up old temporary files created by the capture service to prevent disk bloat.
/// It looks for files with a "capture_" prefix and deletes those older than a specified age (default 30 minutes).
/// The cleanup runs every 5 minutes.
/// </summary>
public sealed class CleanupService : IDisposable
{
    private Timer? timer;
    private int cleanupMinutes = 30;
    private string tempDirectory = "";

    public void Start(string tempDirectory, int cleanupMinutes)
    {
        this.tempDirectory = string.IsNullOrEmpty(tempDirectory)
            ? PlatformHelper.GetTempDirectory()
            : tempDirectory;
        this.cleanupMinutes = cleanupMinutes;

        timer?.Dispose();
        timer = new Timer(Cleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private void Cleanup(object? state)
    {
        if (!Directory.Exists(tempDirectory))
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddMinutes(-cleanupMinutes);

        try
        {
            foreach (var file in Directory.GetFiles(tempDirectory, "capture_*.*"))
            {
                if (File.GetCreationTimeUtc(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}
