// ReSharper disable AsyncVoidEventHandlerMethod
namespace Linksoft.VideoSurveillance.Wpf.App;

#pragma warning disable MA0049, CA1724 // Type name should not match containing namespace
public partial class App
#pragma warning restore MA0049, CA1724
{
    private const string AspireEnvVar = "services__api__https__0";
    private const string AspireEnvVarHttp = "services__api__http__0";
    private const string ChooseServerFlag = "--choose-server";

    private ILogger<App>? logger;
    private IHost? host;
    private bool isAspireManaged;

    public App()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture);

        Log.Logger = loggerConfig.CreateLogger();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        Current.DispatcherUnhandledException += ApplicationOnDispatcherUnhandledException;
        base.OnStartup(e);

        if (Debugger.IsAttached)
        {
            BindingErrorTraceListener.StartTrace();
        }
    }

    private void CurrentDomainUnhandledException(
        object sender,
        UnhandledExceptionEventArgs args)
    {
        if (args is not { ExceptionObject: Exception ex })
        {
            return;
        }

        var exceptionMessage = ex.GetMessage(true);

        logger?.LogError("CurrentDomain Unhandled Exception: {Message}", exceptionMessage);

        MessageBox.Show(
            exceptionMessage,
            "CurrentDomain Unhandled Exception",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ApplicationOnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs args)
    {
        var exceptionMessage = args.Exception.GetMessage(true);
        if (exceptionMessage.Contains(
                "BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'ComboBoxItem'",
                StringComparison.Ordinal))
        {
            args.Handled = true;
            return;
        }

        logger?.LogError("Dispatcher Unhandled Exception: {Message}", exceptionMessage);

        MessageBox.Show(
            exceptionMessage,
            "Dispatcher Unhandled Exception",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        args.Handled = true;
        Shutdown(-1);
    }

    private async void ApplicationStartup(
        object sender,
        StartupEventArgs args)
    {
        // Configure application paths before anything uses them
        ApplicationPaths.Configure("VideoSurveillanceApp");

        // 1. Resolve API base address
        var apiBaseAddress = ResolveApiBaseAddress(args.Args);
        if (apiBaseAddress is null)
        {
            // Dialog cancelled — exit
            Shutdown();
            return;
        }

        // 2. Build DI host
        host = BuildHost(apiBaseAddress);
        logger = host.Services.GetService<ILoggerFactory>()!.CreateLogger<App>();

        logger.LogInformation("App starting");

        // 3. Show splash screen
        var splashScreen = new SplashScreenWindow
        {
            Header = "Linksoft Video Surveillance",
            VersionText = $"Version {Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0)}",
        };

        splashScreen.Show();

        // Loading configuration (0%)
        RenderSplashMessage("Loading configuration...", 0);

        // Initialize video engine (10%)
        RenderSplashMessage("Initializing video engine...", 10);
        VideoEngineBootstrap.Initialize();

        // Start host (20%)
        RenderSplashMessage("Starting services...", 20);
        await host.StartAsync();

        // Apply theme from local settings (50%)
        RenderSplashMessage("Applying settings...", 50);
        var appSettingsService = host.Services.GetRequiredService<IApplicationSettingsService>();
        var generalSettings = appSettingsService.General;
        ThemeManagerHelper.SetThemeAndAccent(Current, $"{generalSettings.ThemeBase}.{generalSettings.ThemeAccent}");

        // Connect SignalR hub (70%)
        RenderSplashMessage("Connecting to server...", 70);
        var hubService = host.Services.GetRequiredService<SurveillanceHubService>();
        try
        {
            await hubService.ConnectAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to SignalR hub during startup");
        }

        // Load main window (80%)
        RenderSplashMessage("Loading main window...", 80);
        var mainWindow = host.Services.GetRequiredService<MainWindow>();

        // Complete (100%)
        RenderSplashMessage("Ready", 100);

        // Show main window and close splash
        mainWindow.Show();
        MainWindow = mainWindow;
        splashScreen.Close();

        // Start notification coordinator (after window is shown for toast visual tree)
        var coordinator = host.Services.GetRequiredService<NotificationCoordinator>();
        var prefsService = new NotificationPreferencesService();
        prefsService.Load();
        coordinator.Preferences = prefsService.Preferences;

        try
        {
            var gatewayService = host.Services.GetRequiredService<GatewayService>();
            var cameras = await gatewayService.GetCamerasAsync().ConfigureAwait(true);
            coordinator.UpdateCameraNameCache(cameras);
        }
        catch (HttpRequestException)
        {
            // Camera name cache will use fallback names
        }

        coordinator.Start();

        // Load initial stats and start latency monitoring
        var mainViewModel = host.Services.GetRequiredService<MainWindowViewModel>();
        await mainViewModel.LoadInitialStatsAsync().ConfigureAwait(true);
        mainViewModel.StartLatencyMonitoring();

        logger.LogInformation("App started");
    }

    private string? ResolveApiBaseAddress(string[] commandLineArgs)
    {
        // Check Aspire environment variables
        var aspireUrl = Environment.GetEnvironmentVariable(AspireEnvVar)
                     ?? Environment.GetEnvironmentVariable(AspireEnvVarHttp);
        if (!string.IsNullOrEmpty(aspireUrl))
        {
            isAspireManaged = true;
            return aspireUrl.TrimEnd('/');
        }

        // Load server profile service
        var profileService = new Services.ServerProfileService();
        profileService.Load();

        var forceChooseServer = Array.Exists(commandLineArgs, arg =>
            string.Equals(arg, ChooseServerFlag, StringComparison.OrdinalIgnoreCase));

        // If we have a last-used profile and no --choose-server flag, auto-connect
        if (!forceChooseServer)
        {
            var lastUsed = profileService.GetLastUsedProfile();
            if (lastUsed is not null)
            {
                return lastUsed.Url.TrimEnd('/');
            }
        }

        // Show server connection dialog
        var vm = new Dialogs.ServerConnectionDialogViewModel(profileService);
        var dialog = new Dialogs.ServerConnectionDialog(vm);

        var result = dialog.ShowDialog();
        if (result != true || string.IsNullOrEmpty(vm.ResolvedUrl))
        {
            return null;
        }

        return vm.ResolvedUrl;
    }

    private IHost BuildHost(string apiBaseAddress)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        return Host
            .CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IConfiguration>(configuration);

                // Local application settings (stored at ProgramData\Linksoft\VideoSurveillanceApp\)
                services.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>();

                // Video engine services
                services.AddSingleton<IGpuAcceleratorFactory, D3D11AcceleratorFactory>();
                services.AddSingleton<IVideoPlayerFactory, VideoPlayerFactory>();

                // Named HttpClient (matches Blazor.App pattern)
                services
                    .AddHttpClient("VideoSurveillance-ApiClient", client =>
                    {
                        client.BaseAddress = new Uri(apiBaseAddress);
                    })
                    .AddStandardResilienceHandler();

                // Source-generated endpoint DI
                services.AddVideoSurveillanceEndpoints();

                // API client services
                services.AddSingleton(sp =>
                {
                    var svc = ActivatorUtilities.CreateInstance<GatewayService>(sp);
                    svc.ApiBaseUrl = apiBaseAddress;
                    return svc;
                });

                services.AddSingleton(new SurveillanceHubService(apiBaseAddress));

                // GitHub release service
                services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();

                // Toast notification + notification coordinator
                services.AddSingleton<IToastNotificationService, ToastNotificationService>();
                services.AddSingleton<NotificationCoordinator>();

                // ViewModels
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<CameraListViewModel>();
                services.AddSingleton<LayoutListViewModel>();
                services.AddSingleton(sp => new LiveViewViewModel(
                    sp.GetRequiredService<GatewayService>(),
                    sp.GetRequiredService<SurveillanceHubService>(),
                    sp.GetRequiredService<IVideoPlayerFactory>(),
                    apiBaseAddress));
                services.AddSingleton(sp => new RecordingsViewModel(
                    sp.GetRequiredService<GatewayService>(),
                    apiBaseAddress));
                services.AddSingleton<NotificationHistoryViewModel>();

                // App
                var aspireManaged = isAspireManaged;
                services.AddSingleton(sp => new MainWindowViewModel(
                    sp.GetRequiredService<GatewayService>(),
                    sp.GetRequiredService<SurveillanceHubService>(),
                    sp.GetRequiredService<IGitHubReleaseService>(),
                    sp.GetRequiredService<IApplicationSettingsService>(),
                    sp.GetRequiredService<LiveViewViewModel>(),
                    sp.GetRequiredService<DashboardViewModel>(),
                    sp.GetRequiredService<CameraListViewModel>(),
                    sp.GetRequiredService<LayoutListViewModel>(),
                    sp.GetRequiredService<RecordingsViewModel>(),
                    sp.GetRequiredService<NotificationHistoryViewModel>(),
                    apiBaseAddress,
                    aspireManaged));
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    private static void RenderSplashMessage(
        string message,
        int percentage)
    {
        MessageListener.Instance.ReceiveMessage(message);
        PercentListener.Instance.ReceivePercent(percentage);
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs args)
    {
        if (host is null)
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
            return;
        }

        logger?.LogInformation("App closing");

        // Stop latency monitoring
        var mainViewModel = host.Services.GetService<MainWindowViewModel>();
        mainViewModel?.StopLatencyMonitoring();

        // Disconnect SignalR hub
        var hubService = host.Services.GetService<SurveillanceHubService>();
        if (hubService is not null)
        {
            await hubService.DisconnectAsync().ConfigureAwait(false);
        }

        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();

        logger?.LogInformation("App closed");

        await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }
}
