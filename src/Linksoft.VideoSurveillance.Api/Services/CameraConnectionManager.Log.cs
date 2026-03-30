namespace Linksoft.VideoSurveillance.Api.Services;

public sealed partial class CameraConnectionManager
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Camera {CameraId} ({DisplayName}) - recording-on-connect disabled, skipping")]
    private partial void LogRecordingOnConnectDisabled(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dead recording pipeline detected for camera {CameraId} ({DisplayName}), cleaning up")]
    private partial void LogDeadPipelineDetected(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting recording-on-connect for camera {CameraId} ({DisplayName})")]
    private partial void LogStartingRecordingOnConnect(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to start recording for camera {CameraId} ({DisplayName}), will retry next iteration")]
    private partial void LogStartRecordingFailed(Exception ex, Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error disposing pipeline for camera {CameraId}")]
    private partial void LogDisposePipelineError(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping managed recording for camera {CameraId}")]
    private partial void LogStoppingManagedRecording(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error stopping recording for camera {CameraId} during shutdown")]
    private partial void LogStopRecordingShutdownError(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording-on-connect started (fallback) for camera {CameraId} ({DisplayName})")]
    private partial void LogRecordingOnConnectStartedFallback(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to start recording (fallback) for camera {CameraId} ({DisplayName})")]
    private partial void LogStartRecordingFallbackFailed(Exception ex, Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Camera {CameraId} ({DisplayName}) connected")]
    private partial void LogCameraConnected(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording-on-connect started for camera {CameraId} ({DisplayName})")]
    private partial void LogRecordingOnConnectStarted(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to start recording after connect for camera {CameraId} ({DisplayName})")]
    private partial void LogStartRecordingAfterConnectFailed(Exception ex, Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Camera {CameraId} ({DisplayName}) disconnected")]
    private partial void LogCameraDisconnected(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pipeline connection failed for camera {CameraId} ({DisplayName}), cleaning up")]
    private partial void LogPipelineConnectionFailed(Guid cameraId, string displayName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error disposing dead pipeline for camera {CameraId}")]
    private partial void LogDisposeDeadPipelineError(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error disposing pipeline for camera {CameraId} (deferred)")]
    private partial void LogDisposePipelineDeferredError(Exception ex, Guid cameraId);
}