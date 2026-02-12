namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Background service that subscribes to Core service events and broadcasts them
/// to connected SignalR clients via the <see cref="SurveillanceHub"/>.
/// </summary>
public sealed class SurveillanceEventBroadcaster : IHostedService
{
    private readonly IHubContext<SurveillanceHub> hubContext;
    private readonly IRecordingService recordingService;
    private readonly IMotionDetectionService motionDetectionService;
    private readonly ILogger<SurveillanceEventBroadcaster> logger;

    public SurveillanceEventBroadcaster(
        IHubContext<SurveillanceHub> hubContext,
        IRecordingService recordingService,
        IMotionDetectionService motionDetectionService,
        ILogger<SurveillanceEventBroadcaster> logger)
    {
        this.hubContext = hubContext;
        this.recordingService = recordingService;
        this.motionDetectionService = motionDetectionService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recordingService.RecordingStateChanged += OnRecordingStateChanged;
        motionDetectionService.MotionDetected += OnMotionDetected;

        logger.LogInformation("Surveillance event broadcaster started");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        recordingService.RecordingStateChanged -= OnRecordingStateChanged;
        motionDetectionService.MotionDetected -= OnMotionDetected;

        logger.LogInformation("Surveillance event broadcaster stopped");

        return Task.CompletedTask;
    }

    private async void OnRecordingStateChanged(
        object? sender,
        RecordingStateChangedEventArgs e)
    {
        try
        {
            await hubContext.Clients.All
                .SendAsync(
                    "RecordingStateChanged",
                    new
                    {
                        e.CameraId,
                        NewState = e.NewState.ToString(),
                        OldState = e.OldState.ToString(),
                        e.FilePath,
                        e.Timestamp,
                    })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast RecordingStateChanged for camera {CameraId}", e.CameraId);
        }
    }

    private async void OnMotionDetected(
        object? sender,
        MotionDetectedEventArgs e)
    {
        try
        {
            await hubContext.Clients.All
                .SendAsync(
                    "MotionDetected",
                    new
                    {
                        e.CameraId,
                        e.IsMotionActive,
                        e.ChangePercentage,
                        BoundingBoxes = e.BoundingBoxes.Select(b => new
                        {
                            b.X,
                            b.Y,
                            b.Width,
                            b.Height,
                        }),
                        e.AnalysisWidth,
                        e.AnalysisHeight,
                        e.Timestamp,
                    })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast MotionDetected for camera {CameraId}", e.CameraId);
        }
    }
}