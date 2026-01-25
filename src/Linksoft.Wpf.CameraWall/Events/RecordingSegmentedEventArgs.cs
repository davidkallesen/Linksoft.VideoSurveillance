namespace Linksoft.Wpf.CameraWall.Events;

/// <summary>
/// Event arguments for recording segmentation events.
/// </summary>
public class RecordingSegmentedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingSegmentedEventArgs"/> class.
    /// </summary>
    /// <param name="cameraId">The ID of the camera whose recording was segmented.</param>
    /// <param name="previousFilePath">The file path of the previous segment.</param>
    /// <param name="newFilePath">The file path of the new segment.</param>
    /// <param name="reason">The reason for segmentation.</param>
    public RecordingSegmentedEventArgs(
        Guid cameraId,
        string previousFilePath,
        string newFilePath,
        SegmentationReason reason)
    {
        CameraId = cameraId;
        PreviousFilePath = previousFilePath ?? throw new ArgumentNullException(nameof(previousFilePath));
        NewFilePath = newFilePath ?? throw new ArgumentNullException(nameof(newFilePath));
        Reason = reason;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the camera whose recording was segmented.
    /// </summary>
    public Guid CameraId { get; }

    /// <summary>
    /// Gets the file path of the previous segment.
    /// </summary>
    public string PreviousFilePath { get; }

    /// <summary>
    /// Gets the file path of the new segment.
    /// </summary>
    public string NewFilePath { get; }

    /// <summary>
    /// Gets the reason for segmentation.
    /// </summary>
    public SegmentationReason Reason { get; }

    /// <summary>
    /// Gets the timestamp when the segmentation occurred.
    /// </summary>
    public DateTime Timestamp { get; }
}