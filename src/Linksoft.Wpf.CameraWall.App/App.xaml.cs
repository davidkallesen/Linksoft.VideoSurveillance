// ReSharper disable AsyncVoidEventHandlerMethod
namespace Linksoft.Wpf.CameraWall.App;

public partial class CameraWallApp
{
    private readonly ILogger<CameraWallApp>? logger;
    private readonly IHost host;

    public CameraWallApp()
    {
        // Load advanced settings early to configure logging before Host is built
        var advancedSettings = LoadAdvancedSettingsForLogging();

        // Configure Serilog based on settings
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture);

        if (advancedSettings.EnableDebugLogging)
        {
            Directory.CreateDirectory(advancedSettings.LogPath);

            var logFile = Path.Combine(advancedSettings.LogPath, "camera-wall-.log");
            loggerConfig.WriteTo.File(
                logFile,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture);
        }

        Log.Logger = loggerConfig.CreateLogger();

        host = Host
            .CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                // Library services (auto-registered via [Registration] attribute)
                services.AddDependencyRegistrationsFromCameraWall();

                // Toast notification service (used by library and app)
                services.AddSingleton<IToastNotificationService, ToastNotificationService>();

                // App
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        logger = host
            .Services
            .GetService<ILoggerFactory>()!
            .CreateLogger<CameraWallApp>();
    }

    private static AdvancedSettings LoadAdvancedSettingsForLogging()
    {
        var settingsPath = ApplicationPaths.DefaultSettingsPath;

        if (!File.Exists(settingsPath))
        {
            return new AdvancedSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new AdvancedSettings();
            }

            var settings = JsonSerializer.Deserialize<ApplicationSettings>(
                json,
                Atc.Serialization.JsonSerializerOptionsFactory.Create());
            return settings?.Advanced ?? new AdvancedSettings();
        }
        catch (JsonException)
        {
            // Invalid JSON content - use defaults
            return new AdvancedSettings();
        }
        catch (IOException)
        {
            // File access error - use defaults
            return new AdvancedSettings();
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        logger!.LogInformation("App initializing");

        // Hook on error before app really starts
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

        logger!.LogError($"CurrentDomain Unhandled Exception: {exceptionMessage}");

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

        logger!.LogError($"Dispatcher Unhandled Exception: {exceptionMessage}");

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
            Header = Translations.ApplicationTitle,
            VersionText = $"Version {Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0)}",
        };

        splashScreen.Show();

        // Load configuration (0%)
        AppHelper.RenderLoadingInitializeMessage(logger, Translations.InitializeLoadConfiguration, 0);

        // Start host (10%)
        AppHelper.RenderLoadingInitializeMessage(logger, Translations.InitializeStartHost, 10);
        await host.StartAsync();

        // Apply UI settings (30%)
        AppHelper.RenderLoadingInitializeMessage(logger, Translations.InitializeApplyUISettings, 30);
        var settingsService = host.Services.GetRequiredService<IApplicationSettingsService>();
        ApplyStartupSettings(settingsService.General);

        // Initialize camera engine (50%)
        AppHelper.RenderLoadingInitializeMessage(logger, Translations.InitializeCameraEngine, 50);
        CameraWallEngine.Initialize();

        // Load main window (80%)
        AppHelper.RenderLoadingInitializeMessage(logger, Translations.InitializeLoadMainWindow, 80);
        var mainWindow = host.Services.GetService<MainWindow>()!;

        // Apply window state from settings
        if (settingsService.General.StartMaximized)
        {
            mainWindow.WindowState = WindowState.Maximized;
        }

        // Initialize media cleanup service (90%)
        var cleanupService = host.Services.GetRequiredService<IMediaCleanupService>();
        cleanupService.Initialize();

        // Initialize recording segmentation service
        var segmentationService = host.Services.GetRequiredService<IRecordingSegmentationService>();
        segmentationService.Initialize();

        // Complete (100%)
        AppHelper.RenderLoadingInitializeMessage(logger, Translations.InitializeComplete, 100);

        // Show main window and close splash
        // Explicitly set MainWindow so dialogs resolve the correct Owner
        // (WPF auto-assigns MainWindow to the first Window created, which is the splash screen)
        mainWindow.Show();
        MainWindow = mainWindow;
        splashScreen.Close();

        logger!.LogInformation("App started");

        _ = CheckForUpdatesAndNotifyAsync();
    }

    private async Task CheckForUpdatesAndNotifyAsync()
    {
        try
        {
            // Wait for UI to settle before checking
            await Task
                .Delay(3000)
                .ConfigureAwait(true);

            var manager = host.Services.GetRequiredService<ICameraWallManager>();
            await manager
                .CheckForUpdatesOnStartupAsync()
                .ConfigureAwait(true);

            if (!manager.IsUpdateAvailable)
            {
                return;
            }

            var toastService = host.Services.GetRequiredService<IToastNotificationService>();
            toastService.ShowInformation(
                Translations.UpdateAvailable,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Translations.UpdateAvailableMessage1,
                    manager.UpdateVersion),
                useDesktop: true,
                expirationTime: TimeSpan.FromSeconds(10),
                onClick: manager.DownloadLatestUpdate);
        }
        catch
        {
            // Silently fail - startup notification is not critical
        }
    }

    private static void ApplyStartupSettings(GeneralSettings settings)
    {
        // Apply language
        if (NumberHelper.TryParseToInt(settings.Language, out var lcid))
        {
            var cultureInfo = new CultureInfo(lcid);
            CultureManager.SetCultures(cultureInfo.Name);
        }

        // Apply theme and accent
        ThemeManagerHelper.SetThemeAndAccent(
            Current,
            $"{settings.ThemeBase}.{settings.ThemeAccent}");
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs args)
    {
        logger!.LogInformation("App closing");

        // Stop all active recordings to properly finalize recording files
        var recordingService = host.Services.GetService<IRecordingService>();
        if (recordingService is not null)
        {
            recordingService.StopAllRecordings();
            logger!.LogInformation("All recordings stopped");
        }

        // Stop recording segmentation service
        var segmentationService = host.Services.GetService<IRecordingSegmentationService>();
        segmentationService?.StopService();

        // Stop media cleanup service
        var cleanupService = host.Services.GetService<IMediaCleanupService>();
        cleanupService?.StopService();

        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();

        logger!.LogInformation("App closed");

        await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }
}