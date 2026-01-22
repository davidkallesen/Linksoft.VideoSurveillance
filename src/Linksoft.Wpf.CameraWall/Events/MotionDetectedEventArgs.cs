namespace Linksoft.Wpf.CameraWall.Events;

/// <summary>
/// Event arguments for motion detection events.
/// </summary>
public class MotionDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MotionDetectedEventArgs"/> class.
    /// </summary>
    /// <param name="cameraId">The ID of the camera where motion was detected.</param>
    /// <param name="changePercentage">The percentage of pixels that changed.</param>
    public MotionDetectedEventArgs(
        Guid cameraId,
        double changePercentage)
    {
        CameraId = cameraId;
        ChangePercentage = changePercentage;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the camera where motion was detected.
    /// </summary>
    public Guid CameraId { get; }

    /// <summary>
    /// Gets the percentage of pixels that changed (0-100).
    /// </summary>
    public double ChangePercentage { get; }

    /// <summary>
    /// Gets the timestamp when motion was detected.
    /// </summary>
    public DateTime Timestamp { get; }
}