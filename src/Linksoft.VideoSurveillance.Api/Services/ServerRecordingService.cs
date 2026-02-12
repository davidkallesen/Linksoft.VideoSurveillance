namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side implementation of <see cref="IRecordingService"/> using FFmpeg media pipelines.
/// </summary>
public sealed class ServerRecordingService : IRecordingService, IDisposable
{
    private readonly IApplicationSettingsService settingsService;
    private readonly ILogger<ServerRecordingService> logger;
    private readonly ConcurrentDictionary<Guid, RecordingSession> sessions = new();
    private readonly ConcurrentDictionary<Guid, IMediaPipeline> pipelines = new();

    public ServerRecordingService(
        IApplicationSettingsService settingsService,
        ILogger<ServerRecordingService> logger)
    {
        this.settingsService = settingsService;
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
        logger.LogInformation("Recording started for camera {CameraId}: {FilePath}", camera.Id, filePath);

        return true;
    }

    /// <inheritdoc/>
    public void StopRecording(Guid cameraId)
    {
        if (!sessions.TryRemove(cameraId, out var session))
        {
            return;
        }
#pragma warning disable CA2000 // Pipeline ownership belongs to caller, not this service
        if (pipelines.TryRemove(cameraId, out var pipeline))
        {
            pipeline.StopRecording();
        }
#pragma warning restore CA2000

        RaiseStateChanged(cameraId, RecordingState.Recording, RecordingState.Idle, session.CurrentFilePath);
        logger.LogInformation("Recording stopped for camera {CameraId}", cameraId);
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

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var ext = string.IsNullOrEmpty(format) ? "mp4" : format.TrimStart('.');

        return Path.Combine(
            settingsService.Recording.RecordingPath,
            safeName,
            $"{safeName}_{timestamp}.{ext}");
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
        => false;

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