namespace Linksoft.VideoSurveillance.Api.Domain.ApiHandlers.Cameras;

/// <summary>
/// Handler business logic for the StartRecording operation.
/// Requires an active IMediaPipeline and IRecordingService.
/// </summary>
public sealed class StartRecordingHandler(
    ICameraStorageService storage,
    IRecordingService recordingService,
    IMediaPipelineFactory pipelineFactory) : IStartRecordingHandler
{
    // Wait at most this long for the camera to reach the Connected state
    // before reporting failure. Longer than the typical RTSP open (1-3 s)
    // but short enough that a stuck client gets a definite answer.
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);

    public async Task<StartRecordingResult> ExecuteAsync(
        StartRecordingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var camera = storage.GetCameraById(parameters.CameraId);
        if (camera is null)
        {
            return StartRecordingResult.NotFound($"Camera {parameters.CameraId} not found.");
        }

        if (recordingService.IsRecording(parameters.CameraId))
        {
            return StartRecordingResult.Conflict($"Camera {parameters.CameraId} is already recording.");
        }

        var pipeline = pipelineFactory.Create(camera);
        try
        {
            // pipelineFactory.Create returns immediately; the actual RTSP
            // open happens on the engine thread. Wait for Connected (or a
            // hard failure) before reporting success so the client sees a
            // definite outcome instead of a fire-and-forget "ok".
            var connected = await WaitForConnectedAsync(pipeline, ConnectionTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (!connected)
            {
                pipeline.Dispose();
                return StartRecordingResult.Conflict(
                    $"Camera {parameters.CameraId} did not reach Connected within {ConnectionTimeout.TotalSeconds:N0}s.");
            }

            recordingService.StartRecording(camera, pipeline);
        }
        catch
        {
            pipeline.Dispose();
            throw;
        }

        var session = recordingService.GetSession(parameters.CameraId);
        var state = recordingService.GetRecordingState(parameters.CameraId);

        var status = new RecordingStatus(
            CameraId: parameters.CameraId,
            State: state.ToApiRecordingState(),
            FilePath: session?.CurrentFilePath ?? string.Empty,
            StartedAt: session is not null ? new DateTimeOffset(session.StartTime, TimeSpan.Zero) : DateTimeOffset.UtcNow);

        return StartRecordingResult.Ok(status);
    }

    private static async Task<bool> WaitForConnectedAsync(
        IMediaPipeline pipeline,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<Linksoft.VideoSurveillance.Events.ConnectionStateChangedEventArgs> handler = (
            object? sender,
            Linksoft.VideoSurveillance.Events.ConnectionStateChangedEventArgs e) =>
        {
            if (e.NewState == Linksoft.VideoSurveillance.Enums.ConnectionState.Connected)
            {
                tcs.TrySetResult(true);
            }
            else if (e.NewState == Linksoft.VideoSurveillance.Enums.ConnectionState.Error)
            {
                tcs.TrySetResult(false);
            }
        };

        pipeline.ConnectionStateChanged += handler;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            await using var reg = cts.Token.Register(() => tcs.TrySetResult(false));

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            pipeline.ConnectionStateChanged -= handler;
        }
    }
}