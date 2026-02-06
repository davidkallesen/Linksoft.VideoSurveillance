namespace Linksoft.Wpf.CameraWall.Events;

/// <summary>
/// Event arguments for timelapse frame capture events.
/// </summary>
public class TimelapseFrameCapturedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimelapseFrameCapturedEventArgs"/> class.
    /// </summary>
    /// <param name="cameraId">The ID of the camera that captured the frame.</param>
    /// <param name="filePath">The file path where the frame was saved.</param>
    /// <param name="capturedAt">The timestamp when the frame was captured.</param>
    public TimelapseFrameCapturedEventArgs(
        Guid cameraId,
        string filePath,
        DateTime capturedAt)
    {
        CameraId = cameraId;
        FilePath = filePath;
        CapturedAt = capturedAt;
    }

    /// <summary>
    /// Gets the ID of the camera that captured the frame.
    /// </summary>
    public Guid CameraId { get; }

    /// <summary>
    /// Gets the file path where the frame was saved.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the timestamp when the frame was captured.
    /// </summary>
    public DateTime CapturedAt { get; }
}