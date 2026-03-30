namespace Linksoft.CameraWall.Wpf.Services;

public partial class ThumbnailGeneratorService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting thumbnail capture for camera {CameraId}, output: {ThumbnailPath}, tiles: {TileCount}")]
    private partial void LogStartingThumbnailCapture(Guid cameraId, string thumbnailPath, int tileCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Snapshot capture returned no data for camera {CameraId}")]
    private partial void LogSnapshotCaptureNoData(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Captured frame {FrameIndex} for camera {CameraId}")]
    private partial void LogCapturedFrame(int frameIndex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to capture frame for camera {CameraId}")]
    private partial void LogFrameCaptureFailed(Exception ex, Guid cameraId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No frames captured for camera {CameraId}, skipping thumbnail generation")]
    private partial void LogNoFramesCaptured(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated thumbnail for camera {CameraId} with {FrameCount}/{TotalFrames} frames ({TileCount} tiles): {ThumbnailPath}")]
    private partial void LogGeneratedThumbnail(Guid cameraId, int frameCount, int totalFrames, int tileCount, string thumbnailPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to generate thumbnail for camera {CameraId}")]
    private partial void LogThumbnailGenerationFailed(Exception ex, Guid cameraId);
}