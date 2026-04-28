namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Background service that subscribes to Core service events and broadcasts them
/// to connected SignalR clients via the <see cref="SurveillanceHub"/>.
/// </summary>
public sealed partial class SurveillanceEventBroadcaster : IHostedService
{
    // Bound the time any single broadcast can spend in SendAsync so a
    // slow/disconnected client cannot accumulate unbounded in-flight tasks
    // while events keep firing from the recording / motion services.
    private static readonly TimeSpan BroadcastTimeout = TimeSpan.FromSeconds(5);

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

        LogBroadcasterStarted();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        recordingService.RecordingStateChanged -= OnRecordingStateChanged;
        motionDetectionService.MotionDetected -= OnMotionDetected;

        LogBroadcasterStopped();

        return Task.CompletedTask;
    }

    private async void OnRecordingStateChanged(
        object? sender,
        RecordingStateChangedEventArgs e)
    {
        using var cts = new CancellationTokenSource(BroadcastTimeout);
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
                    },
                    cts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogBroadcastRecordingFailed(ex, e.CameraId);
        }
    }

    private async void OnMotionDetected(
        object? sender,
        MotionDetectedEventArgs e)
    {
        using var cts = new CancellationTokenSource(BroadcastTimeout);
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
                    },
                    cts.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogBroadcastMotionFailed(ex, e.CameraId);
        }
    }
}