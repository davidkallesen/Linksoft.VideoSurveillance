namespace Linksoft.VideoSurveillance.Api.Services;

public sealed partial class ServerRecordingService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recording started for camera {CameraId}: {FilePath}")]
    private partial void LogRecordingStarted(Guid cameraId, string filePath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording stopped for camera {CameraId}")]
    private partial void LogRecordingStopped(Guid cameraId);
}