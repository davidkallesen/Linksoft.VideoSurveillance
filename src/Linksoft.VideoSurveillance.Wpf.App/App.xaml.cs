// ReSharper disable AsyncVoidEventHandlerMethod
namespace Linksoft.VideoSurveillance.Wpf.App;

#pragma warning disable MA0049, CA1724 // Type name should not match containing namespace
public partial class App
#pragma warning restore MA0049, CA1724
{
    private readonly ILogger<App>? logger;
    private readonly IHost host;

    public App()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture);

        Log.Logger = loggerConfig.CreateLogger();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var apiBaseAddress = configuration["ApiBaseAddress"] ?? "http://localhost:5000";

        host = Host
            .CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IConfiguration>(configuration);

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

                // App
                services.AddSingleton(sp => new MainWindowViewModel(
                    sp.GetRequiredService<GatewayService>(),
                    sp.GetRequiredService<SurveillanceHubService>(),
                    sp.GetRequiredService<IGitHubReleaseService>(),
                    sp.GetRequiredService<LiveViewViewModel>(),
                    sp.GetRequiredService<DashboardViewModel>(),
                    sp.GetRequiredService<CameraListViewModel>(),
                    sp.GetRequiredService<LayoutListViewModel>(),
                    sp.GetRequiredService<RecordingsViewModel>(),
                    apiBaseAddress));
                services.AddSingleton<MainWindow>();
            })
            .Build();

        logger = host
            .Services
            .GetService<ILoggerFactory>()!
            .CreateLogger<App>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        logger!.LogInformation("App initializing");

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

        logger!.LogError("CurrentDomain Unhandled Exception: {Message}", exceptionMessage);

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

        logger!.LogError("Dispatcher Unhandled Exception: {Message}", exceptionMessage);

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
        logger!.LogInformation("App starting");

        // Show splash screen
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

        // Apply theme (50%)
        RenderSplashMessage("Applying settings...", 50);
        ThemeManagerHelper.SetThemeAndAccent(Current, "Dark.Blue");

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

        logger!.LogInformation("App started");
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
        logger!.LogInformation("App closing");

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

        logger!.LogInformation("App closed");

        await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }
}