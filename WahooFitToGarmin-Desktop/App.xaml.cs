using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Toolkit.Uwp.Notifications;

using Serilog;

using WahooFitToGarmin_Desktop.Activation;
using WahooFitToGarmin_Desktop.Contracts.Activation;
using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Contracts.Views;
using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Services;
using WahooFitToGarmin_Desktop.Core.Settings;
using WahooFitToGarmin_Desktop.Models;
using WahooFitToGarmin_Desktop.Services;
using WahooFitToGarmin_Desktop.Services.Logging;
using WahooFitToGarmin_Desktop.Services.Platform;
using WahooFitToGarmin_Desktop.ViewModels;
using WahooFitToGarmin_Desktop.Views;

namespace WahooFitToGarmin_Desktop
{
    // For more inforation about application lifecyle events see https://docs.microsoft.com/dotnet/framework/wpf/app-development/application-management-overview

    // WPF UI elements use language en-US by default.
    // If you need to support other cultures make sure you add converters and review dates and numbers in your UI to ensure everything adapts correctly.
    // Tracking issue for improving this is https://github.com/dotnet/wpf/issues/1946
    public partial class App : Application
    {
        private IHost _host;

        public T GetService<T>()
            where T : class
            => _host.Services.GetService(typeof(T)) as T;

        public App()
        {
        }

        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // https://docs.microsoft.com/windows/uwp/design/shell/tiles-and-notifications/send-local-toast?tabs=desktop
            ToastNotificationManagerCompat.OnActivated += (toastArgs) =>
            {
                Current.Dispatcher.Invoke(async () =>
                {
                    var config = GetService<IConfiguration>();
                    config[ToastNotificationActivationHandler.ActivationArguments] = toastArgs.Argument;
                    await _host.StartAsync();
                });
            };

            // TODO: Register arguments you want to use on App initialization
            var activationArgs = new Dictionary<string, string>
            {
                { ToastNotificationActivationHandler.ActivationArguments, string.Empty }
            };
            var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // For more information about .NET generic host see  https://docs.microsoft.com/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0
            _host = Host.CreateDefaultBuilder(e.Args)
                    .ConfigureAppConfiguration(c =>
                    {
                        c.SetBasePath(appLocation);
                        c.AddInMemoryCollection(activationArgs);
                    })
                    // writeToProviders keeps the other registered providers alive,
                    // so the same entries reach both the rolling file and the
                    // in-app viewer. See design.md, D4.
                    .UseSerilog(
                        (context, loggerConfiguration) =>
                            FileLoggingSetup.Configure(loggerConfiguration, context.Configuration),
                        writeToProviders: true)
                    .ConfigureServices(ConfigureServices)
                    .Build();

            if (ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                // ToastNotificationActivator code will run after this completes and will show a window if necessary.
                return;
            }

            await _host.StartAsync();
        }

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            // TODO WTS: Register your services, viewmodels and pages here

            // App Host
            services.AddHostedService<ApplicationHostService>();

            // The activity pipeline: discovery, readiness, transformation,
            // upload, retention. Runs for the application's lifetime.
            services.AddHostedService<ActivityPipelineHostedService>();

            // Activation Handlers
            services.AddSingleton<IActivationHandler, ToastNotificationActivationHandler>();

            // Logging: one pipeline, two sinks. The rolling file is configured
            // by Serilog; the in-app viewer is fed by a logger provider.
            services.AddSingleton<ILogStore, InAppLogStore>();
            services.AddSingleton<ILoggerProvider, InAppLoggerProvider>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Services
            services.AddSingleton<IToastNotificationsService, ToastNotificationsService>();
            services.AddSingleton<IApplicationInfoService, ApplicationInfoService>();
            services.AddSingleton<ISystemService, SystemService>();
            // The settings store owns user settings: it loads on construction,
            // migrates the previous format once, and persists on every change.
            services.AddSingleton<ISettingsStore>(sp =>
            {
                var appConfig = sp.GetRequiredService<IOptions<AppConfig>>().Value;
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var relative = (appConfig.ConfigurationsFolder ?? Path.Combine("WahooFitToGarmin_Desktop", "Configurations"))
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);

                return new SettingsStore(
                    sp.GetRequiredService<IFileService>(),
                    sp.GetRequiredService<ILogger<SettingsStore>>(),
                    Path.Combine(localAppData, relative),
                    appConfig.SettingsFileName ?? "Settings.json");
            });

            // Activity pipeline and everything it depends on.
            services.AddSingleton<IFileSystemProbe, FileSystemProbe>();
            services.AddSingleton<IActivityFileStore, ActivityFileStore>();
            services.AddSingleton<IProcessedActivityRecord>(sp =>
            {
                var appConfig = sp.GetRequiredService<IOptions<AppConfig>>().Value;
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var relative = (appConfig.ConfigurationsFolder ?? Path.Combine("WahooFitToGarmin_Desktop", "Configurations"))
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);

                return new ProcessedActivityRecord(
                    Path.Combine(localAppData, relative, "ProcessedActivities.json"),
                    sp.GetRequiredService<ILogger<ProcessedActivityRecord>>());
            });
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

            // Platform implementations of the core abstractions. All three are
            // replaced by avalonia-ui-port.
            services.AddSingleton<IFolderPicker, WpfFolderPicker>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<INotifier, ToastNotifier>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Views and ViewModels
            services.AddTransient<IShellWindow, ShellWindow>();
            services.AddTransient<ShellViewModel>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();

            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();

            // Configuration
            services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
        }

        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // This handler used to be empty, so a crash left no trace anywhere.
            // The type, message and stack trace go to the log file; the viewer
            // shows the type and message.
            var logger = _host?.Services.GetService<ILogger<App>>();

            if (logger is not null)
            {
                logger.LogCritical(
                    e.Exception,
                    "Unhandled exception on the dispatcher thread: {ExceptionType}",
                    e.Exception.GetType().FullName);
            }
            else
            {
                // Too early for the host, so go straight to Serilog's static
                // logger rather than losing the exception entirely.
                Log.Fatal(e.Exception, "Unhandled exception before the host was available");
            }

            Log.CloseAndFlush();
        }
    }
}
