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
    IRecipient<SettingsProviderChangedMessage>,
    IRecipient<HotkeyChangedMessage>
{
    private readonly CleanupService cleanupService;
    private readonly HotkeyService hotkeyService;
    private readonly SettingsViewModel settingsViewModel;
    private IntPtr hwnd;
    private bool initialized;
    private bool switching;

    public MainWindowViewModel(
        PtyService ptyService,
        SettingsViewModel settingsViewModel,
        CleanupService cleanupService,
        HotkeyService hotkeyService)
    {
        this.settingsViewModel = settingsViewModel;
        this.cleanupService = cleanupService;
        this.hotkeyService = hotkeyService;
        PtyService = ptyService;
        Settings = SettingsService.Load();

        LoadFromSettings();

        WeakReferenceMessenger.Default.Register<OpacityChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<SettingsProviderChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<HotkeyChangedMessage>(this);
    }

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
    public partial string CaptureHotkeyText { get; set; } = "\u2328 Ctrl+Shift+C";

    [ObservableProperty]
    public partial IBrush PinForeground { get; set; } = Brushes.Transparent;

    public AppSettings Settings { get; }
    public PtyService PtyService { get; }

    public void Receive(HotkeyChangedMessage message)
    {
        if (message.Name == "capture")
        {
            CaptureHotkeyText = $"\u2328 {message.Hotkey}";
            if (hwnd != IntPtr.Zero)
            {
                hotkeyService.Register("capture", message.Hotkey, hwnd, CaptureScreen);
            }
        }
        else if (message.Name == "opacity")
        {
            if (hwnd != IntPtr.Zero)
            {
                hotkeyService.Register("opacity", message.Hotkey, hwnd, CycleOpacity);
            }
        }
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

    public static CliProviderConfig GetActiveProvider()
    {
        return CliProviderRegistry.GetActiveProvider();
    }

    public void CaptureScreen()
    {
        var provider = GetActiveProvider();
        if (!provider.SupportsImageInput)
        {
            return;
        }

        CaptureInjectorService.CaptureAndInject(PtyService, provider, Settings.Capture);
    }

    public void Initialize(IntPtr windowHandle)
    {
        hwnd = windowHandle;
        initialized = true;
        PtyService.ProcessExited += OnProcessExited;
        cleanupService.Start(Settings.Capture.TempDirectory, Settings.Capture.AutoCleanupMinutes);
        WeakReferenceMessenger.Default.Send(new ApplyOpacityMessage(WindowOpacity));
        RegisterGlobalHotkeys();
    }

    private void OnProcessExited(int _)
    {
        if (switching)
        {
            return;
        }

        PtyService.Stop();
        WeakReferenceMessenger.Default.Send(new FallbackToShellMessage());
    }

    public void Cleanup()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        hotkeyService.Dispose();
        PtyService.Stop();
        PtyService.Dispose();
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
            settingsViewModel.Unload();
            IsSettingsVisible = false;
            return;
        }

        settingsViewModel.Load(Settings);
        SettingsContent = settingsViewModel;
        IsSettingsVisible = true;
    }

    public void CycleOpacity()
    {
        var presets = new[] { 1.0, 0.8, 0.6, 0.4 };
        var current = WindowOpacity;
        var next = 1.0;
        for (var i = 0; i < presets.Length; i++)
        {
            if (current > presets[i] + 0.01)
            {
                next = presets[i];
                break;
            }
        }

        WindowOpacity = next;
    }

    partial void OnSelectedProviderIndexChanged(int value)
    {
        if (value < 0)
        {
            return;
        }

        var providers = CliProviderRegistry.GetAllProviders();
        if (value >= providers.Count)
        {
            return;
        }

        var provider = providers[value];
        Settings.ActiveProviderId = provider.Id;
        SettingsService.Save(Settings);

        if (initialized)
        {
            switching = true;
            WeakReferenceMessenger.Default.Send(new SwitchTerminalMessage(provider));
            switching = false;
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

    private void RegisterGlobalHotkeys()
    {
        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero)
        {
            return;
        }

        hotkeyService.Register("capture", Settings.Capture.Hotkey, hwnd, CaptureScreen);
        hotkeyService.Register("opacity", Settings.OpacityHotkey, hwnd, CycleOpacity);
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
        CaptureHotkeyText = $"\u2328 {Settings.Capture.Hotkey}";
        PinForeground = IsAlwaysOnTop
            ? (IBrush)Application.Current!.Resources["AccentBrush"]!
            : (IBrush)Application.Current!.Resources["SecondaryFg"]!;
    }
}
