namespace Linksoft.VideoSurveillance.Services;

/// <summary>
/// Service for managing camera recording sessions.
/// </summary>
public interface IRecordingService
{
    event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;

    RecordingState GetRecordingState(Guid cameraId);

    RecordingSession? GetSession(Guid cameraId);

    bool StartRecording(
        CameraConfiguration camera,
        IMediaPipeline pipeline);

    void StopRecording(Guid cameraId);

    bool IsRecording(Guid cameraId);

    bool TriggerMotionRecording(
        CameraConfiguration camera,
        IMediaPipeline pipeline);

    void UpdateMotionTimestamp(Guid cameraId);

    string GenerateRecordingFilename(
        CameraConfiguration camera,
        string format);

    void StopAllRecordings();

    bool SegmentRecording(Guid cameraId);

    IReadOnlyList<RecordingSession> GetActiveSessions();
}