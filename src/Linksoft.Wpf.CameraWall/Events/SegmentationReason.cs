namespace Linksoft.Wpf.CameraWall.Events;

/// <summary>
/// Reasons for recording segmentation.
/// </summary>
public enum SegmentationReason
{
    /// <summary>
    /// Segmented at a clock-aligned interval boundary (e.g., every 15 minutes at :00, :15, :30, :45).
    /// </summary>
    IntervalBoundary,

    /// <summary>
    /// Segmented because the maximum duration was reached.
    /// </summary>
    MaxDurationReached,
}