namespace StealthCode.Terminal;

public interface IPtyProvider : IDisposable
{
    void Start(string command, string[] args, string workingDirectory, int cols, int rows);
    void Write(byte[] data);
    void Resize(int cols, int rows);
    event Action<byte[]>? OutputReceived;
    event Action<int>? ProcessExited;
}
