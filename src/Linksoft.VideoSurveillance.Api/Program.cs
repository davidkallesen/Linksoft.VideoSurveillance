var builder = WebApplication.CreateBuilder(args);

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
var recordingContentTypes = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
recordingContentTypes.Mappings[".mp4"] = "video/mp4";
recordingContentTypes.Mappings[".mkv"] = "video/x-matroska";
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(recordingPath),
    RequestPath = "/recordings-files",
    ServeUnknownFileTypes = false,
    ContentTypeProvider = recordingContentTypes,
});

// Map SignalR hub for real-time surveillance events
app.MapHub<SurveillanceHub>("/hubs/surveillance");

await app
    .RunAsync()
    .ConfigureAwait(false);