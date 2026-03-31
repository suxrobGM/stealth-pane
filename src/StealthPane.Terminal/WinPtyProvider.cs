using System.Runtime.Versioning;
using Pty.Net;

namespace StealthPane.Terminal;

/// <summary>
/// PTY provider that uses winpty (via Quick.PtyNet) instead of ConPTY.
/// Winpty uses screen-scraping of a hidden console, which works on all
/// Windows versions including those with broken ConPTY rendering (Win11 25H2).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WinPtyProvider : IPtyProvider
{
    private IPtyConnection? connection;
    private Thread? readThread;
    private volatile bool disposed;

    public event Action<byte[]>? OutputReceived;
    public event Action<int>? ProcessExited;

    public void Start(string command, string[] args, string workingDirectory, int cols, int rows)
    {
        var options = new PtyOptions
        {
            App = command,
            CommandLine = args,
            Cwd = workingDirectory,
            Cols = cols,
            Rows = rows,
            ForceWinPty = true,
            Environment = new Dictionary<string, string>
            {
                ["TERM"] = "xterm-256color"
            }
        };

        connection = Task.Run(() => PtyProvider.SpawnAsync(options, CancellationToken.None))
            .GetAwaiter().GetResult();

        connection.ProcessExited += (_, e) => ProcessExited?.Invoke(e.ExitCode);

        readThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "WinPTY-Read"
        };
        readThread.Start();
    }

    public void Write(byte[] data)
    {
        try
        {
            connection?.WriterStream.Write(data, 0, data.Length);
            connection?.WriterStream.Flush();
        }
        catch when (disposed)
        {
        }
    }

    public void Resize(int cols, int rows)
    {
        connection?.Resize(cols, rows);
    }

    private void ReadLoop()
    {
        var buffer = new byte[4096];
        try
        {
            while (!disposed && connection is not null)
            {
                var bytesRead = connection.ReaderStream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0) break;

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                OutputReceived?.Invoke(data);
            }
        }
        catch when (disposed)
        {
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        connection?.Kill();
        connection?.Dispose();
    }
}
