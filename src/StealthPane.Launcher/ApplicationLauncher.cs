using System.Diagnostics;

namespace StealthPane.Launcher;

/// <summary>
///     Handles launching the main application.
/// </summary>
internal static class ApplicationLauncher
{
    private const string ExtractionDir = LauncherConfiguration.ExtractionDirectory;

    /// <summary>
    ///     Extracts (if needed) and runs the main application.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to the main application.</param>
    /// <returns>The exit code from the main application.</returns>
    public static int Launch(string[] args)
    {
        // Clean up update script if it exists from a previous update
        var updateScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.cmd");
        if (File.Exists(updateScript))
        {
            try
            {
                File.Delete(updateScript);
            }
            catch
            {
                /* best effort */
            }
        }

        var mainExePath = Path.Combine(ExtractionDir, LauncherConfiguration.MainExecutableName);

        // Re-extract if directory is missing or main exe doesn't exist
        if (!File.Exists(mainExePath))
        {
            if (Directory.Exists(ExtractionDir))
            {
                Directory.Delete(ExtractionDir, true);
            }

            PrepareApplication(ExtractionDir);
        }

        // Verify the main executable exists
        if (!File.Exists(mainExePath))
        {
            Console.Error.WriteLine("Failed to find main executable.");
            return 1;
        }

        return RunApplication(mainExePath, ExtractionDir, args);
    }

    /// <summary>
    ///     Prepares the application by creating the extraction directory and extracting resources.
    /// </summary>
    /// <param name="extractionDir">The directory where resources will be extracted.</param>
    private static void PrepareApplication(string extractionDir)
    {
        // Create a directory if it doesn't exist
        Directory.CreateDirectory(extractionDir);

        // Extract all embedded resources
        EmbeddedResourceExtractor.ExtractResources(extractionDir);
    }


    /// <summary>
    ///     Runs the main application and waits for it to exit.
    /// </summary>
    /// <param name="exePath">Path to the main executable.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The exit code from the application.</returns>
    private static int RunApplication(string exePath, string workingDirectory, string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            Arguments = string.Join(" ", args)
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine("Failed to start main application.");
            return 1;
        }

        // Wait for the main app to exit
        process.WaitForExit();
        return process.ExitCode;
    }
}
