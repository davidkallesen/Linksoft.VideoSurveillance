namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for automatically segmenting recordings at clock-aligned interval boundaries
/// (e.g., every 15 minutes at :00, :15, :30, :45).
/// </summary>
public interface IRecordingSegmentationService
{
    /// <summary>
    /// Occurs when a recording is segmented.
    /// </summary>
    event EventHandler<RecordingSegmentedEventArgs>? RecordingSegmented;

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Initializes and starts the segmentation service.
    /// This should be called during application startup.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Stops the segmentation service.
    /// This should be called during application shutdown.
    /// </summary>
    void StopService();
}