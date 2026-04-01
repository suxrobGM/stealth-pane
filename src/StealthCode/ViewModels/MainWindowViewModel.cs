using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StealthCode.Messages;
using StealthCode.ScreenCapture.Models;
using StealthCode.ScreenCapture.Utilities;
using StealthCode.Services;
using StealthCode.Terminal;
using StealthCode.Updater.Services;

namespace StealthCode.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase,
    IRecipient<OpacityChangedMessage>,
    IRecipient<SettingsProviderChangedMessage>,
    IRecipient<HotkeyChangedMessage>,
    IRecipient<UpdateAvailableMessage>
{
    private readonly CaptureInjectorService captureInjectorService;
    private readonly HotkeyService hotkeyService;
    private readonly CliProviderRegistry providerRegistry;
    private readonly SettingsService settingsService;
    private readonly SettingsViewModel settingsViewModel;
    private IntPtr hwnd;
    private bool initialized;
    private bool switching;

    public MainWindowViewModel(
        PtyService ptyService,
        SettingsService settingsService,
        CliProviderRegistry providerRegistry,
        SettingsViewModel settingsViewModel,
        HotkeyService hotkeyService,
        CaptureInjectorService captureInjectorService,
        AudioViewModel audioViewModel)
    {
        this.settingsService = settingsService;
        this.providerRegistry = providerRegistry;
        this.settingsViewModel = settingsViewModel;
        this.hotkeyService = hotkeyService;
        this.captureInjectorService = captureInjectorService;
        PtyService = ptyService;
        Audio = audioViewModel;

        LoadFromSettings();

        WeakReferenceMessenger.Default.Register<OpacityChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<SettingsProviderChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<HotkeyChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<UpdateAvailableMessage>(this);
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
    public partial string CaptureHotkeyText { get; set; } = "\u2328 Shift+C";

    public string VersionText { get; } = $"v{UpdateService.CurrentVersion}";

    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    [ObservableProperty]
    public partial IBrush PinForeground { get; set; } = Brushes.Transparent;

    public PtyService PtyService { get; }
    public AudioViewModel Audio { get; }

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
        else if (message.Name == "audio")
        {
            Audio.OnHotkeyChanged(message.Hotkey);
        }
    }

    public void Receive(OpacityChangedMessage message)
    {
        WindowOpacity = message.Opacity;
    }

    public void Receive(SettingsProviderChangedMessage message)
    {
        var providers = providerRegistry.GetAllProviders();
        if (message.Index >= 0 && message.Index < providers.Count)
        {
            SelectedProviderIndex = message.Index;
        }
    }

    public void Receive(UpdateAvailableMessage message)
    {
        IsUpdateAvailable = message.Available;
    }

    public void CaptureScreen()
    {
        captureInjectorService.CaptureAndInject();
    }

    public void Initialize(IntPtr windowHandle)
    {
        hwnd = windowHandle;
        initialized = true;
        PtyService.ProcessExited += OnProcessExited;
        CleanupUtils.CleanupOldCaptures();
        WeakReferenceMessenger.Default.Send(new ApplyOpacityMessage(WindowOpacity));
        Audio.Initialize(windowHandle, settingsViewModel);
        RegisterGlobalHotkeys();
    }

    public void Cleanup()
    {
        Audio.Cleanup();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        hotkeyService.Dispose();
        PtyService.Stop();
        PtyService.Dispose();
    }

    public CliProviderConfig GetActiveProvider()
    {
        return providerRegistry.GetActiveProvider();
    }

    [RelayCommand]
    private void TogglePin()
    {
        IsAlwaysOnTop = !IsAlwaysOnTop;
    }

    [RelayCommand]
    private void ShowUpdate()
    {
        if (!IsSettingsVisible)
        {
            ToggleSettings();
        }
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

        settingsViewModel.Load();
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

        var providers = providerRegistry.GetAllProviders();
        if (value >= providers.Count)
        {
            return;
        }

        var provider = providers[value];
        settingsService.Settings.ActiveProviderId = provider.Id;
        settingsService.Save();

        if (initialized)
        {
            switching = true;
            WeakReferenceMessenger.Default.Send(new SwitchTerminalMessage(provider));
            switching = false;
        }
    }

    partial void OnIsAlwaysOnTopChanged(bool value)
    {
        settingsService.Settings.AlwaysOnTop = value;
        settingsService.Save();
        PinForeground = value
            ? (IBrush)Application.Current!.Resources["AccentBrush"]!
            : (IBrush)Application.Current!.Resources["SecondaryFg"]!;
    }

    partial void OnWindowOpacityChanged(double value)
    {
        settingsService.Settings.WindowOpacity = value;
        OpacityText = $"\u25D0 {(int)(value * 100)}%";

        if (initialized)
        {
            WeakReferenceMessenger.Default.Send(new ApplyOpacityMessage(value));
        }
    }

    private void OnProcessExited(int _)
    {
        if (!switching)
        {
            PtyService.Stop();
            WeakReferenceMessenger.Default.Send(new FallbackToShellMessage());
        }
    }

    private void RegisterGlobalHotkeys()
    {
        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero)
        {
            return;
        }

        var settings = settingsService.Settings;
        hotkeyService.Register("capture", settings.Capture.Hotkey, hwnd, CaptureScreen);
        hotkeyService.Register("opacity", settings.OpacityHotkey, hwnd, CycleOpacity);
    }

    private void LoadFromSettings()
    {
        var settings = settingsService.Settings;
        var providers = providerRegistry.GetAllProviders();
        ProviderNames = providers.Select(p => p.Name).ToList();

        var activeProvider = providerRegistry.GetActiveProvider();
        var index = providers.ToList().FindIndex(p => p.Id == activeProvider.Id);
        SelectedProviderIndex = index >= 0 ? index : 0;

        IsAlwaysOnTop = settings.AlwaysOnTop;
        WindowOpacity = settings.WindowOpacity;
        CaptureHotkeyText = $"\u2328 {settings.Capture.Hotkey}";
        Audio.LoadFromSettings();
        PinForeground = IsAlwaysOnTop
            ? (IBrush)Application.Current!.Resources["AccentBrush"]!
            : (IBrush)Application.Current!.Resources["SecondaryFg"]!;
    }
}
