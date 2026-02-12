namespace Linksoft.VideoSurveillance.Api.Hubs;

/// <summary>
/// SignalR hub for real-time surveillance events.
/// Broadcasts camera connection changes, recording state, and motion detection events.
/// Provides client-to-server methods for camera control.
/// </summary>
/// <remarks>
/// Server-to-client events (broadcast by <see cref="SurveillanceEventBroadcaster"/>):
/// - ConnectionStateChanged { CameraId, NewState, Timestamp }
/// - MotionDetected { CameraId, IsMotionActive, BoundingBoxes[], AnalysisWidth, AnalysisHeight }
/// - RecordingStateChanged { CameraId, NewState, OldState, FilePath }
/// </remarks>
public sealed class SurveillanceHub : Hub
{
    private readonly ICameraStorageService storage;
    private readonly IRecordingService recordingService;
    private readonly IMediaPipelineFactory pipelineFactory;
    private readonly StreamingService streamingService;
    private readonly ILogger<SurveillanceHub> logger;

    public SurveillanceHub(
        ICameraStorageService storage,
        IRecordingService recordingService,
        IMediaPipelineFactory pipelineFactory,
        StreamingService streamingService,
        ILogger<SurveillanceHub> logger)
    {
        this.storage = storage;
        this.recordingService = recordingService;
        this.pipelineFactory = pipelineFactory;
        this.streamingService = streamingService;
        this.logger = logger;
    }

    /// <summary>
    /// Called when a new client connects to the hub.
    /// </summary>
    public override Task OnConnectedAsync()
    {
        logger.LogInformation(
            "Client connected: {ConnectionId}",
            Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "Client disconnected: {ConnectionId}",
            Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Starts recording for a camera.
    /// </summary>
    public async Task StartRecording(Guid cameraId)
    {
        var camera = storage.GetCameraById(cameraId);
        if (camera is null)
        {
            await Clients.Caller
                .SendAsync("Error", $"Camera {cameraId} not found.")
                .ConfigureAwait(false);
            return;
        }

        if (recordingService.IsRecording(cameraId))
        {
            await Clients.Caller
                .SendAsync("Error", $"Camera {cameraId} is already recording.")
                .ConfigureAwait(false);
            return;
        }

        var pipeline = pipelineFactory.Create(camera);
        var started = recordingService.StartRecording(camera, pipeline);

        logger.LogInformation(
            "Recording {Result} for camera {CameraId} via SignalR",
            started ? "started" : "failed to start",
            cameraId);
    }

    /// <summary>
    /// Stops recording for a camera.
    /// </summary>
    public Task StopRecording(Guid cameraId)
    {
        recordingService.StopRecording(cameraId);

        logger.LogInformation(
            "Recording stopped for camera {CameraId} via SignalR",
            cameraId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Swaps two adjacent cameras in the current layout.
    /// </summary>
    public Task SwapCameras(
        Guid cameraId,
        SwapDirection direction)
        => Clients.All.SendAsync(
            "CameraSwapRequested",
            new { CameraId = cameraId, Direction = direction.ToString() });

    /// <summary>
    /// Starts HLS streaming for a camera. Returns the playlist URL.
    /// </summary>
    public async Task StartStream(Guid cameraId)
    {
        try
        {
            var playlistPath = streamingService.StartStream(cameraId);

            // Wait for FFmpeg to create the playlist file (up to 30 seconds)
            var timeout = TimeSpan.FromSeconds(30);
            var start = DateTime.UtcNow;
            while (!File.Exists(playlistPath))
            {
                if (streamingService.HasProcessExited(cameraId))
                {
                    var errors = streamingService.GetProcessError(cameraId);
                    streamingService.StopStream(cameraId);
                    throw new InvalidOperationException(
                        $"FFmpeg exited unexpectedly. Output: {errors}");
                }

                if (DateTime.UtcNow - start > timeout)
                {
                    var errors = streamingService.GetProcessError(cameraId);
                    streamingService.StopStream(cameraId);
                    throw new TimeoutException(
                        $"FFmpeg did not produce the HLS playlist in time. Last output: {errors}");
                }

                await Task.Delay(500).ConfigureAwait(false);
            }

            var relativePath = Path.GetRelativePath(
                streamingService.HlsOutputRoot,
                playlistPath).Replace('\\', '/');

            await Clients.Caller
                .SendAsync("StreamStarted", new { CameraId = cameraId, PlaylistUrl = $"/streams/{relativePath}" })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to start stream for camera {CameraId}", cameraId);
            throw new HubException($"Failed to start stream: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops HLS streaming for a camera.
    /// </summary>
    public Task StopStream(Guid cameraId)
    {
        streamingService.StopStream(cameraId);
        return Task.CompletedTask;
    }
}