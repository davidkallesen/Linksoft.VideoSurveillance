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
    /// <param name="boundingBoxes">The bounding boxes of detected motion regions in analysis coordinates.</param>
    /// <param name="analysisWidth">The analysis resolution width.</param>
    /// <param name="analysisHeight">The analysis resolution height.</param>
    public MotionDetectedEventArgs(
        Guid cameraId,
        double changePercentage,
        bool isMotionActive,
        IReadOnlyList<Rect>? boundingBoxes = null,
        int analysisWidth = 320,
        int analysisHeight = 240)
    {
        CameraId = cameraId;
        ChangePercentage = changePercentage;
        IsMotionActive = isMotionActive;
        BoundingBoxes = boundingBoxes ?? [];
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
    /// Gets the bounding boxes of detected motion regions in analysis coordinates.
    /// Empty if no distinct motion regions were identified.
    /// Multiple boxes indicate multiple separate moving objects.
    /// </summary>
    public IReadOnlyList<Rect> BoundingBoxes { get; }

    /// <summary>
    /// Gets a value indicating whether any bounding boxes were detected.
    /// </summary>
    public bool HasBoundingBoxes => BoundingBoxes.Count > 0;

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