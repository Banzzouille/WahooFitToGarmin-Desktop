using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;

using WahooFitToGarmin.UI.Models;
using WahooFitToGarmin.UI.Services;
using WahooFitToGarmin.UI.Services.Logging;
using WahooFitToGarmin.UI.Services.Platform;
using WahooFitToGarmin.UI.ViewModels;
using WahooFitToGarmin.UI.Views;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Services;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.UI;

public partial class App : Application
{
    private IHost? _host;
    private SingleInstanceGuard? _instanceGuard;
    private ShellWindow? _window;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        _instanceGuard = new SingleInstanceGuard();
        if (!_instanceGuard.TryAcquire())
        {
            // Another instance is already watching the folder; it has been asked
            // to show its window. Two processes on one folder would upload the
            // same activity twice.
            //
            // Leaving through the lifetime is not an option here: the dispatcher
            // loop has not started yet, and asking it to shut down throws. The
            // process has built nothing worth unwinding, so it simply stops.
            _instanceGuard.Dispose();
            Environment.Exit(0);
            return;
        }

        _instanceGuard.ListenerFailed += ex =>
        {
            // This runs before the host exists, so the logger is not configured
            // yet. Standard error is the only channel available at this point,
            // and a guard that silently fails to guard has to be visible.
            Console.Error.WriteLine($"[single-instance] listener failed: {ex}");
            Log.Warning(ex, "The single instance listener could not be established; a second launch will not be prevented");
        };
        _instanceGuard.ActivationRequested += () =>
            Avalonia.Threading.Dispatcher.UIThread.Post(ShowWindow);

        // Closing the window hides the application; quitting is explicit, from
        // the tray menu. Without this the process would end at the first close
        // and stop watching.
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        desktop.ShutdownRequested += (_, _) => _instanceGuard?.Dispose();

        _host = BuildHost();

        _host.Services.GetRequiredService<ThemeService>().Initialise();

        _window = new ShellWindow { DataContext = _host.Services.GetRequiredService<ShellViewModel>() };
        _window.Closing += OnWindowClosing;
        _window.Show();

        desktop.MainWindow = _window;

        base.OnFrameworkInitializationCompleted();

        await _host.StartAsync();
    }

    private IHost BuildHost()
    {
        var appLocation = AppContext.BaseDirectory;

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => c.SetBasePath(appLocation))
            // writeToProviders keeps the in-app viewer alive alongside the file
            // sink, so both show the same stream.
            .UseSerilog(
                (context, logger) => FileLoggingSetup.Configure(logger, context.Configuration),
                writeToProviders: true)
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));

        // Platform implementations of the core abstractions.
        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IFolderPicker, AvaloniaFolderPicker>();
        services.AddSingleton<INotifier, LoggingNotifier>();

        // Logging: one pipeline, two sinks.
        services.AddSingleton<InAppLogStore>();
        services.AddSingleton<ILoggerProvider, InAppLoggerProvider>();

        // Settings and storage.
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ISettingsStore>(sp => new SettingsStore(
            sp.GetRequiredService<IFileService>(),
            sp.GetRequiredService<ILogger<SettingsStore>>(),
            UserDataPath(sp, c => c.ConfigurationsFolder, Path.Combine("WahooFitToGarmin_Desktop", "Configurations")),
            sp.GetRequiredService<IOptions<AppConfig>>().Value.SettingsFileName ?? "Settings.json"));

        services.AddSingleton<ThemeService>();

        // The activity pipeline and everything it needs.
        services.AddSingleton<IFileSystemProbe, FileSystemProbe>();
        services.AddSingleton<IActivityFileStore, ActivityFileStore>();
        services.AddSingleton<IProcessedActivityRecord>(sp => new ProcessedActivityRecord(
            Path.Combine(
                UserDataPath(sp, c => c.ConfigurationsFolder, Path.Combine("WahooFitToGarmin_Desktop", "Configurations")),
                "ProcessedActivities.json"),
            sp.GetRequiredService<ILogger<ProcessedActivityRecord>>()));
        services.AddSingleton<IGarminClientFactory>(sp =>
            new GarminClientFactory(sp.GetRequiredService<ILogger<GarminSession>>()));
        services.AddSingleton<IGarminSession>(sp => new GarminSession(
            sp.GetRequiredService<ISettingsStore>(),
            sp.GetRequiredService<IGarminClientFactory>(),
            sp.GetRequiredService<ILogger<GarminSession>>()));
        services.AddSingleton<IActivityUploader>(sp => new GarminActivityUploader(
            sp.GetRequiredService<IGarminSession>(),
            sp.GetRequiredService<ILogger<GarminActivityUploader>>()));
        services.AddSingleton<IFolderWatcher>(sp =>
            new FileSystemFolderWatcher(sp.GetRequiredService<ILogger<ActivityDiscovery>>()));
        services.AddSingleton(sp => new FileReadinessWaiter(
            sp.GetRequiredService<IFileSystemProbe>(),
            sp.GetRequiredService<ILogger<ActivityPipeline>>()));
        services.AddSingleton(sp => new ActivityPipeline(
            sp.GetRequiredService<ISettingsStore>(),
            sp.GetRequiredService<IProcessedActivityRecord>(),
            sp.GetRequiredService<IActivityUploader>(),
            // Empty until fit-device-emulation supplies a member.
            sp.GetServices<IActivityTransformation>(),
            sp.GetRequiredService<IFileSystemProbe>(),
            sp.GetRequiredService<FileReadinessWaiter>(),
            sp.GetRequiredService<IActivityFileStore>(),
            sp.GetRequiredService<ILogger<ActivityPipeline>>()));
        services.AddSingleton(sp => new ActivityDiscovery(
            sp.GetRequiredService<ISettingsStore>(),
            sp.GetRequiredService<IProcessedActivityRecord>(),
            sp.GetRequiredService<IActivityFileStore>(),
            sp.GetRequiredService<IFolderWatcher>(),
            sp.GetRequiredService<ActivityPipeline>(),
            sp.GetRequiredService<INotifier>(),
            sp.GetRequiredService<ILogger<ActivityDiscovery>>()));

        services.AddHostedService<ActivityPipelineHostedService>();

        // View models.
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<ShellViewModel>();
    }

    private static string UserDataPath(
        IServiceProvider services,
        Func<AppConfig, string?> select,
        string fallback)
    {
        var configured = select(services.GetRequiredService<IOptions<AppConfig>>().Value);
        var relative = (string.IsNullOrWhiteSpace(configured) ? fallback : configured)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            relative);
    }

    /// <summary>
    /// Closing hides the window. The application keeps watching the folder, and
    /// the tray icon is how it is brought back or quit.
    /// </summary>
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { ShutdownMode: ShutdownMode.OnExplicitShutdown })
        {
            e.Cancel = true;
            _window?.Hide();

            // Running in the background now: menu bar only, out of the Dock and
            // out of the application switcher.
            MacOsDockVisibility.Set(visibleInDock: false);
        }
    }

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        // Back to an ordinary application before showing, so the window can
        // take focus and the icon is there while it is on screen.
        MacOsDockVisibility.Set(visibleInDock: true);

        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void OnTrayClicked(object? sender, EventArgs e) => ShowWindow();

    private void OnTrayOpen(object? sender, EventArgs e) => ShowWindow();

    private async void OnTrayQuit(object? sender, EventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        _instanceGuard?.Dispose();
        Log.CloseAndFlush();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

}
