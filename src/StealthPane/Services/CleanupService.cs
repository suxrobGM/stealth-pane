using StealthPane.Terminal;

namespace StealthPane.Services;

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
        if (!Directory.Exists(tempDirectory)) return;

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
