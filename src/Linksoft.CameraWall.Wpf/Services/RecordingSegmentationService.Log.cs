namespace Linksoft.CameraWall.Wpf.Services;

public partial class RecordingSegmentationService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmentation service is disabled")]
    private partial void LogSegmentationDisabled();

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmentation service initializing - Interval: {Interval} minutes")]
    private partial void LogSegmentationInitializing(int interval);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmentation service started")]
    private partial void LogSegmentationStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Recording segmentation service stopped")]
    private partial void LogSegmentationStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Segmentation check timer started with interval: {Interval}")]
    private partial void LogSegmentationTimerStarted(TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Segmentation check timer stopped")]
    private partial void LogSegmentationTimerStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Interval boundary detected for camera '{CameraName}', slot: {CurrentSlot} (interval: {Interval} min)")]
    private partial void LogIntervalBoundaryDetected(string cameraName, int currentSlot, int interval);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Max duration reached for camera '{CameraName}', duration: {Duration}")]
    private partial void LogMaxDurationReached(string cameraName, TimeSpan duration);

    [LoggerMessage(Level = LogLevel.Information, Message = "Segmenting recording for camera '{CameraName}', reason: {Reason}")]
    private partial void LogSegmentingRecording(string cameraName, SegmentationReason reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to segment recording for camera '{CameraName}'")]
    private partial void LogSegmentingFailed(string cameraName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Segmentation tick failed; will retry on next interval")]
    private partial void LogSegmentationTickFailed(Exception ex);
}