using StealthCode.ScreenCapture.Models;

namespace StealthCode.Messages;

// SettingsViewModel -> MainWindowViewModel
public sealed record OpacityChangedMessage(double Opacity);
public sealed record SettingsProviderChangedMessage(int Index);

// MainWindowViewModel -> View
public sealed record SwitchTerminalMessage(CliProviderConfig Provider);
public sealed record FallbackToShellMessage;
public sealed record ApplyOpacityMessage(double Opacity);

// Settings -> MainWindow: re-register hotkeys
public sealed record HotkeyChangedMessage(string Name, string Hotkey);

// SettingsViewModel -> MainWindow: request UI dialogs
public sealed record RequestRegionSelectionMessage;
public sealed record RequestWindowSelectionMessage;

// MainWindowViewModel -> View: recording state changed
public sealed record AudioRecordingChangedMessage(bool IsRecording);

// MainWindowViewModel -> View: no-focus mode changed
public sealed record NoFocusChangedMessage(bool IsNoFocus);

// CaptureInjectorService -> MainWindowViewModel: multi-capture state changed
public sealed record MultiCaptureChangedMessage(bool IsActive, int Count);

// SettingsViewModel -> MainWindowViewModel: request model download
public sealed record ModelDownloadRequestedMessage(string ModelPath);

// Update notifications
public sealed record UpdateAvailableMessage(bool Available);

// MainWindow -> SettingsViewModel: dialog results
public sealed record RegionSelectedMessage(int X, int Y, int Width, int Height);
public sealed record WindowSelectedMessage(nint Handle, string Title);
