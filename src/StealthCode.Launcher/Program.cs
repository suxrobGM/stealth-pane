namespace StealthCode.Launcher;

/// <summary>
///     Entry point for the Launcher application.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     Main entry point for the launcher.
    /// </summary>
    /// <param name="args">Command-line arguments to pass to the main application.</param>
    /// <returns>Exit code from the main application.</returns>
    public static int Main(string[] args)
    {
        try
        {
            return ApplicationLauncher.Launch(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to launch application: {ex.Message}");
            Console.Error.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
            return 1;
        }
    }
}
