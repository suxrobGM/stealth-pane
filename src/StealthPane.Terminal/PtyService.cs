namespace StealthPane.Terminal;

public sealed class PtyService : IDisposable
{
    private IPtyProvider? provider;

    public event Action<byte[]>? OutputReceived;
    public event Action<int>? ProcessExited;

    public void Start(string command, string[] args, string workingDirectory, int cols, int rows)
    {
        Stop();

        provider = CreateProvider();
        provider.OutputReceived += data => OutputReceived?.Invoke(data);
        provider.ProcessExited += code => ProcessExited?.Invoke(code);
        provider.Start(command, args, workingDirectory, cols, rows);
    }

    public void Write(byte[] data)
    {
        provider?.Write(data);
    }

    public void Resize(int cols, int rows)
    {
        provider?.Resize(cols, rows);
    }

    public void Stop()
    {
        provider?.Dispose();
        provider = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private static IPtyProvider CreateProvider()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsPtyProvider();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacOsPtyProvider();
        }

        throw new PlatformNotSupportedException("Only Windows and macOS are supported");
    }
}
