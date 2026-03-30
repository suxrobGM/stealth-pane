using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using StealthPane.Models;
using StealthPane.Services;
using StealthPane.ViewModels;

namespace StealthPane;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly SettingsViewModel settingsViewModel;

    public MainWindow()
    {
        viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();

        DataContext = viewModel;

        InitializeComponent();

        Opened += OnWindowOpened;
        Closing += OnWindowClosing;

        viewModel.ProviderSwitchRequested += OnProviderSwitch;
        settingsViewModel.OpacityUpdated += opacity => viewModel.WindowOpacity = opacity;
        settingsViewModel.ProviderSelectionChanged += OnSettingsProviderChanged;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        ContentProtectionService.EnableProtection(this);

        Terminal.Initialize(viewModel.PtyService);

        var provider = viewModel.GetActiveProvider();
        Terminal.StartProcess(provider.Command, provider.Args, Environment.CurrentDirectory);

        var cleanup = App.Services.GetRequiredService<CleanupService>();
        cleanup.Start(viewModel.Settings.Capture.TempDirectory, viewModel.Settings.Capture.AutoCleanupMinutes);
    }

    private void OnProviderSwitch(CliProviderConfig provider)
    {
        viewModel.PtyService.Stop();
        Terminal.Reset();
        Terminal.StartProcess(provider.Command, provider.Args, Environment.CurrentDirectory);
    }

    private void OnSettingsProviderChanged()
    {
        var providers = viewModel.GetAllProviders();
        if (settingsViewModel.SelectedProviderIndex >= 0 &&
            settingsViewModel.SelectedProviderIndex < providers.Count)
        {
            viewModel.SelectedProviderIndex = settingsViewModel.SelectedProviderIndex;
        }
    }

    private void OnPinClick(object? sender, RoutedEventArgs e) => viewModel.TogglePinCommand.Execute(null);

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (viewModel.IsSettingsVisible)
        {
            viewModel.IsSettingsVisible = false;
            return;
        }

        settingsViewModel.Load(viewModel.Settings);
        viewModel.IsSettingsVisible = true;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
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
