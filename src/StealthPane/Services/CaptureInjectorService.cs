using System.Text;
using StealthPane.Models;
using StealthPane.Terminal;

namespace StealthPane.Services;

public sealed class CaptureInjectorService(ScreenCaptureService captureService)
{
    public void CaptureAndInject(PtyService pty, CliProviderConfig provider, CaptureSettings settings)
    {
        var imagePath = captureService.Capture(settings);

        var prompt = provider.ImageMode switch
        {
            ImageInputMode.FilePath =>
                $"{settings.SystemPrompt}\n\nSee the screenshot: {imagePath}\n",
            ImageInputMode.Base64 =>
                $"{settings.SystemPrompt}\n\n[base64:{Convert.ToBase64String(File.ReadAllBytes(imagePath))}]\n",
            _ =>
                $"{settings.SystemPrompt}\n\nSee the screenshot: {imagePath}\n"
        };

        pty.Write(Encoding.UTF8.GetBytes(prompt + "\n"));
    }
}
