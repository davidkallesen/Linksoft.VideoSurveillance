namespace Linksoft.VideoSurveillance.Wpf.Core.UserControls;

/// <summary>
/// Source-generated high-performance log methods for <see cref="CameraTile"/>.
/// </summary>
public partial class CameraTile
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recording-on-connect starting for camera '{CameraName}'")]
    private partial void LogRecordingOnConnectStarting(string cameraName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Recording-on-connect enabled but skipped for camera '{CameraName}' (pipeline={HasPipeline}, service={HasService}, state={RecordingState})")]
    private partial void LogRecordingOnConnectSkipped(string cameraName, bool hasPipeline, bool hasService, RecordingState recordingState);

    [LoggerMessage(Level = LogLevel.Information, Message = "Auto-reconnect scheduled for camera '{CameraName}' in {DelaySeconds}s")]
    private partial void LogAutoReconnectScheduled(string cameraName, int delaySeconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Auto-reconnect attempting for camera '{CameraName}'")]
    private partial void LogAutoReconnectAttempting(string cameraName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stream stale detected for camera '{CameraName}' (no frames for 15s), triggering reconnect")]
    private partial void LogStreamStaleDetected(string cameraName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection timeout for camera '{CameraName}' after {Seconds}s")]
    private partial void LogConnectionTimeout(string cameraName, int seconds);
}