namespace Linksoft.CameraWall.Wpf.Services;

public partial class RecordingService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recording started for camera: '{CameraName}', file: {FilePath}")]
    private partial void LogRecordingStarted(string cameraName, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start recording for camera: {CameraName}")]
    private partial void LogRecordingStartFailed(Exception ex, string cameraName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording stopped for camera ID: {CameraId}, file: {FilePath}")]
    private partial void LogRecordingStopped(Guid cameraId, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error stopping recording for camera ID: {CameraId}")]
    private partial void LogRecordingStopError(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Motion recording started for camera: {CameraName}, file: {FilePath}")]
    private partial void LogMotionRecordingStarted(string cameraName, string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start motion recording for camera: {CameraName}")]
    private partial void LogMotionRecordingStartFailed(Exception ex, string cameraName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping all recordings ({Count} active sessions)")]
    private partial void LogStoppingAllRecordings(int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No active session for camera ID: {CameraId}, cannot segment")]
    private partial void LogNoActiveSessionForSegment(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No pipeline found for camera ID: {CameraId}, cannot segment")]
    private partial void LogNoPipelineForSegment(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Camera not found for ID: {CameraId}, cannot segment")]
    private partial void LogCameraNotFoundForSegment(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Segmenting recording for camera: {CameraName}, old file: {OldFilePath}")]
    private partial void LogSegmentingRecording(string cameraName, string oldFilePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to add new session for camera ID: {CameraId}")]
    private partial void LogFailedToAddNewSession(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmented for camera: {CameraName}, new file: {NewFilePath}")]
    private partial void LogRecordingSegmented(string cameraName, string newFilePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to segment recording for camera: {CameraName}")]
    private partial void LogSegmentRecordingFailed(Exception ex, string cameraName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Post-motion period elapsed for camera ID: {CameraId}, stopping recording")]
    private partial void LogPostMotionPeriodElapsed(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Transitioning to post-motion state for camera ID: {CameraId}, {Remaining}s remaining")]
    private partial void LogTransitioningToPostMotion(Guid cameraId, string remaining);
}