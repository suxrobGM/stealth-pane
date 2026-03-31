using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StealthPane.Messages;
using StealthPane.Models;
using StealthPane.Services;

namespace StealthPane.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase,
    IRecipient<RegionSelectedMessage>,
    IRecipient<WindowSelectedMessage>
{
    private Timer? saveTimer;
    private AppSettings settings = SettingsService.Load();

    [ObservableProperty]
    public partial IReadOnlyList<string> ProviderNames { get; set; } = [];

    [ObservableProperty]
    public partial int SelectedProviderIndex { get; set; }

    [ObservableProperty]
    public partial double Opacity { get; set; } = 1.0;

    [ObservableProperty]
    public partial string OpacityValueText { get; set; } = "100%";

    [ObservableProperty]
    public partial int SelectedCaptureModeIndex { get; set; }

    [ObservableProperty]
    public partial string Hotkey { get; set; } = "Ctrl+Shift+C";

    [ObservableProperty]
    public partial string OpacityHotkey { get; set; } = "Ctrl+Shift+O";

    [ObservableProperty]
    public partial string SystemPrompt { get; set; } = "";

    [ObservableProperty]
    public partial string AudioHotkey { get; set; } = "Ctrl+Shift+A";

    [ObservableProperty]
    public partial string AudioModelPath { get; set; } = "";

    [ObservableProperty]
    public partial string AudioSystemPrompt { get; set; } = "";

    [ObservableProperty]
    public partial bool IsModelDownloading { get; set; }

    [ObservableProperty]
    public partial string DownloadModelButtonText { get; set; } = "Download Model";

    [ObservableProperty]
    public partial bool IsRegionMode { get; set; }

    [ObservableProperty]
    public partial bool IsWindowMode { get; set; }

    [ObservableProperty]
    public partial string RegionDisplayText { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedWindowTitle { get; set; } = "";

    public void Receive(RegionSelectedMessage message)
    {
        settings.Capture.RegionX = message.X;
        settings.Capture.RegionY = message.Y;
        settings.Capture.RegionWidth = message.Width;
        settings.Capture.RegionHeight = message.Height;
        RegionDisplayText = $"{message.Width}\u00D7{message.Height} at ({message.X}, {message.Y})";
        ScheduleSave();
    }

    public void Receive(WindowSelectedMessage message)
    {
        settings.Capture.WindowHandle = message.Handle;
        settings.Capture.WindowTitle = message.Title;
        SelectedWindowTitle = message.Title;
        ScheduleSave();
    }

    partial void OnSelectedProviderIndexChanged(int value)
    {
        WeakReferenceMessenger.Default.Send(new SettingsProviderChangedMessage(value));
    }

    partial void OnOpacityChanged(double value)
    {
        settings.WindowOpacity = value;
        OpacityValueText = $"{(int)(value * 100)}%";
        WeakReferenceMessenger.Default.Send(new OpacityChangedMessage(value));
        ScheduleSave();
    }

    partial void OnSelectedCaptureModeIndexChanged(int value)
    {
        settings.Capture.Mode = (CaptureMode)value;
        IsRegionMode = value == (int)CaptureMode.Region;
        IsWindowMode = value == (int)CaptureMode.Window;
        ScheduleSave();
    }

    partial void OnHotkeyChanged(string value)
    {
        settings.Capture.Hotkey = value;
        WeakReferenceMessenger.Default.Send(new HotkeyChangedMessage("capture", value));
        ScheduleSave();
    }

    partial void OnOpacityHotkeyChanged(string value)
    {
        settings.OpacityHotkey = value;
        WeakReferenceMessenger.Default.Send(new HotkeyChangedMessage("opacity", value));
        ScheduleSave();
    }

    partial void OnSystemPromptChanged(string value)
    {
        settings.Capture.SystemPrompt = value;
        ScheduleSave();
    }

    partial void OnAudioHotkeyChanged(string value)
    {
        settings.Audio.Hotkey = value;
        WeakReferenceMessenger.Default.Send(new HotkeyChangedMessage("audio", value));
        ScheduleSave();
    }

    partial void OnAudioModelPathChanged(string value)
    {
        settings.Audio.ModelPath = value;
        ScheduleSave();
    }

    partial void OnAudioSystemPromptChanged(string value)
    {
        settings.Audio.SystemPrompt = value;
        ScheduleSave();
    }

    [RelayCommand]
    private void SelectRegion()
    {
        WeakReferenceMessenger.Default.Send(new RequestRegionSelectionMessage());
    }

    [RelayCommand]
    private void SelectWindow()
    {
        WeakReferenceMessenger.Default.Send(new RequestWindowSelectionMessage());
    }

    public void Load(AppSettings settings)
    {
        this.settings = settings;

        var providers = CliProviderRegistry.GetAllProviders();
        ProviderNames = [.. providers.Select(p => p.Name)];

        var index = providers.ToList().FindIndex(p => p.Id == settings.ActiveProviderId);
        SelectedProviderIndex = index >= 0 ? index : 0;

        Opacity = settings.WindowOpacity;
        OpacityValueText = $"{(int)(settings.WindowOpacity * 100)}%";

        SelectedCaptureModeIndex = (int)settings.Capture.Mode;
        IsRegionMode = settings.Capture.Mode == CaptureMode.Region;
        IsWindowMode = settings.Capture.Mode == CaptureMode.Window;

        Hotkey = settings.Capture.Hotkey;
        OpacityHotkey = settings.OpacityHotkey;
        SystemPrompt = settings.Capture.SystemPrompt;
        if (settings.Capture is { RegionWidth: > 0, RegionHeight: > 0 })
        {
            RegionDisplayText =
                $"{settings.Capture.RegionWidth}\u00D7{settings.Capture.RegionHeight} at ({settings.Capture.RegionX}, {settings.Capture.RegionY})";
        }
        else
        {
            RegionDisplayText = "";
        }

        SelectedWindowTitle = settings.Capture.WindowTitle;

        AudioHotkey = settings.Audio.Hotkey;
        AudioModelPath = settings.Audio.ModelPath;
        AudioSystemPrompt = settings.Audio.SystemPrompt;
        DownloadModelButtonText = ModelDownloadService.ModelExists(settings.Audio.ModelPath)
            ? "Model ready"
            : "Download Model";

        WeakReferenceMessenger.Default.Register<RegionSelectedMessage>(this);
        WeakReferenceMessenger.Default.Register<WindowSelectedMessage>(this);
    }

    public void Unload()
    {
        WeakReferenceMessenger.Default.Unregister<RegionSelectedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<WindowSelectedMessage>(this);
    }

    [RelayCommand]
    private void ResetPrompt()
    {
        var provider = CliProviderRegistry.GetActiveProvider();
        SystemPrompt = provider.DefaultSystemPrompt;
    }

    [RelayCommand]
    private void ResetAudioPrompt()
    {
        AudioSystemPrompt = new AudioSettings().SystemPrompt;
    }

    [RelayCommand]
    private async Task DownloadModel()
    {
        var modelPath = settings.Audio.ModelPath;
        if (ModelDownloadService.ModelExists(modelPath))
        {
            DownloadModelButtonText = "Model already exists";
            return;
        }

        IsModelDownloading = true;
        DownloadModelButtonText = "Downloading...";
        WeakReferenceMessenger.Default.Send(new ModelDownloadRequestedMessage(modelPath));
    }

    public void OnDownloadCompleted(bool success)
    {
        IsModelDownloading = false;
        DownloadModelButtonText = success ? "Downloaded" : "Download Model";
    }

    private void ScheduleSave()
    {
        saveTimer?.Dispose();
        saveTimer = new Timer(_ =>
        {
            SettingsService.Save(settings);
            saveTimer?.Dispose();
            saveTimer = null;
        }, null, 500, Timeout.Infinite);
    }
}
