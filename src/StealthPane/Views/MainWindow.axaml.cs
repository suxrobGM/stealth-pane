using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using StealthPane.Messages;
using StealthPane.Services;
using StealthPane.ViewModels;

namespace StealthPane;

public sealed partial class MainWindow : Window,
    IRecipient<SwitchTerminalMessage>,
    IRecipient<ApplyOpacityMessage>
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

    private void OnWindowOpened(object? sender, EventArgs e)
    {
#if !DEBUG
        ContentProtectionService.EnableProtection(this);
#endif

        WeakReferenceMessenger.Default.Register<SwitchTerminalMessage>(this);
        WeakReferenceMessenger.Default.Register<ApplyOpacityMessage>(this);

        Terminal.Initialize(viewModel.PtyService);

        var provider = viewModel.GetActiveProvider();
        Terminal.StartProcess(provider.Command, provider.Args, Environment.CurrentDirectory);

        viewModel.Initialize();
    }

    public void Receive(SwitchTerminalMessage message)
    {
        viewModel.PtyService.Stop();
        Terminal.Reset();
        Terminal.StartProcess(message.Provider.Command, message.Provider.Args, Environment.CurrentDirectory);
    }

    public void Receive(ApplyOpacityMessage message)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowOpacityHelper.Apply(this, message.Opacity);
        }
        else
        {
            Opacity = message.Opacity;
        }
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        viewModel.PtyService.Stop();
        Terminal.Dispose();
        viewModel.PtyService.Dispose();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var pos = e.GetPosition(this);
        if (pos.Y <= 28 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
