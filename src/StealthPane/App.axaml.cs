using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using StealthPane.Audio;
using StealthPane.ScreenCapture;
using StealthPane.Services;
using StealthPane.Terminal;
using StealthPane.Updater;
using StealthPane.ViewModels;

namespace StealthPane;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Modules
        services.AddTerminal();
        services.AddScreenCapture();
        services.AddAudioCapture();

        // App services
        services.AddSingleton<SettingsService>();
        services.AddSingleton<CliProviderRegistry>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<CaptureInjectorService>();
        services.AddSingleton<AudioInjectorService>();
        services.AddUpdater();

        // ViewModels
        services.AddSingleton<AudioViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }
}
