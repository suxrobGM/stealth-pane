using System.Text;
using StealthPane.Models;
using StealthPane.Terminal;

namespace StealthPane.Services;

public static class CaptureInjectorService
{
    private static readonly byte[] Enter = "\r"u8.ToArray();

    public static void CaptureAndInject(PtyService pty, CliProviderConfig provider, CaptureSettings settings)
    {
        var imagePath = ScreenCaptureService.Capture(settings)
            .Replace('\\', '/');

        var prompt = provider.ImageMode switch
        {
            ImageInputMode.FilePath =>
                $"{settings.SystemPrompt.Trim()} See the screenshot: {imagePath}",
            ImageInputMode.Base64 =>
                $"{settings.SystemPrompt.Trim()} [base64:{Convert.ToBase64String(File.ReadAllBytes(imagePath))}]",
            _ =>
                $"{settings.SystemPrompt.Trim()} See the screenshot: {imagePath}"
        };

        pty.Write(Encoding.UTF8.GetBytes(prompt));

        // Delay Enter so the CLI's input handler (Ink) processes the text
        // in a separate event-loop tick before receiving the submit keystroke.
        Task.Delay(500).ContinueWith(_ => pty.Write(Enter));
    }
}
