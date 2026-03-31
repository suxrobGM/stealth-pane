using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using StealthPane.Messages;
using StealthPane.Models;
using StealthPane.Services;

namespace StealthPane.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private AppSettings settings = SettingsService.Load();
    private Timer? saveTimer;

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
    public partial string SystemPrompt { get; set; } = "";

    [ObservableProperty]
    public partial decimal CleanupMinutes { get; set; } = 30;

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
        ScheduleSave();
    }

    partial void OnHotkeyChanged(string value)
    {
        settings.Capture.Hotkey = value;
        ScheduleSave();
    }

    partial void OnSystemPromptChanged(string value)
    {
        settings.Capture.SystemPrompt = value;
        ScheduleSave();
    }

    partial void OnCleanupMinutesChanged(decimal value)
    {
        settings.Capture.AutoCleanupMinutes = (int)value;
        ScheduleSave();
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
        Hotkey = settings.Capture.Hotkey;
        SystemPrompt = settings.Capture.SystemPrompt;
        CleanupMinutes = settings.Capture.AutoCleanupMinutes;
    }

    [RelayCommand]
    private void ResetPrompt()
    {
        var provider = CliProviderRegistry.GetActiveProvider();
        SystemPrompt = provider.DefaultSystemPrompt;
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
