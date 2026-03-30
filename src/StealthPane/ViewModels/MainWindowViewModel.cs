using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StealthPane.Models;
using StealthPane.Services;
using StealthPane.Terminal;

namespace StealthPane.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly CliProviderRegistry providerRegistry;

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
    public partial IBrush PinForeground { get; set; } = new SolidColorBrush(Color.Parse("#808080"));

    public MainWindowViewModel(CliProviderRegistry providerRegistry, PtyService ptyService)
    {
        this.providerRegistry = providerRegistry;
        PtyService = ptyService;
        Settings = SettingsService.Load();

        LoadFromSettings();
    }

    public AppSettings Settings { get; private set; }
    public PtyService PtyService { get; }

    public CliProviderConfig GetActiveProvider() => providerRegistry.GetActiveProvider();
    public IReadOnlyList<CliProviderConfig> GetAllProviders() => providerRegistry.GetAllProviders();

    public event Action<CliProviderConfig>? ProviderSwitchRequested;

    [RelayCommand]
    private void TogglePin()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsVisible = !IsSettingsVisible;
    }

    partial void OnSelectedProviderIndexChanged(int value)
    {
        if (value < 0) return;

        var providers = providerRegistry.GetAllProviders();
        if (value >= providers.Count) return;

        var provider = providers[value];
        Settings.ActiveProviderId = provider.Id;
        SettingsService.Save(Settings);

        ProviderSwitchRequested?.Invoke(provider);
    }

    partial void OnIsAlwaysOnTopChanged(bool value)
    {
        Settings.AlwaysOnTop = value;
        SettingsService.Save(Settings);
        PinForeground = value
            ? new SolidColorBrush(Color.Parse("#10B981"))
            : new SolidColorBrush(Color.Parse("#808080"));
    }

    partial void OnWindowOpacityChanged(double value)
    {
        Settings.WindowOpacity = value;
        OpacityText = $"\u25D0 {(int)(value * 100)}%";
    }

    private void LoadFromSettings()
    {
        var providers = providerRegistry.GetAllProviders();
        ProviderNames = providers.Select(p => p.Name).ToList();

        var activeProvider = providerRegistry.GetActiveProvider();
        var index = providers.ToList().FindIndex(p => p.Id == activeProvider.Id);
        SelectedProviderIndex = index >= 0 ? index : 0;

        IsAlwaysOnTop = Settings.AlwaysOnTop;
        WindowOpacity = Settings.WindowOpacity;
        PinForeground = IsAlwaysOnTop
            ? new SolidColorBrush(Color.Parse("#10B981"))
            : new SolidColorBrush(Color.Parse("#808080"));
    }
}
