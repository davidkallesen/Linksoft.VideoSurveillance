namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Per-camera setting overrides that allow individual cameras to deviate from application-level defaults.
/// Organized into logical sub-sections matching the application settings structure.
/// </summary>
public class CameraOverrides
{
    /// <summary>
    /// Gets or sets connection-related overrides (timeout, reconnect, notifications).
    /// </summary>
    public ConnectionOverrides Connection { get; set; } = new();

    /// <summary>
    /// Gets or sets camera display overrides (overlay settings).
    /// </summary>
    public CameraDisplayOverrides CameraDisplay { get; set; } = new();

    /// <summary>
    /// Gets or sets performance overrides (video quality, hardware acceleration).
    /// </summary>
    public PerformanceOverrides Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets recording overrides (path, format, triggers, timelapse).
    /// </summary>
    public RecordingOverrides Recording { get; set; } = new();

    /// <summary>
    /// Gets or sets motion detection overrides (sensitivity, analysis, bounding box).
    /// </summary>
    public MotionDetectionOverrides MotionDetection { get; set; } = new();

    /// <summary>
    /// Determines whether any override is set (non-null).
    /// </summary>
    /// <returns>True if at least one override is set; otherwise, false.</returns>
    public bool HasAnyOverride()
        => Connection.HasAnyOverride() ||
           CameraDisplay.HasAnyOverride() ||
           Performance.HasAnyOverride() ||
           Recording.HasAnyOverride() ||
           MotionDetection.HasAnyOverride();

    /// <summary>
    /// Creates a deep copy of this camera overrides.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public CameraOverrides Clone()
        => new()
        {
            Connection = Connection.Clone(),
            CameraDisplay = CameraDisplay.Clone(),
            Performance = Performance.Clone(),
            Recording = Recording.Clone(),
            MotionDetection = MotionDetection.Clone(),
        };

    /// <summary>
    /// Copies values from another camera overrides instance.
    /// </summary>
    /// <param name="source">The source to copy from.</param>
    public void CopyFrom(CameraOverrides? source)
    {
        Connection.CopyFrom(source?.Connection);
        CameraDisplay.CopyFrom(source?.CameraDisplay);
        Performance.CopyFrom(source?.Performance);
        Recording.CopyFrom(source?.Recording);
        MotionDetection.CopyFrom(source?.MotionDetection);
    }

    /// <summary>
    /// Determines whether the specified instance has the same values.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns>True if both have the same values; otherwise, false.</returns>
    public bool ValueEquals(CameraOverrides? other)
    {
        if (other is null)
        {
            return !HasAnyOverride();
        }

        return Connection.ValueEquals(other.Connection) &&
               CameraDisplay.ValueEquals(other.CameraDisplay) &&
               Performance.ValueEquals(other.Performance) &&
               Recording.ValueEquals(other.Recording) &&
               MotionDetection.ValueEquals(other.MotionDetection);
    }
}