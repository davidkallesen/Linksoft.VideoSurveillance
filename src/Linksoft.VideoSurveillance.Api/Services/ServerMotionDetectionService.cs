namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side implementation of <see cref="IMotionDetectionService"/>.
/// Uses <see cref="IMediaPipeline.CaptureFrameAsync"/> for frame-based motion detection.
/// </summary>
public sealed class ServerMotionDetectionService : IMotionDetectionService, IDisposable
{
    private readonly ILogger<ServerMotionDetectionService> logger;
    private readonly ConcurrentDictionary<Guid, DetectionContext> contexts = new();

    public ServerMotionDetectionService(
        ILogger<ServerMotionDetectionService> logger)
        => this.logger = logger;

    /// <inheritdoc/>
#pragma warning disable CS0067 // Event is required by interface but not raised in stub implementation
    public event EventHandler<MotionDetectedEventArgs>? MotionDetected;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public void StartDetection(
        Guid cameraId,
        IMediaPipeline pipeline,
        MotionDetectionSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        if (contexts.ContainsKey(cameraId))
        {
            return;
        }

        CancellationTokenSource? cts = null;
        try
        {
            cts = new CancellationTokenSource();
            var context = new DetectionContext(pipeline, settings ?? new MotionDetectionSettings(), cts);

            if (contexts.TryAdd(cameraId, context))
            {
                _ = RunDetectionLoopAsync(cameraId, context);
                logger.LogInformation("Motion detection started for camera {CameraId}", cameraId);
                cts = null;
            }
        }
        finally
        {
            cts?.Dispose();
        }
    }

    /// <inheritdoc/>
    public void StopDetection(Guid cameraId)
    {
        if (contexts.TryRemove(cameraId, out var context))
        {
            context.Cts.Cancel();
            context.Cts.Dispose();
            logger.LogInformation("Motion detection stopped for camera {CameraId}", cameraId);
        }
    }

    /// <inheritdoc/>
    public bool IsDetectionActive(Guid cameraId)
        => contexts.ContainsKey(cameraId);

    /// <inheritdoc/>
    public bool IsMotionDetected(Guid cameraId)
        => contexts.TryGetValue(cameraId, out var ctx) && ctx.IsMotionActive;

    /// <inheritdoc/>
    public IReadOnlyList<BoundingBox> GetLastBoundingBoxes(Guid cameraId)
        => contexts.TryGetValue(cameraId, out var ctx)
            ? ctx.LastBoundingBoxes
            : [];

    /// <inheritdoc/>
    public (int Width, int Height) GetAnalysisResolution(Guid cameraId)
        => (320, 240);

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var cameraId in contexts.Keys.ToList())
        {
            StopDetection(cameraId);
        }
    }

    private async Task RunDetectionLoopAsync(
        Guid cameraId,
        DetectionContext context)
    {
        var intervalMs = Math.Max(200, 1000 / Math.Max(1, context.Settings.AnalysisFrameRate));

        try
        {
            while (!context.Cts.Token.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, context.Cts.Token).ConfigureAwait(false);

                var frame = await context.Pipeline
                    .CaptureFrameAsync(context.Cts.Token)
                    .ConfigureAwait(false);

                if (frame is null)
                {
                    continue;
                }

                // TODO: Implement frame differencing for actual motion detection
                // For now, discard the frame and leave motion inactive
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping detection
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Motion detection loop error for camera {CameraId}", cameraId);
        }
    }

    private sealed class DetectionContext(
        IMediaPipeline pipeline,
        MotionDetectionSettings settings,
        CancellationTokenSource cts)
    {
        public IMediaPipeline Pipeline { get; } = pipeline;

        public MotionDetectionSettings Settings { get; } = settings;

        public CancellationTokenSource Cts { get; } = cts;

#pragma warning disable S3459 // Properties assigned when motion detection logic is implemented
        public byte[]? PreviousFrame { get; set; }

        public bool IsMotionActive { get; set; }
#pragma warning restore S3459

        public IReadOnlyList<BoundingBox> LastBoundingBoxes { get; set; } = [];
    }
}