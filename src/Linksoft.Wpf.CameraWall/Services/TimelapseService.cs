namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for managing timelapse capture sessions using FlyleafLib.
/// Captures PNG snapshots at configurable intervals.
/// </summary>
[Registration(Lifetime.Singleton)]
public class TimelapseService : ITimelapseService, IDisposable
{
    private readonly ILogger<TimelapseService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly ConcurrentDictionary<Guid, TimelapseCaptureContext> contexts = new();
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelapseService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingsService">The application settings service.</param>
    public TimelapseService(
        ILogger<TimelapseService> logger,
        IApplicationSettingsService settingsService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    /// <inheritdoc/>
    public event EventHandler<TimelapseFrameCapturedEventArgs>? FrameCaptured;

    /// <inheritdoc/>
    public void StartCapture(
        CameraConfiguration camera,
        Player player)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(player);

        // Check if timelapse is enabled for this camera
        if (!GetEffectiveEnabled(camera))
        {
            logger.LogDebug(
                "Timelapse not enabled for camera: {CameraName}",
                camera.Display.DisplayName);
            return;
        }

        // Stop existing capture if any
        StopCapture(camera.Id);

        var interval = GetEffectiveInterval(camera);
        var context = new TimelapseCaptureContext(camera, player, interval);

        if (!contexts.TryAdd(camera.Id, context))
        {
            logger.LogWarning(
                "Failed to add timelapse context for camera: {CameraName}",
                camera.Display.DisplayName);
            return;
        }

        // Start the timer
        context.Timer.Tick += (_, _) => CaptureFrame(camera.Id);
        context.Timer.Start();

        // Capture first frame immediately
        CaptureFrame(camera.Id);

        logger.LogInformation(
            "Timelapse capture started for camera: {CameraName}, interval: {Interval}",
            camera.Display.DisplayName,
            interval);
    }

    /// <inheritdoc/>
    public void StopCapture(Guid cameraId)
    {
        if (!contexts.TryRemove(cameraId, out var context))
        {
            return;
        }

        context.Timer.Stop();
        context.Dispose();

        logger.LogInformation(
            "Timelapse capture stopped for camera: {CameraName}",
            context.Camera.Display.DisplayName);
    }

    /// <inheritdoc/>
    public void StopAllCaptures()
    {
        var cameraIds = contexts.Keys.ToList();

        if (cameraIds.Count > 0)
        {
            logger.LogInformation("Stopping all timelapse captures ({Count} active)", cameraIds.Count);
        }

        foreach (var cameraId in cameraIds)
        {
            StopCapture(cameraId);
        }
    }

    /// <inheritdoc/>
    public bool IsCapturing(Guid cameraId)
        => contexts.ContainsKey(cameraId);

    /// <inheritdoc/>
    public TimeSpan GetEffectiveInterval(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var overrideInterval = camera.Overrides?.Recording.TimelapseInterval;
        if (!string.IsNullOrEmpty(overrideInterval))
        {
            return DropDownItemsFactory.ParseTimelapseInterval(overrideInterval);
        }

        var appInterval = settingsService.Recording.TimelapseInterval;
        return DropDownItemsFactory.ParseTimelapseInterval(appInterval);
    }

    /// <inheritdoc/>
    public bool GetEffectiveEnabled(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var overrideEnabled = camera.Overrides?.Recording.EnableTimelapse;
        if (overrideEnabled.HasValue)
        {
            return overrideEnabled.Value;
        }

        return settingsService.Recording.EnableTimelapse;
    }

    /// <summary>
    /// Disposes of the service resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            StopAllCaptures();
        }

        disposed = true;
    }

    private void CaptureFrame(Guid cameraId)
    {
        if (!contexts.TryGetValue(cameraId, out var context))
        {
            return;
        }

        try
        {
            var filePath = GenerateFilePath(context.Camera);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Capture snapshot using FlyleafLib
            context.Player.TakeSnapshotToFile(filePath);

            var capturedAt = DateTime.Now;

            logger.LogDebug(
                "Timelapse frame captured for camera: {CameraName}, file: {FilePath}",
                context.Camera.Display.DisplayName,
                filePath);

            // Raise event
            FrameCaptured?.Invoke(
                this,
                new TimelapseFrameCapturedEventArgs(cameraId, filePath, capturedAt));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to capture timelapse frame for camera: {CameraName}",
                context.Camera.Display.DisplayName);
        }
    }

    private string GenerateFilePath(CameraConfiguration camera)
    {
        var basePath = GetSnapshotBasePath();
        var safeDisplayName = SanitizeFilename(camera.Display.DisplayName);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        // Create timelapse subfolder structure: {SnapshotPath}/timelapse/{CameraName}/
        var timelapseFolder = Path.Combine(basePath, "timelapse", safeDisplayName);

        // Generate filename: {CameraName}_{timestamp}.png
        var filename = $"{safeDisplayName}_{timestamp}.png";

        return Path.Combine(timelapseFolder, filename);
    }

    private string GetSnapshotBasePath()
    {
        var appPath = settingsService.CameraDisplay.SnapshotPath;
        return !string.IsNullOrEmpty(appPath)
            ? appPath
            : ApplicationPaths.DefaultSnapshotsPath;
    }

    private static string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(filename.Length);

        foreach (var c in filename)
        {
            sanitized.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return sanitized.ToString();
    }
}