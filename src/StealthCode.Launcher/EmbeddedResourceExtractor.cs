using System.IO.Compression;
using System.Reflection;

namespace StealthCode.Launcher;

/// <summary>
///     Handles extraction of embedded resources from the launcher assembly.
/// </summary>
internal static class EmbeddedResourceExtractor
{
    /// <summary>
    ///     Extracts all embedded resources to the target directory.
    /// </summary>
    /// <param name="targetDirectory">The directory where resources will be extracted.</param>
    public static void ExtractResources(string targetDirectory)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        Console.WriteLine($"Extracting {resourceNames.Length} files to {targetDirectory}...");

        foreach (var resourceName in resourceNames)
        {
            ExtractResource(assembly, resourceName, targetDirectory);
        }

        Console.WriteLine("Extraction complete.");
    }

    /// <summary>
    ///     Extracts a single embedded resource to the target directory.
    ///     Resources ending in .gz are automatically decompressed.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    /// <param name="resourceName">The name of the embedded resource.</param>
    /// <param name="targetDirectory">The directory where the file will be extracted.</param>
    private static void ExtractResource(Assembly assembly, string resourceName, string targetDirectory)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return;
        }

        // Convert forward slashes to platform path separators
        var fileName = resourceName.Replace('/', Path.DirectorySeparatorChar);

        // Check if the resource is GZip compressed
        var isCompressed = fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        if (isCompressed)
        {
            // Remove the .gz extension to get the original file name
            fileName = fileName[..^3];
        }

        var targetPath = Path.Combine(targetDirectory, fileName);

        // Create parent directory if it doesn't exist
        var parentDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        using var fileStream = File.Create(targetPath);
        if (isCompressed)
        {
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            gzipStream.CopyTo(fileStream);
        }
        else
        {
            stream.CopyTo(fileStream);
        }
    }
}
