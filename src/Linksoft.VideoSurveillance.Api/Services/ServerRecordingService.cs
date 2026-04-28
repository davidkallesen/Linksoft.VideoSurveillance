namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side implementation of <see cref="IRecordingService"/> using FFmpeg media pipelines.
/// </summary>
public sealed partial class ServerRecordingService : IRecordingService, IDisposable
{
    private readonly IApplicationSettingsService settingsService;
    private readonly ICameraStorageService cameraStorageService;
    private readonly ILogger<ServerRecordingService> logger;
    private readonly ConcurrentDictionary<Guid, RecordingSession> sessions = new();
    private readonly ConcurrentDictionary<Guid, IMediaPipeline> pipelines = new();

    public ServerRecordingService(
        IApplicationSettingsService settingsService,
        ICameraStorageService cameraStorageService,
        ILogger<ServerRecordingService> logger)
    {
        this.settingsService = settingsService;
        this.cameraStorageService = cameraStorageService;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;

    /// <inheritdoc/>
    public RecordingState GetRecordingState(Guid cameraId)
        => sessions.ContainsKey(cameraId) ? RecordingState.Recording : RecordingState.Idle;

    /// <inheritdoc/>
    public RecordingSession? GetSession(Guid cameraId)
        => sessions.GetValueOrDefault(cameraId);

    /// <inheritdoc/>
    public bool StartRecording(
        CameraConfiguration camera,
        IMediaPipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(pipeline);

        if (sessions.ContainsKey(camera.Id))
        {
            return false;
        }

        var filePath = GenerateRecordingFilename(camera, settingsService.Recording.RecordingFormat);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        pipeline.StartRecording(filePath);
        pipelines[camera.Id] = pipeline;

        var session = new RecordingSession(camera.Id, filePath, isManualRecording: true);

        sessions[camera.Id] = session;

        RaiseStateChanged(camera.Id, RecordingState.Idle, RecordingState.Recording, filePath);
        LogRecordingStarted(camera.Id, filePath);

        return true;
    }

    /// <inheritdoc/>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Pipeline ownership belongs to the caller; this service only holds a non-owning reference for the recording lifetime.")]
    public void StopRecording(Guid cameraId)
    {
        if (!sessions.TryRemove(cameraId, out var session))
        {
            return;
        }

        if (pipelines.TryRemove(cameraId, out var pipeline))
        {
            pipeline.StopRecording();
        }

        RaiseStateChanged(cameraId, RecordingState.Recording, RecordingState.Idle, session.CurrentFilePath);
        LogRecordingStopped(cameraId);
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

        if (IsRecording(camera.Id))
        {
            return false;
        }

        return StartRecording(camera, pipeline);
    }

    /// <inheritdoc/>
    public void UpdateMotionTimestamp(Guid cameraId)
    {
        // No-op for server implementation (no post-motion timer needed)
    }

    /// <inheritdoc/>
    public string GenerateRecordingFilename(
        CameraConfiguration camera,
        string format)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var safeName = string.IsNullOrWhiteSpace(camera.Display.DisplayName)
            ? camera.Id.ToString("N")[..8]
            : string.Join("_", camera.Display.DisplayName.Split(Path.GetInvalidFileNameChars()));

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
        var ext = string.IsNullOrEmpty(format) ? "mp4" : format.TrimStart('.');

        return UniqueFilename.EnsureUnique(Path.Combine(
            settingsService.Recording.RecordingPath,
            safeName,
            $"{safeName}_{timestamp}.{ext}"));
    }

    /// <inheritdoc/>
    public void StopAllRecordings()
    {
        foreach (var cameraId in sessions.Keys.ToList())
        {
            StopRecording(cameraId);
        }
    }

    /// <inheritdoc/>
    public bool SegmentRecording(Guid cameraId)
    {
        if (!sessions.TryGetValue(cameraId, out var session))
        {
            return false;
        }

        if (!pipelines.TryGetValue(cameraId, out var pipeline))
        {
            LogSegmentNoPipeline(cameraId);
            return false;
        }

        var camera = cameraStorageService.GetCameraById(cameraId);
        if (camera is null)
        {
            LogSegmentCameraNotFound(cameraId);
            return false;
        }

        var oldFilePath = session.CurrentFilePath;
        var newFilePath = GenerateRecordingFilename(camera, settingsService.Recording.RecordingFormat);

        var directory = Path.GetDirectoryName(newFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            // Atomic switch: packets arriving from the demux thread mid-switch
            // land in either the previous segment or the new one — never the
            // close/open gap. Falls back to Stop+Start if the underlying
            // pipeline doesn't support atomic switching.
            pipeline.SwitchRecording(newFilePath);
        }
        catch (Exception ex)
        {
            LogSegmentFailed(ex, cameraId);
            return false;
        }

        var newSession = new RecordingSession(cameraId, newFilePath, session.IsManualRecording)
        {
            LastMotionTime = session.LastMotionTime,
            State = session.State,
        };

        sessions[cameraId] = newSession;

        RaiseStateChanged(cameraId, session.State, RecordingState.Idle, oldFilePath);
        RaiseStateChanged(cameraId, RecordingState.Idle, newSession.State, newFilePath);

        LogSegmented(cameraId, newFilePath);
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<RecordingSession> GetActiveSessions()
        => sessions.Values.ToList().AsReadOnly();

    /// <inheritdoc/>
    public void Dispose()
    {
        StopAllRecordings();
    }

    private void RaiseStateChanged(
        Guid cameraId,
        RecordingState oldState,
        RecordingState newState,
        string? filePath)
    {
        RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs(
            cameraId,
            oldState,
            newState,
            filePath));
    }
}