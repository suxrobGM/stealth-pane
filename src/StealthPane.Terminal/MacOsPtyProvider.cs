using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace StealthPane.Terminal;

[SupportedOSPlatform("macos")]
public sealed class MacOsPtyProvider : IPtyProvider
{
    private int masterFd = -1;
    private int childPid = -1;
    private Thread? readThread;
    private volatile bool disposed;

    public event Action<byte[]>? OutputReceived;
    public event Action<int>? ProcessExited;

    public void Start(string command, string[] args, string workingDirectory, int cols, int rows)
    {
        var winSize = new WinSize
        {
            ws_col = (ushort)cols,
            ws_row = (ushort)rows
        };

        var winSizePtr = Marshal.AllocHGlobal(Marshal.SizeOf<WinSize>());
        Marshal.StructureToPtr(winSize, winSizePtr, false);

        childPid = forkpty(out masterFd, IntPtr.Zero, IntPtr.Zero, winSizePtr);
        Marshal.FreeHGlobal(winSizePtr);

        if (childPid < 0)
        {
            throw new InvalidOperationException("forkpty failed");
        }

        if (childPid == 0)
        {
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                chdir(workingDirectory);
            }

            var argv = new string[args.Length + 2];
            argv[0] = command;
            Array.Copy(args, 0, argv, 1, args.Length);
            argv[^1] = null!;

            execvp(command, argv);
            _exit(1);
        }

        StartReadThread();
    }

    public void Write(byte[] data)
    {
        if (masterFd >= 0)
        {
            write(masterFd, data, (IntPtr)data.Length);
        }
    }

    public void Resize(int cols, int rows)
    {
        if (masterFd < 0)
        {
            return;
        }

        var winSize = new WinSize
        {
            ws_col = (ushort)cols,
            ws_row = (ushort)rows
        };

        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<WinSize>());
        Marshal.StructureToPtr(winSize, ptr, false);
        ioctl(masterFd, TIOCSWINSZ, ptr);
        Marshal.FreeHGlobal(ptr);
    }

    private void StartReadThread()
    {
        readThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "PTY-Read"
        };
        readThread.Start();
    }

    private void ReadLoop()
    {
        var buffer = new byte[4096];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

        try
        {
            while (!disposed)
            {
                var bytesRead = (int)read(masterFd, handle.AddrOfPinnedObject(), (IntPtr)buffer.Length);
                if (bytesRead <= 0)
                {
                    break;
                }

                var data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                OutputReceived?.Invoke(data);
            }
        }
        catch when (disposed)
        {
            // Expected during disposal
        }
        finally
        {
            handle.Free();
        }

        var status = 0;
        waitpid(childPid, ref status, 0);
        ProcessExited?.Invoke(status);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        if (masterFd >= 0)
        {
            close(masterFd);
            masterFd = -1;
        }

        if (childPid > 0)
        {
            kill(childPid, 9); // SIGKILL
        }
    }

    // --- P/Invoke declarations ---

    private const ulong TIOCSWINSZ = 0x80087467;

    [StructLayout(LayoutKind.Sequential)]
    private struct WinSize
    {
        public ushort ws_row;
        public ushort ws_col;
        public ushort ws_xpixel;
        public ushort ws_ypixel;
    }

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int forkpty(out int amaster, IntPtr name, IntPtr termp, IntPtr winp);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int execvp(string file, string[] argv);

    [DllImport("libSystem.dylib")]
    private static extern void _exit(int status);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int chdir(string path);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern IntPtr read(int fd, IntPtr buf, IntPtr count);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern IntPtr write(int fd, byte[] buf, IntPtr count);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int close(int fd);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int ioctl(int fd, ulong request, IntPtr argp);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int waitpid(int pid, ref int status, int options);

    [DllImport("libSystem.dylib", SetLastError = true)]
    private static extern int kill(int pid, int sig);
}
