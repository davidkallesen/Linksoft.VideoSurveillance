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

                logger.LogInformation(
                    "Starting recording-on-connect for camera {CameraId} ({DisplayName})",
                    camera.Id,
                    camera.Display.DisplayName);

                var pipeline = pipelineFactory.Create(camera);
                managedPipelines[camera.Id] = pipeline;
                recordingService.StartRecording(camera, pipeline);
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

#pragma warning disable CA2000 // Pipeline is disposed inside the method
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
#pragma warning restore CA2000
}