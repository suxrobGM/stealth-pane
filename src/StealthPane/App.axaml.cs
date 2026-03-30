using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using StealthPane.Services;
using StealthPane.Terminal;
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
        // Services
        services.AddSingleton<CliProviderRegistry>();
        services.AddSingleton<PtyService>();
        services.AddSingleton<ScreenCaptureService>();
        services.AddSingleton<CaptureInjectorService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<CleanupService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}
