namespace Linksoft.VideoSurveillance.Wpf.App;

#pragma warning disable MA0049, CA1724 // Type name should not match containing namespace
public partial class App
#pragma warning restore MA0049, CA1724
{
    private readonly IHost host;

    public App()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture);

        Log.Logger = loggerConfig.CreateLogger();

        host = Host
            .CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    private async void ApplicationStartup(
        object sender,
        StartupEventArgs args)
    {
        await host.StartAsync().ConfigureAwait(true);

        var mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        MainWindow = mainWindow;
    }

    private async void ApplicationExit(
        object sender,
        ExitEventArgs args)
    {
        await host
            .StopAsync()
            .ConfigureAwait(false);

        host.Dispose();

        await Log.CloseAndFlushAsync().ConfigureAwait(false);
    }
}