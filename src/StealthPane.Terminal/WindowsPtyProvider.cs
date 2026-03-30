using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace StealthPane.Terminal;

[SupportedOSPlatform("windows")]
public sealed class WindowsPtyProvider : IPtyProvider
{
    private IntPtr consoleHandle;
    private SafeFileHandle? pipeIn;
    private SafeFileHandle? pipeOut;
    private FileStream? writer;
    private Thread? readThread;
    private SafeProcessHandle? processHandle;
    private volatile bool disposed;

    public event Action<byte[]>? OutputReceived;
    public event Action<int>? ProcessExited;

    public void Start(string command, string[] args, string workingDirectory, int cols, int rows)
    {
        CreatePipes(out var inputReadSide, out var inputWriteSide, out var outputReadSide, out var outputWriteSide);

        pipeIn = inputWriteSide;
        pipeOut = outputReadSide;

        var size = new COORD { X = (short)cols, Y = (short)rows };
        var hr = CreatePseudoConsole(size, inputReadSide.DangerousGetHandle(), outputWriteSide.DangerousGetHandle(), 0, out consoleHandle);
        if (hr != 0)
        {
            throw new InvalidOperationException($"CreatePseudoConsole failed with HRESULT 0x{hr:X8}");
        }

        inputReadSide.Dispose();
        outputWriteSide.Dispose();

        writer = new FileStream(pipeIn, FileAccess.Write);

        var fullCommand = args.Length > 0 ? $"{command} {string.Join(' ', args)}" : command;
        StartProcess(fullCommand, workingDirectory);
        StartReadThread();
    }

    public void Write(byte[] data)
    {
        writer?.Write(data, 0, data.Length);
        writer?.Flush();
    }

    public void Resize(int cols, int rows)
    {
        if (consoleHandle != IntPtr.Zero)
        {
            var size = new COORD { X = (short)cols, Y = (short)rows };
            ResizePseudoConsole(consoleHandle, size);
        }
    }

    private static void CreatePipes(out SafeFileHandle inputRead, out SafeFileHandle inputWrite, out SafeFileHandle outputRead, out SafeFileHandle outputWrite)
    {
        if (!CreatePipe(out inputRead, out inputWrite, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException("Failed to create input pipe");
        }

        if (!CreatePipe(out outputRead, out outputWrite, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException("Failed to create output pipe");
        }
    }

    private void StartProcess(string commandLine, string workingDirectory)
    {
        var startupInfo = new STARTUPINFOEX();
        startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();

        var attributeSize = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeSize);

        startupInfo.lpAttributeList = Marshal.AllocHGlobal(attributeSize);
        if (!InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref attributeSize))
        {
            throw new InvalidOperationException("InitializeProcThreadAttributeList failed");
        }

        if (!UpdateProcThreadAttribute(
                startupInfo.lpAttributeList,
                0,
                (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                consoleHandle,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero))
        {
            throw new InvalidOperationException("UpdateProcThreadAttribute failed");
        }

        if (!CreateProcessW(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                EXTENDED_STARTUPINFO_PRESENT,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out var processInfo))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"CreateProcess failed with error {error}");
        }

        processHandle = new SafeProcessHandle(processInfo.hProcess, ownsHandle: true);
        CloseHandle(processInfo.hThread);

        DeleteProcThreadAttributeList(startupInfo.lpAttributeList);
        Marshal.FreeHGlobal(startupInfo.lpAttributeList);
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
        using var reader = new FileStream(pipeOut!, FileAccess.Read);

        try
        {
            while (!disposed)
            {
                var bytesRead = reader.Read(buffer, 0, buffer.Length);
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

        if (processHandle is { IsInvalid: false, IsClosed: false })
        {
            GetExitCodeProcess(processHandle.DangerousGetHandle(), out var exitCode);
            ProcessExited?.Invoke((int)exitCode);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        writer?.Dispose();

        if (consoleHandle != IntPtr.Zero)
        {
            ClosePseudoConsole(consoleHandle);
            consoleHandle = IntPtr.Zero;
        }

        processHandle?.Dispose();
        pipeIn?.Dispose();
        pipeOut?.Dispose();
    }

    // --- P/Invoke declarations ---

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;

    [StructLayout(LayoutKind.Sequential)]
    private struct COORD
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFOEX
    {
        public STARTUPINFO StartupInfo;
        public IntPtr lpAttributeList;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void ClosePseudoConsole(IntPtr hPC);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(string? lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);
}
