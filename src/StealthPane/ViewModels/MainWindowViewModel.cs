using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StealthPane.Audio.Services;
using StealthPane.Messages;
using StealthPane.ScreenCapture.Models;
using StealthPane.ScreenCapture.Services;
using StealthPane.Services;
using StealthPane.Terminal;

namespace StealthPane.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase,
    IRecipient<OpacityChangedMessage>,
    IRecipient<SettingsProviderChangedMessage>,
    IRecipient<HotkeyChangedMessage>,
    IRecipient<ModelDownloadRequestedMessage>
{
    private readonly AudioInjectorService audioInjectorService;
    private readonly CaptureInjectorService captureInjectorService;
    private readonly HotkeyService hotkeyService;
    private readonly ModelDownloadService modelDownloadService;
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
        AudioInjectorService audioInjectorService,
        CaptureInjectorService captureInjectorService,
        ModelDownloadService modelDownloadService)
    {
        this.settingsService = settingsService;
        this.providerRegistry = providerRegistry;
        this.settingsViewModel = settingsViewModel;
        this.hotkeyService = hotkeyService;
        this.audioInjectorService = audioInjectorService;
        this.captureInjectorService = captureInjectorService;
        this.modelDownloadService = modelDownloadService;
        PtyService = ptyService;

        LoadFromSettings();

        WeakReferenceMessenger.Default.Register<OpacityChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<SettingsProviderChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<HotkeyChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ModelDownloadRequestedMessage>(this);
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
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial string AudioHotkeyText { get; set; } = "\u23FA Ctrl+Shift+A";

    [ObservableProperty]
    public partial bool IsModelAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsModelDownloading { get; set; }

    [ObservableProperty]
    public partial string ModelStatusText { get; set; } = "";

    [ObservableProperty]
    public partial IBrush PinForeground { get; set; } = Brushes.Transparent;

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
        else if (message.Name == "audio")
        {
            AudioHotkeyText = $"\u23FA {message.Hotkey}";
            if (IsModelAvailable)
            {
                RegisterAudioHotkey();
            }
        }
    }

    public async void Receive(ModelDownloadRequestedMessage message)
    {
        var success = await DownloadModelAsync();
        settingsViewModel.OnDownloadCompleted(success);
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

    public CliProviderConfig GetActiveProvider()
    {
        return providerRegistry.GetActiveProvider();
    }

    public void CaptureScreen()
    {
        var provider = GetActiveProvider();
        if (!provider.SupportsImageInput)
        {
            return;
        }

        captureInjectorService.CaptureAndInject();
    }

    public void ToggleAudioRecording()
    {
        if (!IsModelAvailable || IsModelDownloading)
        {
            ModelStatusText = "Whisper model not ready";
            return;
        }

        var wasRecording = IsRecording;
        var started = audioInjectorService.Toggle(isRec =>
        {
            IsRecording = isRec;
            ModelStatusText = "";
            WeakReferenceMessenger.Default.Send(new AudioRecordingChangedMessage(isRec));
        });

        if (!started && !wasRecording)
        {
            ModelStatusText = audioInjectorService.LastError ?? "Audio capture failed";
        }
    }

    private void OnDownloadProgress(long downloaded, long total)
    {
        if (total > 0)
        {
            var pct = (int)(downloaded * 100 / total);
            ModelStatusText = $"Downloading model... {downloaded / 1048576.0:F1}/{total / 1048576.0:F0} MB ({pct}%)";
        }
        else
        {
            ModelStatusText = $"Downloading model... {downloaded / 1048576.0:F1} MB";
        }
    }

    public async Task<bool> DownloadModelAsync()
    {
        if (IsModelDownloading)
        {
            return false;
        }

        var modelPath = settingsService.Settings.Audio.ModelPath;
        var modelFileName = Path.GetFileName(modelPath);

        IsModelDownloading = true;
        ModelStatusText = "Downloading model...";

        modelDownloadService.DownloadProgress += OnDownloadProgress;
        var success = await modelDownloadService.DownloadAsync(modelFileName, modelPath);
        modelDownloadService.DownloadProgress -= OnDownloadProgress;

        IsModelDownloading = false;

        if (success)
        {
            IsModelAvailable = true;
            ModelStatusText = "";
            RegisterAudioHotkey();
        }
        else
        {
            ModelStatusText = "Download failed";
        }

        return success;
    }

    public void Initialize(IntPtr windowHandle)
    {
        hwnd = windowHandle;
        initialized = true;
        PtyService.ProcessExited += OnProcessExited;
        CleanupService.CleanupOldCaptures();
        WeakReferenceMessenger.Default.Send(new ApplyOpacityMessage(WindowOpacity));
        CheckModelAvailability();
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

    private void RegisterGlobalHotkeys()
    {
        if (!OperatingSystem.IsWindows() || hwnd == IntPtr.Zero)
        {
            return;
        }

        var settings = settingsService.Settings;
        hotkeyService.Register("capture", settings.Capture.Hotkey, hwnd, CaptureScreen);
        hotkeyService.Register("opacity", settings.OpacityHotkey, hwnd, CycleOpacity);

        if (IsModelAvailable)
        {
            RegisterAudioHotkey();
        }
    }

    private void RegisterAudioHotkey()
    {
        if (hwnd != IntPtr.Zero)
        {
            hotkeyService.Register("audio", settingsService.Settings.Audio.Hotkey, hwnd, ToggleAudioRecording);
        }
    }

    private void CheckModelAvailability()
    {
        IsModelAvailable = ModelDownloadService.ModelExists(settingsService.Settings.Audio.ModelPath);
        if (!IsModelAvailable)
        {
            ModelStatusText = "Whisper model not found, please download by clicking the button in settings";
        }
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
        AudioHotkeyText = $"\u23FA {settings.Audio.Hotkey}";
        PinForeground = IsAlwaysOnTop
            ? (IBrush)Application.Current!.Resources["AccentBrush"]!
            : (IBrush)Application.Current!.Resources["SecondaryFg"]!;
    }
}
