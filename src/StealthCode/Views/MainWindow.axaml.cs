using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using StealthCode.Messages;
using StealthCode.ScreenCapture.Services;
// ReSharper disable once RedundantUsingDirective
using StealthCode.Services;
using StealthCode.Utilities;
using StealthCode.ViewModels;
using StealthCode.Views;

namespace StealthCode;

public sealed partial class MainWindow : Window,
    IRecipient<SwitchTerminalMessage>,
    IRecipient<FallbackToShellMessage>,
    IRecipient<ApplyOpacityMessage>,
    IRecipient<RequestRegionSelectionMessage>,
    IRecipient<RequestWindowSelectionMessage>
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow()
    {
        viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = viewModel;

        InitializeComponent();

        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
    }

    public void Receive(ApplyOpacityMessage message)
    {
        WindowOpacityUtils.Apply(this, message.Opacity);
    }

    public void Receive(FallbackToShellMessage message)
    {
        Terminal.StartProcess("cmd.exe", [], Environment.CurrentDirectory);
    }

    public async void Receive(RequestRegionSelectionMessage message)
    {
        var overlay = new RegionSelectionWindow();
        overlay.Show();
        var result = await overlay.GetSelectionAsync();
        if (result.HasValue)
        {
            var r = result.Value;
            WeakReferenceMessenger.Default.Send(new RegionSelectedMessage(r.X, r.Y, r.Width, r.Height));
        }
    }

    public async void Receive(RequestWindowSelectionMessage message)
    {
        var ownHandle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        var windows = WindowEnumerationService.GetVisibleWindows(ownHandle);
        var picker = new WindowPickerWindow(windows);
        await picker.ShowDialog(this);
        var result = await picker.GetSelectionAsync();
        if (result is not null)
        {
            WeakReferenceMessenger.Default.Send(new WindowSelectedMessage(result.Handle, result.Title));
        }
    }

    public void Receive(SwitchTerminalMessage message)
    {
        viewModel.PtyService.Stop();
        Terminal.Reset();
        Terminal.StartProcess(message.Provider.Command, message.Provider.Args, Environment.CurrentDirectory);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
#if !DEBUG
        ContentProtectionService.EnableProtection(this);
#endif

        WeakReferenceMessenger.Default.Register<SwitchTerminalMessage>(this);
        WeakReferenceMessenger.Default.Register<FallbackToShellMessage>(this);
        WeakReferenceMessenger.Default.Register<ApplyOpacityMessage>(this);
        WeakReferenceMessenger.Default.Register<RequestRegionSelectionMessage>(this);
        WeakReferenceMessenger.Default.Register<RequestWindowSelectionMessage>(this);

        Terminal.Initialize(viewModel.PtyService);

        var provider = viewModel.GetActiveProvider();
        Terminal.StartProcess(provider.Command, provider.Args, Environment.CurrentDirectory);

        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        viewModel.Initialize(hwnd);
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        viewModel.Cleanup();
        Terminal.Dispose();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pos = e.GetPosition(this);
        if (pos.Y <= 28 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Don't drag if clicking on an interactive control
            var source = e.Source as Visual;
            while (source is not null && source != this)
            {
                if (source is Button or ComboBox)
                {
                    return;
                }

                source = source.GetVisualParent();
            }

            BeginMoveDrag(e);
        }
    }
}
