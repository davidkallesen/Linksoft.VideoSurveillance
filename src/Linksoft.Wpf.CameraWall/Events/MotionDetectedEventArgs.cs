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
    /// <param name="isMotionActive">Whether motion is currently active (above threshold).</param>
    /// <param name="boundingBox">The bounding box of the detected motion in analysis coordinates.</param>
    /// <param name="analysisWidth">The analysis resolution width.</param>
    /// <param name="analysisHeight">The analysis resolution height.</param>
    public MotionDetectedEventArgs(
        Guid cameraId,
        double changePercentage,
        bool isMotionActive,
        Rect? boundingBox = null,
        int analysisWidth = 320,
        int analysisHeight = 240)
    {
        CameraId = cameraId;
        ChangePercentage = changePercentage;
        IsMotionActive = isMotionActive;
        BoundingBox = boundingBox;
        AnalysisWidth = analysisWidth;
        AnalysisHeight = analysisHeight;
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
    /// Gets a value indicating whether motion is currently active (above threshold).
    /// </summary>
    public bool IsMotionActive { get; }

    /// <summary>
    /// Gets the bounding box of the detected motion in analysis coordinates.
    /// Null if no distinct motion region was identified.
    /// </summary>
    public Rect? BoundingBox { get; }

    /// <summary>
    /// Gets the analysis resolution width used for the bounding box coordinates.
    /// </summary>
    public int AnalysisWidth { get; }

    /// <summary>
    /// Gets the analysis resolution height used for the bounding box coordinates.
    /// </summary>
    public int AnalysisHeight { get; }

    /// <summary>
    /// Gets the timestamp when motion was detected.
    /// </summary>
    public DateTime Timestamp { get; }
}