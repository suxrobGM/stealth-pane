namespace StealthPane.Terminal;

public static class TerminalAssets
{
    private static string? AssetDir;

    public static string GetAssetDirectory()
    {
        if (AssetDir is not null && Directory.Exists(AssetDir))
        {
            return AssetDir;
        }

        AssetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        Directory.CreateDirectory(AssetDir);

        var assembly = typeof(TerminalAssets).Assembly;
        const string prefix = "StealthPane.Terminal.Assets.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(prefix))
            {
                continue;
            }

            var fileName = resourceName[prefix.Length..];
            var filePath = Path.Combine(AssetDir, fileName);

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var fileStream = File.Create(filePath);
            stream.CopyTo(fileStream);
        }

        return AssetDir;
    }

    public static string GetTerminalHtmlPath()
    {
        return Path.Combine(GetAssetDirectory(), "terminal.html");
    }
}
