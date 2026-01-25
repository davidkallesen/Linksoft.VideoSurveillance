// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for managing camera recording sessions using FlyleafLib.
/// </summary>
[Registration(Lifetime.Singleton)]
public class RecordingService : IRecordingService, IDisposable
{
    private readonly ILogger<RecordingService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly IThumbnailGeneratorService thumbnailService;
    private readonly ICameraStorageService cameraStorageService;
    private readonly ConcurrentDictionary<Guid, RecordingSession> sessions = new();
    private readonly ConcurrentDictionary<Guid, Player> players = new();
    private readonly ConcurrentDictionary<Guid, DispatcherTimer> postMotionTimers = new();
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="thumbnailService">The thumbnail generator service.</param>
    /// <param name="cameraStorageService">The camera storage service.</param>
    public RecordingService(
        ILogger<RecordingService> logger,
        IApplicationSettingsService settingsService,
        IThumbnailGeneratorService thumbnailService,
        ICameraStorageService cameraStorageService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
        this.cameraStorageService = cameraStorageService ?? throw new ArgumentNullException(nameof(cameraStorageService));
    }

    /// <inheritdoc/>
    public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;

    /// <inheritdoc/>
    public RecordingState GetRecordingState(Guid cameraId)
    {
        if (sessions.TryGetValue(cameraId, out var session))
        {
            return session.State;
        }

        return RecordingState.Idle;
    }

    /// <inheritdoc/>
    public RecordingSession? GetSession(Guid cameraId)
    {
        sessions.TryGetValue(cameraId, out var session);
        return session;
    }

    /// <inheritdoc/>
    public bool StartRecording(
        CameraConfiguration camera,
        Player player)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(player);

        // Check if already recording
        if (sessions.ContainsKey(camera.Id))
        {
            return false;
        }

        var format = GetEffectiveRecordingFormat(camera);
        var filePath = GenerateRecordingFilename(camera, format);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Start FlyleafLib recording (useRecommendedExtension=false to use our format)
            player.StartRecording(ref filePath, useRecommendedExtension: false);

            // Create session
            var session = new RecordingSession(camera.Id, filePath, isManualRecording: true);
            if (!sessions.TryAdd(camera.Id, session))
            {
                player.StopRecording();
                return false;
            }

            // Store player reference for later stop
            players[camera.Id] = player;

            logger.LogInformation(
                "Recording started for camera: '{CameraName}', file: {FilePath}",
                camera.Display.DisplayName,
                filePath);

            // Raise event
            OnRecordingStateChanged(camera.Id, RecordingState.Idle, RecordingState.Recording, filePath);

            // Start thumbnail capture
            thumbnailService.StartCapture(camera.Id, player, filePath);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start recording for camera: {CameraName}", camera.Display.DisplayName);
            return false;
        }
    }

    /// <inheritdoc/>
    public void StopRecording(Guid cameraId)
    {
        // Stop thumbnail capture first (generates thumbnail with captured frames)
        thumbnailService.StopCapture(cameraId);

        if (!sessions.TryRemove(cameraId, out var session))
        {
            return;
        }

        var oldState = session.State;

        // Stop post-motion timer if running
        StopPostMotionTimer(cameraId);

        // Stop FlyleafLib recording
        // Note: Player is owned by CameraTile, not RecordingService - we just hold a reference for recording
#pragma warning disable CA2000 // Player is owned and disposed by CameraTile
        if (players.TryRemove(cameraId, out var player))
#pragma warning restore CA2000
        {
            try
            {
                player.StopRecording();
                logger.LogInformation(
                    "Recording stopped for camera ID: {CameraId}, file: {FilePath}",
                    cameraId,
                    session.CurrentFilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping recording for camera ID: {CameraId}", cameraId);
            }
        }

        // Raise event
        OnRecordingStateChanged(cameraId, oldState, RecordingState.Idle, session.CurrentFilePath);
    }

    /// <inheritdoc/>
    public bool IsRecording(Guid cameraId)
        => sessions.ContainsKey(cameraId);

    /// <inheritdoc/>
    public bool TriggerMotionRecording(
        CameraConfiguration camera,
        Player player)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(player);

        // If already recording manually, don't interfere
        if (sessions.TryGetValue(camera.Id, out var existingSession) && existingSession.IsManualRecording)
        {
            return true;
        }

        // If already recording motion, just update the timestamp
        if (existingSession is not null)
        {
            UpdateMotionTimestamp(camera.Id);
            return true;
        }

        var format = GetEffectiveRecordingFormat(camera);
        var filePath = GenerateRecordingFilename(camera, format);

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Start FlyleafLib recording (useRecommendedExtension=false to use our format)
            player.StartRecording(ref filePath, useRecommendedExtension: false);

            // Create session
            var session = new RecordingSession(camera.Id, filePath, isManualRecording: false)
            {
                LastMotionTime = DateTime.UtcNow,
            };

            if (!sessions.TryAdd(camera.Id, session))
            {
                player.StopRecording();
                return false;
            }

            // Store player reference for later stop
            players[camera.Id] = player;

            // Start post-motion timer
            StartPostMotionTimer(camera);

            logger.LogInformation(
                "Motion recording started for camera: {CameraName}, file: {FilePath}",
                camera.Display.DisplayName,
                filePath);

            // Raise event
            OnRecordingStateChanged(camera.Id, RecordingState.Idle, RecordingState.RecordingMotion, filePath);

            // Start thumbnail capture
            thumbnailService.StartCapture(camera.Id, player, filePath);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start motion recording for camera: {CameraName}", camera.Display.DisplayName);
            return false;
        }
    }

    /// <inheritdoc/>
    public void UpdateMotionTimestamp(Guid cameraId)
    {
        if (!sessions.TryGetValue(cameraId, out var session) || session.IsManualRecording)
        {
            return;
        }

        session.LastMotionTime = DateTime.UtcNow;

        // If in post-motion state, transition back to recording motion
        if (session.State == RecordingState.RecordingPostMotion)
        {
            var oldState = session.State;
            session.State = RecordingState.RecordingMotion;
            OnRecordingStateChanged(cameraId, oldState, RecordingState.RecordingMotion, session.CurrentFilePath);
        }
    }

    /// <inheritdoc/>
    public string GenerateRecordingFilename(
        CameraConfiguration camera,
        string format)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var basePath = GetEffectiveRecordingPath(camera);
        var safeDisplayName = SanitizeFilename(camera.Display.DisplayName);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

        // Create camera subfolder
        var cameraFolder = Path.Combine(basePath, safeDisplayName);

        // Generate filename with the specified format extension
        var filename = $"{safeDisplayName}_{timestamp}.{format}";

        return Path.Combine(cameraFolder, filename);
    }

    /// <inheritdoc/>
    public void StopAllRecordings()
    {
        // Get all camera IDs to stop
        var cameraIds = sessions.Keys.ToList();

        if (cameraIds.Count > 0)
        {
            logger.LogInformation("Stopping all recordings ({Count} active sessions)", cameraIds.Count);
        }

        foreach (var cameraId in cameraIds)
        {
            StopRecording(cameraId);
        }

        // Stop all timers
        foreach (var timer in postMotionTimers.Values)
        {
            timer.Stop();
        }

        postMotionTimers.Clear();
    }

    /// <inheritdoc/>
    public bool SegmentRecording(Guid cameraId)
    {
        // Get the current session
        if (!sessions.TryGetValue(cameraId, out var session))
        {
            logger.LogDebug("No active session for camera ID: {CameraId}, cannot segment", cameraId);
            return false;
        }

        // Get the player
        if (!players.TryGetValue(cameraId, out var player))
        {
            logger.LogWarning("No player found for camera ID: {CameraId}, cannot segment", cameraId);
            return false;
        }

        // Get camera configuration
        var camera = cameraStorageService.GetCameraById(cameraId);
        if (camera is null)
        {
            logger.LogWarning("Camera not found for ID: {CameraId}, cannot segment", cameraId);
            return false;
        }

        // Preserve recording type and motion state
        var isManualRecording = session.IsManualRecording;
        var lastMotionTime = session.LastMotionTime;
        var oldState = session.State;
        var oldFilePath = session.CurrentFilePath;

        logger.LogInformation(
            "Segmenting recording for camera: {CameraName}, old file: {OldFilePath}",
            camera.Display.DisplayName,
            oldFilePath);

        try
        {
            // 1. Stop thumbnail capture for the old segment
            thumbnailService.StopCapture(cameraId);

            // 2. Stop FlyleafLib recording
            player.StopRecording();

            // 3. Remove old session
            sessions.TryRemove(cameraId, out _);

            // 4. Fire state changed event for old segment completion
            OnRecordingStateChanged(cameraId, oldState, RecordingState.Idle, oldFilePath);

            // 5. Generate new filename with current timestamp
            var format = GetEffectiveRecordingFormat(camera);
            var newFilePath = GenerateRecordingFilename(camera, format);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(newFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 6. Start new FlyleafLib recording (useRecommendedExtension=false to use our format)
            player.StartRecording(ref newFilePath, useRecommendedExtension: false);

            // 7. Create new session with preserved state
            var newState = isManualRecording ? RecordingState.Recording : RecordingState.RecordingMotion;
            var newSession = new RecordingSession(cameraId, newFilePath, isManualRecording)
            {
                LastMotionTime = lastMotionTime,
                State = newState,
            };

            if (!sessions.TryAdd(cameraId, newSession))
            {
                player.StopRecording();
                logger.LogError("Failed to add new session for camera ID: {CameraId}", cameraId);
                return false;
            }

            // Keep player reference
            players[cameraId] = player;

            logger.LogInformation(
                "Recording segmented for camera: {CameraName}, new file: {NewFilePath}",
                camera.Display.DisplayName,
                newFilePath);

            // 8. Fire state changed events for new segment start
            OnRecordingStateChanged(cameraId, RecordingState.Idle, newState, newFilePath);

            // 9. Start thumbnail capture for new segment
            thumbnailService.StartCapture(cameraId, player, newFilePath);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to segment recording for camera: {CameraName}", camera.Display.DisplayName);
            return false;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<RecordingSession> GetActiveSessions()
        => sessions.Values.ToList();

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
            StopAllRecordings();
        }

        disposed = true;
    }

    private string GetEffectiveRecordingPath(CameraConfiguration camera)
    {
        var overridePath = camera.Overrides?.RecordingPath;
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath;
        }

        var appPath = settingsService.Recording.RecordingPath;
        return !string.IsNullOrEmpty(appPath)
            ? appPath
            : ApplicationPaths.DefaultRecordingsPath;
    }

    private string GetEffectiveRecordingFormat(CameraConfiguration camera)
    {
        var overrideFormat = camera.Overrides?.RecordingFormat;
        if (!string.IsNullOrEmpty(overrideFormat))
        {
            return overrideFormat;
        }

        var appFormat = settingsService.Recording.RecordingFormat;
        return !string.IsNullOrEmpty(appFormat)
            ? appFormat
            : DropDownItemsFactory.DefaultRecordingFormat;
    }

    private int GetEffectivePostMotionDuration(CameraConfiguration camera)
    {
        var overrideDuration = camera.Overrides?.PostMotionDurationSeconds;
        if (overrideDuration.HasValue)
        {
            return overrideDuration.Value;
        }

        return settingsService.Recording.MotionDetection?.PostMotionDurationSeconds ?? 10;
    }

    private void StartPostMotionTimer(CameraConfiguration camera)
    {
        var postMotionSeconds = GetEffectivePostMotionDuration(camera);
        var cameraId = camera.Id;

        // Stop existing timer if any
        StopPostMotionTimer(cameraId);

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };

        timer.Tick += (_, _) => CheckPostMotionState(cameraId, postMotionSeconds);

        postMotionTimers[cameraId] = timer;
        timer.Start();
    }

    private void StopPostMotionTimer(Guid cameraId)
    {
        if (postMotionTimers.TryRemove(cameraId, out var timer))
        {
            timer.Stop();
        }
    }

    private void CheckPostMotionState(
        Guid cameraId,
        int postMotionSeconds)
    {
        if (!sessions.TryGetValue(cameraId, out var session) || session.IsManualRecording)
        {
            StopPostMotionTimer(cameraId);
            return;
        }

        var lastMotion = session.LastMotionTime ?? session.StartTime;
        var elapsed = (DateTime.UtcNow - lastMotion).TotalSeconds;

        if (elapsed >= postMotionSeconds)
        {
            // Post-motion period elapsed, stop recording
            StopRecording(cameraId);
        }
        else if (session.State == RecordingState.RecordingMotion && elapsed >= 0)
        {
            // Motion stopped, transition to post-motion state
            // This is triggered when no motion update has occurred for a bit
            // The actual transition to PostMotion is handled by lack of UpdateMotionTimestamp calls
            // For now, we just check if we need to stop
        }
    }

    private void OnRecordingStateChanged(
        Guid cameraId,
        RecordingState oldState,
        RecordingState newState,
        string? filePath)
    {
        RecordingStateChanged?.Invoke(
            this,
            new RecordingStateChangedEventArgs(cameraId, oldState, newState, filePath));
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