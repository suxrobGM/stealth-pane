using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using StealthPane.Terminal;

namespace StealthPane.Controls;

public sealed class TerminalWebView : UserControl, IDisposable
{
    private NativeWebView? webView;
    private PtyService? ptyService;
    private readonly List<byte> outputBuffer = [];
    private readonly object bufferLock = new();
    private bool terminalReady;
    private int pendingCols;
    private int pendingRows;

    public event Action<int>? ProcessExited;

    public void Initialize(PtyService ptyService)
    {
        this.ptyService = ptyService;
        this.ptyService.OutputReceived += OnPtyOutput;
        this.ptyService.ProcessExited += OnPtyProcessExited;

        webView = new NativeWebView();
        webView.NavigationCompleted += OnNavigationCompleted;
        webView.WebMessageReceived += OnWebMessageReceived;

        Content = webView;

        var htmlPath = TerminalAssets.GetTerminalHtmlPath();
        webView.Source = new Uri($"file:///{htmlPath.Replace('\\', '/')}");
    }

    public void StartProcess(string command, string[] args, string workingDirectory)
    {
        if (terminalReady && pendingCols > 0)
        {
            ptyService?.Start(command, args, workingDirectory, pendingCols, pendingRows);
        }
    }

    public void Reset()
    {
        webView?.InvokeScript("termReset()");
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            // TODO: show error fallback
        }
    }

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        try
        {
            if (e.Body is null)
            {
                return;
            }

            using var doc = JsonDocument.Parse(e.Body);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "ready":
                    pendingCols = root.GetProperty("cols").GetInt32();
                    pendingRows = root.GetProperty("rows").GetInt32();
                    terminalReady = true;
                    break;

                case "input":
                    var data = root.GetProperty("data").GetString();
                    if (data is not null)
                    {
                        ptyService?.Write(System.Text.Encoding.UTF8.GetBytes(data));
                    }
                    break;

                case "resize":
                    var cols = root.GetProperty("cols").GetInt32();
                    var rows = root.GetProperty("rows").GetInt32();
                    pendingCols = cols;
                    pendingRows = rows;
                    ptyService?.Resize(cols, rows);
                    break;
            }
        }
        catch
        {
            // Ignore malformed messages
        }
    }

    private void OnPtyOutput(byte[] data)
    {
        lock (bufferLock)
        {
            if (outputBuffer.Count == 0)
            {
                Dispatcher.UIThread.InvokeAsync(FlushOutput, DispatcherPriority.Background);
            }

            outputBuffer.AddRange(data);
        }
    }

    private void FlushOutput()
    {
        byte[] data;
        lock (bufferLock)
        {
            if (outputBuffer.Count == 0)
            {
                return;
            }

            data = outputBuffer.ToArray();
            outputBuffer.Clear();
        }

        var base64 = Convert.ToBase64String(data);
        webView?.InvokeScript($"termWrite('{base64}')");
    }

    private void OnPtyProcessExited(int exitCode)
    {
        Dispatcher.UIThread.InvokeAsync(() => ProcessExited?.Invoke(exitCode));
    }

    public void Dispose()
    {
        if (ptyService is not null)
        {
            ptyService.OutputReceived -= OnPtyOutput;
            ptyService.ProcessExited -= OnPtyProcessExited;
        }
    }
}
