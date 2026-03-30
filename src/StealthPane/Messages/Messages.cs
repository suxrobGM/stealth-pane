using StealthPane.Models;

namespace StealthPane.Messages;

// SettingsViewModel -> MainWindowViewModel
public sealed record OpacityChangedMessage(double Opacity);
public sealed record SettingsProviderChangedMessage(int Index);

// MainWindowViewModel -> View
public sealed record SwitchTerminalMessage(CliProviderConfig Provider);
public sealed record ApplyOpacityMessage(double Opacity);
