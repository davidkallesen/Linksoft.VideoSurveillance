// Load advanced settings early to configure logging before Host is built
// ReSharper disable SeparateLocalFunctionsWithJumpStatement
var advancedSettings = LoadAdvancedSettingsForLogging();

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture);

if (advancedSettings.EnableDebugLogging)
{
    Directory.CreateDirectory(advancedSettings.LogPath);

    var logFile = Path.Combine(advancedSettings.LogPath, "video-surveillance-api-.log");
    loggerConfig
        .MinimumLevel.Debug()
        .WriteTo.File(
            logFile,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
            formatProvider: System.Globalization.CultureInfo.InvariantCulture);
}

Log.Logger = loggerConfig.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Configure OpenAPI document generation
    builder.Services.AddOpenApi();

    // Register API handlers from the Domain project (source-generated)
    builder.Services.AddApiHandlersFromDomain();

    // Register Core service implementations for the server
    builder.Services.AddSingleton<ICameraStorageService, JsonCameraStorageService>();
    builder.Services.AddSingleton<IApplicationSettingsService, JsonApplicationSettingsService>();
    builder.Services.AddSingleton<IRecordingService, ServerRecordingService>();
    builder.Services.AddSingleton<IMotionDetectionService, ServerMotionDetectionService>();

    // Register server-specific services
    builder.Services.AddSingleton<IMediaPipelineFactory, FFmpegMediaPipelineFactory>();
    builder.Services.AddSingleton<StreamingService>();

    // Configure CORS for Blazor client (AllowCredentials required for SignalR WebSocket transport)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Register SignalR for real-time events
    builder.Services.AddSignalR();

    // Register the event broadcaster hosted service
    builder.Services.AddHostedService<SurveillanceEventBroadcaster>();

    // Register the camera connection manager for auto-recording on connect
    builder.Services.AddSingleton<IBackgroundServiceOptions>(new DefaultBackgroundServiceOptions
    {
        ServiceName = nameof(CameraConnectionManager),
        StartupDelaySeconds = 3,
        RepeatIntervalSeconds = 30,
    });

    builder.Services.AddHostedService<CameraConnectionManager>();

    var app = builder.Build();

    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // Redirect root to Scalar API docs
    app
        .MapGet("/", () => Results.Redirect("/scalar/v1"))
        .ExcludeFromDescription();

    // Map all generated REST endpoints
    app.MapEndpoints();

    // Serve HLS stream segments as static files
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            app.Services.GetRequiredService<StreamingService>().HlsOutputRoot),
        RequestPath = "/streams",
        ServeUnknownFileTypes = true,
    });

    // Serve recorded video files as static files
    var recordingPath = app.Services.GetRequiredService<IApplicationSettingsService>()
        .Recording.RecordingPath;

    Directory.CreateDirectory(recordingPath);

    var recordingContentTypes = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider
    {
        Mappings =
        {
            [".mp4"] = "video/mp4",
            [".mkv"] = "video/x-matroska",
        },
    };

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(recordingPath),
        RequestPath = "/recordings-files",
        ServeUnknownFileTypes = false,
        ContentTypeProvider = recordingContentTypes,
    });

    // Map SignalR hub for real-time surveillance events
    app.MapHub<SurveillanceHub>("/hubs/surveillance");

    Log.Information("Video Surveillance API starting");

    await app
        .RunAsync()
        .ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Video Surveillance API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

static AdvancedSettings LoadAdvancedSettingsForLogging()
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
        return new AdvancedSettings();
    }
    catch (IOException)
    {
        return new AdvancedSettings();
    }
}