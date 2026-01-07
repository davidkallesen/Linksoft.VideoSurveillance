// ReSharper disable AsyncVoidEventHandlerMethod
namespace Linksoft.Wpf.CameraWall.App;

public partial class CameraWallApp
{
    private static AppOptions? appOptions;
    private readonly ILogger<CameraWallApp>? logger;
    private readonly IHost host;

    public static AppOptions AppOptions
    {
        get
        {
            if (appOptions is null)
            {
                LoadAppOptions();
            }

            return appOptions!;
        }

        set
        {
            appOptions = value ?? throw new ArgumentNullException(nameof(value));
            SaveAppOptions();
        }
    }

    public CameraWallApp()
    {
        host = Host
            .CreateDefaultBuilder()
            .ConfigureLogging((_, logging) =>
            {
                logging
                    .ClearProviders()
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddDebug();
            })
            .ConfigureServices((_, services) =>
            {
                // Library services (auto-registered via [Registration] attribute)
                services.AddDependencyRegistrationsFromCameraWall();

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

    private static void LoadAppOptions()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var options = new AppOptions();
        config.Bind("App", options);
        AppOptions = options;
    }

    private static void SaveAppOptions()
    {
        // TODO:
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

        LoadAppOptions();

        await host
            .StartAsync()
            .ConfigureAwait(false);

        // Initialize the CameraWall engine (FlyleafLib)
        CameraWallEngine.Initialize();

        CultureManager.SetCultures(AppOptions.ApplicationUi!.Language);

        ThemeManagerHelper.SetThemeAndAccent(
            Current,
            $"{AppOptions.ApplicationUi.ThemeBase}.{AppOptions.ApplicationUi.ThemeAccent}");

        var mainWindow = host
            .Services
            .GetService<MainWindow>()!;

        mainWindow.Show();

        logger!.LogInformation("App started");
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs args)
    {
        logger!.LogInformation("App closing");

        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();

        logger!.LogInformation("App closed");
    }
}