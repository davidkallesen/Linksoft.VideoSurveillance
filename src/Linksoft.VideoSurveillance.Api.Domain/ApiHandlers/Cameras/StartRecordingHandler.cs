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
    public Task<StartRecordingResult> ExecuteAsync(
        StartRecordingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var camera = storage.GetCameraById(parameters.CameraId);
        if (camera is null)
        {
            return Task.FromResult(StartRecordingResult.NotFound($"Camera {parameters.CameraId} not found."));
        }

        if (recordingService.IsRecording(parameters.CameraId))
        {
            return Task.FromResult(StartRecordingResult.Conflict($"Camera {parameters.CameraId} is already recording."));
        }

        var pipeline = pipelineFactory.Create(camera);
        recordingService.StartRecording(camera, pipeline);

        var session = recordingService.GetSession(parameters.CameraId);
        var state = recordingService.GetRecordingState(parameters.CameraId);

        var status = new RecordingStatus(
            CameraId: parameters.CameraId,
            State: state.ToApiRecordingState(),
            FilePath: session?.CurrentFilePath ?? string.Empty,
            StartedAt: session is not null ? new DateTimeOffset(session.StartTime, TimeSpan.Zero) : DateTimeOffset.UtcNow);

        return Task.FromResult(StartRecordingResult.Ok(status));
    }
}