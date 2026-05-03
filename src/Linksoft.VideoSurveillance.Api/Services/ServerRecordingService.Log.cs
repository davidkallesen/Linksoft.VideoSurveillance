namespace Linksoft.VideoSurveillance.Api.Services;

public sealed partial class ServerRecordingService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recording started for camera '{CameraName}' ({CameraId}): {FilePath}")]
    private partial void LogRecordingStarted(string cameraName, Guid cameraId, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording stopped for camera '{CameraName}' ({CameraId})")]
    private partial void LogRecordingStopped(string cameraName, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmented for camera '{CameraName}' ({CameraId}): {FilePath}")]
    private partial void LogSegmented(string cameraName, Guid cameraId, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot segment camera {CameraId}: pipeline reference missing")]
    private partial void LogSegmentNoPipeline(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot segment camera {CameraId}: camera not found in storage")]
    private partial void LogSegmentCameraNotFound(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Segmentation failed for camera {CameraId}")]
    private partial void LogSegmentFailed(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stopping pipeline for camera {CameraId} threw")]
    private partial void LogStopPipelineFailed(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Disposing pipeline for camera {CameraId} threw")]
    private partial void LogDisposePipelineFailed(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reaping inactive recording session for camera '{CameraName}' ({CameraId}): {Reason}")]
    private partial void LogReapingInactiveSession(string cameraName, Guid cameraId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reaper swept {ReapedCount} inactive recording session(s)")]
    private partial void LogReaperSwept(int reapedCount);
}