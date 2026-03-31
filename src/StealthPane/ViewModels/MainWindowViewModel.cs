using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StealthPane.Messages;
using StealthPane.Models;
using StealthPane.Services;
using StealthPane.Terminal;

namespace StealthPane.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase,
    IRecipient<OpacityChangedMessage>,
    IRecipient<SettingsProviderChangedMessage>
{
    private readonly SettingsViewModel settingsViewModel;
    private readonly CleanupService cleanupService;
    private bool initialized;

    [ObservableProperty]
    public partial IReadOnlyList<string> ProviderNames { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedProviderIndex { get; set; }

    [ObservableProperty]
    public partial bool IsAlwaysOnTop { get; set; }

    [ObservableProperty]
    public partial double WindowOpacity { get; set; } = 1.0;

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial ViewModelBase? SettingsContent { get; set; }

    [ObservableProperty]
    public partial string OpacityText { get; set; } = "\u25D0 100%";

    [ObservableProperty]
    public partial IBrush PinForeground { get; set; } = Brushes.Transparent;

    public MainWindowViewModel(
        PtyService ptyService,
        SettingsViewModel settingsViewModel,
        CleanupService cleanupService)
    {
        this.settingsViewModel = settingsViewModel;
        this.cleanupService = cleanupService;
        PtyService = ptyService;
        Settings = SettingsService.Load();

        LoadFromSettings();

        WeakReferenceMessenger.Default.Register<OpacityChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<SettingsProviderChangedMessage>(this);
    }

    public AppSettings Settings { get; private set; }
    public PtyService PtyService { get; }

    public static CliProviderConfig GetActiveProvider() => CliProviderRegistry.GetActiveProvider();

    public void Initialize()
    {
        initialized = true;
        cleanupService.Start(Settings.Capture.TempDirectory, Settings.Capture.AutoCleanupMinutes);
        WeakReferenceMessenger.Default.Send(new ApplyOpacityMessage(WindowOpacity));
    }

    [RelayCommand]
    private void TogglePin()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        if (IsSettingsVisible)
        {
            IsSettingsVisible = false;
            return;
        }

        settingsViewModel.Load(Settings);
        SettingsContent = settingsViewModel;
        IsSettingsVisible = true;
    }

    public void Receive(OpacityChangedMessage message)
    {
        WindowOpacity = message.Opacity;
    }

    public void Receive(SettingsProviderChangedMessage message)
    {
        var providers = CliProviderRegistry.GetAllProviders();
        if (message.Index >= 0 && message.Index < providers.Count)
        {
            SelectedProviderIndex = message.Index;
        }
    }

    partial void OnSelectedProviderIndexChanged(int value)
    {
        if (value < 0) return;

        var providers = CliProviderRegistry.GetAllProviders();
        if (value >= providers.Count) return;

        var provider = providers[value];
        Settings.ActiveProviderId = provider.Id;
        SettingsService.Save(Settings);

        if (initialized)
        {
            WeakReferenceMessenger.Default.Send(new SwitchTerminalMessage(provider));
        }
    }

    partial void OnIsAlwaysOnTopChanged(bool value)
    {
        Settings.AlwaysOnTop = value;
        SettingsService.Save(Settings);
        PinForeground = value
            ? (IBrush)Application.Current!.Resources["AccentBrush"]!
            : (IBrush)Application.Current!.Resources["SecondaryFg"]!;
    }

    partial void OnWindowOpacityChanged(double value)
    {
        Settings.WindowOpacity = value;
        OpacityText = $"\u25D0 {(int)(value * 100)}%";

        if (initialized)
        {
            WeakReferenceMessenger.Default.Send(new ApplyOpacityMessage(value));
        }
    }

    private void LoadFromSettings()
    {
        var providers = CliProviderRegistry.GetAllProviders();
        ProviderNames = providers.Select(p => p.Name).ToList();

        var activeProvider = CliProviderRegistry.GetActiveProvider();
        var index = providers.ToList().FindIndex(p => p.Id == activeProvider.Id);
        SelectedProviderIndex = index >= 0 ? index : 0;

        IsAlwaysOnTop = Settings.AlwaysOnTop;
        WindowOpacity = Settings.WindowOpacity;
        PinForeground = IsAlwaysOnTop
            ? (IBrush)Application.Current!.Resources["AccentBrush"]!
            : (IBrush)Application.Current!.Resources["SecondaryFg"]!;
    }
}
