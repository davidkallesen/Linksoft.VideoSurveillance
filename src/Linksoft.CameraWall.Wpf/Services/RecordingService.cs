// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.CameraWall.Wpf.Services;

/// <summary>
/// Service for managing camera recording sessions.
/// </summary>
[Registration(Lifetime.Singleton)]
public partial class RecordingService : IRecordingService, IDisposable
{
    // Cooldown entries older than this are stale — no realistic cooldown
    // setting exceeds a few minutes, so 24h is a wide safety margin.
    private static readonly TimeSpan CooldownEntryMaxAge = TimeSpan.FromHours(24);

    private readonly ILogger<RecordingService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly IThumbnailGeneratorService thumbnailService;
    private readonly ICameraStorageService cameraStorageService;
    private readonly ConcurrentDictionary<Guid, RecordingSession> sessions = new();
    private readonly ConcurrentDictionary<Guid, IMediaPipeline> pipelines = new();
    private readonly ConcurrentDictionary<Guid, DispatcherTimer> postMotionTimers = new();
    private readonly ConcurrentDictionary<Guid, DateTime> recordingCooldowns = new();
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
        IMediaPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(pipeline);

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

            // Start recording via media pipeline
            pipeline.StartRecording(filePath);

            // Create session
            var session = new RecordingSession(camera.Id, filePath, isManualRecording: true);
            if (!sessions.TryAdd(camera.Id, session))
            {
                pipeline.StopRecording();
                return false;
            }

            // Store pipeline reference for later stop
            pipelines[camera.Id] = pipeline;

            LogRecordingStarted(camera.Display.DisplayName, filePath);

            // Raise event
            OnRecordingStateChanged(camera.Id, RecordingState.Idle, RecordingState.Recording, filePath);

            // Start thumbnail capture with configured tile count
            var tileCount = GetEffectiveThumbnailTileCount(camera);
            thumbnailService.StartCapture(camera.Id, pipeline, filePath, tileCount);

            return true;
        }
        catch (Exception ex)
        {
            LogRecordingStartFailed(ex, camera.Display.DisplayName);
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

        // Stop recording via media pipeline
        // Note: Pipeline is owned by CameraTile, not RecordingService - we just hold a reference for recording
#pragma warning disable CA2000 // Pipeline is owned and disposed by CameraTile
        if (pipelines.TryRemove(cameraId, out var pipeline))
#pragma warning restore CA2000
        {
            try
            {
                pipeline.StopRecording();
                LogRecordingStopped(cameraId, session.CurrentFilePath);
            }
            catch (Exception ex)
            {
                LogRecordingStopError(ex, cameraId);
            }
        }

        // Track cooldown for motion-triggered recordings
        if (!session.IsManualRecording)
        {
            recordingCooldowns[cameraId] = DateTime.UtcNow;
            PruneStaleCooldowns();
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
        IMediaPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(pipeline);

        // If already recording manually, don't interfere
        if (sessions.TryGetValue(camera.Id, out var existingSession) && existingSession.IsManualRecording)
        {
            return true;
        }

        // If already recording motion, just update the timestamp (no cooldown check)
        if (existingSession is not null)
        {
            UpdateMotionTimestamp(camera.Id);
            return true;
        }

        // Check recording cooldown before starting a NEW recording
        if (recordingCooldowns.TryGetValue(camera.Id, out var lastStopTime))
        {
            var cooldownSeconds = GetEffectiveRecordingCooldown(camera);
            if (cooldownSeconds > 0 && (DateTime.UtcNow - lastStopTime).TotalSeconds < cooldownSeconds)
            {
                return false;
            }
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

            // Start recording via media pipeline
            pipeline.StartRecording(filePath);

            // Create session
            var session = new RecordingSession(camera.Id, filePath, isManualRecording: false)
            {
                LastMotionTime = DateTime.UtcNow,
            };

            if (!sessions.TryAdd(camera.Id, session))
            {
                pipeline.StopRecording();
                return false;
            }

            // Store pipeline reference for later stop
            pipelines[camera.Id] = pipeline;

            // Start post-motion timer
            StartPostMotionTimer(camera);

            LogMotionRecordingStarted(camera.Display.DisplayName, filePath);

            // Raise event
            OnRecordingStateChanged(camera.Id, RecordingState.Idle, RecordingState.RecordingMotion, filePath);

            // Start thumbnail capture with configured tile count
            var tileCount = GetEffectiveThumbnailTileCount(camera);
            thumbnailService.StartCapture(camera.Id, pipeline, filePath, tileCount);

            return true;
        }
        catch (Exception ex)
        {
            LogMotionRecordingStartFailed(ex, camera.Display.DisplayName);
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
            LogStoppingAllRecordings(cameraIds.Count);
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
            LogNoActiveSessionForSegment(cameraId);
            return false;
        }

        // Get the pipeline
        if (!pipelines.TryGetValue(cameraId, out var pipeline))
        {
            LogNoPipelineForSegment(cameraId);
            return false;
        }

        // Get camera configuration
        var camera = cameraStorageService.GetCameraById(cameraId);
        if (camera is null)
        {
            LogCameraNotFoundForSegment(cameraId);
            return false;
        }

        // Preserve recording type and motion state
        var isManualRecording = session.IsManualRecording;
        var lastMotionTime = session.LastMotionTime;
        var oldState = session.State;
        var oldFilePath = session.CurrentFilePath;

        LogSegmentingRecording(camera.Display.DisplayName, oldFilePath);

        try
        {
            // 1. Stop thumbnail capture for the old segment
            thumbnailService.StopCapture(cameraId);

            // 2. Stop recording via media pipeline
            pipeline.StopRecording();

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

            // 6. Start new recording via media pipeline
            pipeline.StartRecording(newFilePath);

            // 7. Create new session with preserved state
            var newState = isManualRecording ? RecordingState.Recording : RecordingState.RecordingMotion;
            var newSession = new RecordingSession(cameraId, newFilePath, isManualRecording)
            {
                LastMotionTime = lastMotionTime,
                State = newState,
            };

            if (!sessions.TryAdd(cameraId, newSession))
            {
                pipeline.StopRecording();
                LogFailedToAddNewSession(cameraId);
                return false;
            }

            // Keep pipeline reference
            pipelines[cameraId] = pipeline;

            LogRecordingSegmented(camera.Display.DisplayName, newFilePath);

            // 8. Fire state changed events for new segment start
            OnRecordingStateChanged(cameraId, RecordingState.Idle, newState, newFilePath);

            // 9. Start thumbnail capture for new segment with configured tile count
            var tileCount = GetEffectiveThumbnailTileCount(camera);
            thumbnailService.StartCapture(cameraId, pipeline, newFilePath, tileCount);

            return true;
        }
        catch (Exception ex)
        {
            LogSegmentRecordingFailed(ex, camera.Display.DisplayName);
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
        var overridePath = camera.Overrides?.Recording.RecordingPath;
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
        var overrideFormat = camera.Overrides?.Recording.RecordingFormat;
        if (!string.IsNullOrEmpty(overrideFormat))
        {
            return overrideFormat;
        }

        var appFormat = settingsService.Recording.RecordingFormat;
        return !string.IsNullOrEmpty(appFormat)
            ? appFormat
            : DropDownItemsFactory.DefaultRecordingFormat;
    }

    private int GetEffectiveRecordingCooldown(CameraConfiguration camera)
    {
        var overrideCooldown = camera.Overrides?.MotionDetection.CooldownSeconds;
        if (overrideCooldown.HasValue)
        {
            return overrideCooldown.Value;
        }

        return settingsService.MotionDetection.CooldownSeconds;
    }

    private int GetEffectivePostMotionDuration(CameraConfiguration camera)
    {
        var overrideDuration = camera.Overrides?.MotionDetection.PostMotionDurationSeconds;
        if (overrideDuration.HasValue)
        {
            return overrideDuration.Value;
        }

        return settingsService.MotionDetection.PostMotionDurationSeconds;
    }

    private int GetEffectiveThumbnailTileCount(CameraConfiguration camera)
    {
        var overrideTileCount = camera.Overrides?.Recording.ThumbnailTileCount;
        if (overrideTileCount.HasValue)
        {
            return overrideTileCount.Value;
        }

        return settingsService.Recording.ThumbnailTileCount;
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

        // Catches all exceptions; an unhandled exception in a DispatcherTimer.Tick
        // handler crashes the WPF dispatcher.
        timer.Tick += (_, _) =>
        {
            try
            {
                CheckPostMotionState(cameraId, postMotionSeconds);
            }
            catch (Exception ex)
            {
                LogPostMotionTickFailed(ex, cameraId);
            }
        };

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
            LogPostMotionPeriodElapsed(cameraId);
            StopRecording(cameraId);
        }
        else if (session.State == RecordingState.RecordingMotion && elapsed >= 1)
        {
            // Motion stopped (no update for 1+ second), transition to post-motion state
            var oldState = session.State;
            session.State = RecordingState.RecordingPostMotion;
            LogTransitioningToPostMotion(
                cameraId,
                (postMotionSeconds - elapsed).ToString("F0", CultureInfo.InvariantCulture));
            OnRecordingStateChanged(cameraId, oldState, RecordingState.RecordingPostMotion, session.CurrentFilePath);
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

    // Bounds recordingCooldowns over weeks of motion events. Iterating
    // ConcurrentDictionary is safe; TryRemove is atomic.
    private void PruneStaleCooldowns()
    {
        var cutoff = DateTime.UtcNow - CooldownEntryMaxAge;
        foreach (var entry in recordingCooldowns)
        {
            if (entry.Value < cutoff)
            {
                recordingCooldowns.TryRemove(entry.Key, out _);
            }
        }
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