using System.Text;
using StealthPane.ScreenCapture.Models;
using StealthPane.ScreenCapture.Services;
using StealthPane.Terminal;

namespace StealthPane.Services;

/// <summary>
///     Captures a screenshot and injects it into the terminal.
/// </summary>
public sealed class CaptureInjectorService(
    SettingsService settingsService,
    CliProviderRegistry providerRegistry,
    ScreenCaptureService screenCaptureService,
    PtyService pty)
{
    private static readonly byte[] Enter = "\r"u8.ToArray();

    public void CaptureAndInject()
    {
        var provider = providerRegistry.GetActiveProvider();
        if (!provider.SupportsImageInput)
        {
            return;
        }

        var capture = settingsService.Settings.Capture;
        var imagePath = screenCaptureService.Capture(capture)
            .Replace('\\', '/');

        var prompt = provider.ImageMode switch
        {
            ImageInputMode.FilePath =>
                $"{capture.SystemPrompt.Trim()} See the screenshot: {imagePath}",
            ImageInputMode.Base64 =>
                $"{capture.SystemPrompt.Trim()} [base64:{Convert.ToBase64String(File.ReadAllBytes(imagePath))}]",
            _ =>
                $"{capture.SystemPrompt.Trim()} See the screenshot: {imagePath}"
        };

        pty.Write(Encoding.UTF8.GetBytes(prompt));
        Task.Delay(500).ContinueWith(_ => pty.Write(Enter));
    }
}
