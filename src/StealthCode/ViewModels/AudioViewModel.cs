using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StealthCode.Audio.Services;
using StealthCode.Messages;
using StealthCode.Services;

namespace StealthCode.ViewModels;

// ReSharper disable once PartialTypeWithSinglePart
public sealed partial class AudioViewModel(
    SettingsService settingsService,
    HotkeyService hotkeyService,
    AudioInjectorService audioInjectorService,
    ModelDownloadService modelDownloadService) : ViewModelBase, IRecipient<ModelDownloadRequestedMessage>
{
    private IntPtr hwnd;
    private SettingsViewModel? settingsViewModel;

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    public partial string HotkeyText { get; set; } = "\u23FA Ctrl+Shift+A";

    [ObservableProperty]
    public partial bool IsModelAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsModelDownloading { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "";

    public async void Receive(ModelDownloadRequestedMessage message)
    {
        if (IsModelDownloading)
        {
            return;
        }

        var modelPath = settingsService.Settings.Audio.ModelPath;
        var modelFileName = Path.GetFileName(modelPath);

        IsModelDownloading = true;
        StatusText = "Downloading model...";

        modelDownloadService.DownloadProgress += OnDownloadProgress;
        var success = await modelDownloadService.DownloadAsync(modelFileName, modelPath);
        modelDownloadService.DownloadProgress -= OnDownloadProgress;

        IsModelDownloading = false;

        if (success)
        {
            IsModelAvailable = true;
            StatusText = "";
            RegisterHotkey();
        }
        else
        {
            StatusText = "Download failed";
        }

        settingsViewModel?.OnDownloadCompleted(success);
    }

    public void Initialize(IntPtr windowHandle, SettingsViewModel settingsVm)
    {
        hwnd = windowHandle;
        settingsViewModel = settingsVm;

        IsModelAvailable = ModelDownloadService.ModelExists(settingsService.Settings.Audio.ModelPath);
        if (!IsModelAvailable)
        {
            StatusText = "Whisper model not found, please download by clicking the button in settings";
        }

        if (IsModelAvailable)
        {
            RegisterHotkey();
        }

        WeakReferenceMessenger.Default.Register(this);
    }

    public void OnHotkeyChanged(string hotkey)
    {
        HotkeyText = $"\u23FA {hotkey}";
        if (IsModelAvailable)
        {
            RegisterHotkey();
        }
    }

    public void Toggle()
    {
        if (!IsModelAvailable || IsModelDownloading)
        {
            StatusText = "Whisper model not ready";
            return;
        }

        var wasRecording = IsRecording;
        var started = audioInjectorService.Toggle(isRec =>
        {
            IsRecording = isRec;
            StatusText = "";
            WeakReferenceMessenger.Default.Send(new AudioRecordingChangedMessage(isRec));
        });

        if (!started && !wasRecording)
        {
            StatusText = audioInjectorService.LastError ?? "Audio capture failed";
        }
    }

    public void Cleanup()
    {
        WeakReferenceMessenger.Default.Unregister<ModelDownloadRequestedMessage>(this);
    }

    public void LoadFromSettings()
    {
        HotkeyText = $"\u23FA {settingsService.Settings.Audio.Hotkey}";
    }

    private void RegisterHotkey()
    {
        if (hwnd != IntPtr.Zero)
        {
            hotkeyService.Register("audio", settingsService.Settings.Audio.Hotkey, hwnd, Toggle);
        }
    }

    private void OnDownloadProgress(long downloaded, long total)
    {
        if (total > 0)
        {
            var pct = (int)(downloaded * 100 / total);
            StatusText = $"Downloading model... {downloaded / 1048576.0:F1}/{total / 1048576.0:F0} MB ({pct}%)";
        }
        else
        {
            StatusText = $"Downloading model... {downloaded / 1048576.0:F1} MB";
        }
    }
}
