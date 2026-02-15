namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Background service that proactively connects cameras and starts recording
/// when <see cref="RecordingSettings.EnableRecordingOnConnect"/> is enabled
/// (respecting per-camera overrides).
/// </summary>
public sealed class CameraConnectionManager : BackgroundServiceBase<CameraConnectionManager>
{
    private readonly ICameraStorageService storageService;
    private readonly IApplicationSettingsService settingsService;
    private readonly IRecordingService recordingService;
    private readonly IMediaPipelineFactory pipelineFactory;
    private readonly ConcurrentDictionary<Guid, IMediaPipeline> managedPipelines = new();

    public CameraConnectionManager(
        ILogger<CameraConnectionManager> logger,
        IBackgroundServiceOptions backgroundServiceOptions,
        ICameraStorageService storageService,
        IApplicationSettingsService settingsService,
        IRecordingService recordingService,
        IMediaPipelineFactory pipelineFactory)
        : base(logger, backgroundServiceOptions)
    {
        this.storageService = storageService;
        this.settingsService = settingsService;
        this.recordingService = recordingService;
        this.pipelineFactory = pipelineFactory;
    }

    /// <inheritdoc />
    public override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        var cameras = storageService.GetAllCameras();
        var appDefault = settingsService.Recording.EnableRecordingOnConnect;

        foreach (var camera in cameras)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (!RecordingPolicyHelper.ShouldRecordOnConnect(camera, appDefault))
                {
                    continue;
                }

                // Detect dead pipelines: session exists but FFmpeg process has exited
                if (recordingService.IsRecording(camera.Id) &&
                    managedPipelines.TryGetValue(camera.Id, out var existingPipeline) &&
                    !existingPipeline.IsRecordingActive)
                {
                    logger.LogWarning(
                        "Dead recording pipeline detected for camera {CameraId} ({DisplayName}), cleaning up",
                        camera.Id,
                        camera.Display.DisplayName);

                    recordingService.StopRecording(camera.Id);
                    RemoveAndDisposePipeline(camera.Id);
                }

                if (recordingService.IsRecording(camera.Id))
                {
                    continue;
                }

                // Pipeline already created — try to start recording if player is now playing
                if (managedPipelines.TryGetValue(camera.Id, out var pending))
                {
                    TryStartRecordingForPipeline(camera, pending);
                    continue;
                }

                logger.LogInformation(
                    "Starting recording-on-connect for camera {CameraId} ({DisplayName})",
                    camera.Id,
                    camera.Display.DisplayName);

                var pipeline = pipelineFactory.Create(camera);
                managedPipelines[camera.Id] = pipeline;

                // Open() is non-blocking — start recording when the player reaches Playing state
                var capturedCamera = camera;
                pipeline.ConnectionStateChanged += (_, e) =>
                    OnManagedPipelineConnected(capturedCamera, e);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to start recording for camera {CameraId} ({DisplayName}), will retry next iteration",
                    camera.Id,
                    camera.Display.DisplayName);

                RemoveAndDisposePipeline(camera.Id);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gracefully stops all managed recordings before the service shuts down.
    /// This runs during the hosted-service shutdown phase which has the full
    /// shutdown timeout (30 s by default), unlike Dispose which runs during
    /// DI container teardown when time may be limited.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);

        StopAllManagedRecordings();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        // Pipelines should already be stopped by StopAsync; dispose cleans up remaining resources
        foreach (var kvp in managedPipelines)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error disposing pipeline for camera {CameraId}", kvp.Key);
            }
        }

        managedPipelines.Clear();
        base.Dispose();
    }

    private void StopAllManagedRecordings()
    {
        foreach (var cameraId in managedPipelines.Keys.ToList())
        {
            try
            {
                logger.LogInformation("Stopping managed recording for camera {CameraId}", cameraId);
                recordingService.StopRecording(cameraId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error stopping recording for camera {CameraId} during shutdown", cameraId);
            }
        }
    }

    private void TryStartRecordingForPipeline(
        CameraConfiguration camera,
        IMediaPipeline pipeline)
    {
        // FramesDecoded > 0 means the player is playing and decoding — safe to start recording
        if (pipeline.FramesDecoded <= 0)
        {
            return;
        }

        try
        {
            recordingService.StartRecording(camera, pipeline);
            logger.LogInformation(
                "Recording-on-connect started (fallback) for camera {CameraId} ({DisplayName})",
                camera.Id,
                camera.Display.DisplayName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to start recording (fallback) for camera {CameraId} ({DisplayName})",
                camera.Id,
                camera.Display.DisplayName);
        }
    }

    /// <summary>
    /// Handles pipeline state changes. Runs on the demux thread — must NOT
    /// call <see cref="RemoveAndDisposePipeline"/> synchronously because
    /// Dispose → Join would deadlock (self-join on the demux thread).
    /// </summary>
    private void OnManagedPipelineConnected(
        CameraConfiguration camera,
        ConnectionStateChangedEventArgs e)
    {
        if (e.NewState == ConnectionState.Connected)
        {
            if (recordingService.IsRecording(camera.Id) ||
                !managedPipelines.TryGetValue(camera.Id, out var pipeline))
            {
                return;
            }

            try
            {
                recordingService.StartRecording(camera, pipeline);
                logger.LogInformation(
                    "Recording-on-connect started for camera {CameraId} ({DisplayName})",
                    camera.Id,
                    camera.Display.DisplayName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to start recording after connect for camera {CameraId} ({DisplayName})",
                    camera.Id,
                    camera.Display.DisplayName);

                // Defer disposal — this handler runs on the demux thread,
                // and Dispose joins that thread (self-join deadlock).
                ScheduleDeferredDisposal(camera.Id);
            }
        }
        else if (e.NewState == ConnectionState.Error)
        {
            logger.LogWarning(
                "Pipeline connection failed for camera {CameraId} ({DisplayName}), cleaning up",
                camera.Id,
                camera.Display.DisplayName);

            ScheduleDeferredDisposal(camera.Id);
        }
    }

    private void RemoveAndDisposePipeline(Guid cameraId)
    {
        if (managedPipelines.TryRemove(cameraId, out var pipeline))
        {
            try
            {
                pipeline.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error disposing dead pipeline for camera {CameraId}", cameraId);
            }
        }
    }

    /// <summary>
    /// Removes the pipeline from the managed dictionary and disposes it on a
    /// ThreadPool thread. This prevents self-join deadlocks when called from
    /// the pipeline's demux thread (via the ConnectionStateChanged event).
    /// </summary>
#pragma warning disable CA2000 // Pipeline is disposed asynchronously on the ThreadPool
    private void ScheduleDeferredDisposal(Guid cameraId)
    {
        if (managedPipelines.TryRemove(cameraId, out var pipeline))
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    pipeline.Dispose();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error disposing pipeline for camera {CameraId} (deferred)", cameraId);
                }
            });
        }
    }
#pragma warning restore CA2000
}