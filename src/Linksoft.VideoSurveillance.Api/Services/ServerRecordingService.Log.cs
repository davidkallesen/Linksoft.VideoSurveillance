namespace Linksoft.VideoSurveillance.Api.Services;

public sealed partial class ServerRecordingService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recording started for camera {CameraId}: {FilePath}")]
    private partial void LogRecordingStarted(Guid cameraId, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording stopped for camera {CameraId}")]
    private partial void LogRecordingStopped(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmented for camera {CameraId}: {FilePath}")]
    private partial void LogSegmented(Guid cameraId, string filePath);

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reaping inactive recording session for camera {CameraId} (pipeline died)")]
    private partial void LogReapingInactiveSession(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reaper swept {ReapedCount} inactive recording session(s)")]
    private partial void LogReaperSwept(int reapedCount);
}