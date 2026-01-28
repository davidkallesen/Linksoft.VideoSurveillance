namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Settings for motion detection functionality.
/// </summary>
public class MotionDetectionSettings
{
    /// <summary>
    /// Gets or sets the motion sensitivity (0-100).
    /// Higher values require more motion to trigger.
    /// </summary>
    public int Sensitivity { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum percentage of pixels that must change to trigger motion.
    /// </summary>
    public double MinimumChangePercent { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the frame rate at which to analyze for motion (frames per second).
    /// Lower values reduce CPU usage.
    /// </summary>
    public int AnalysisFrameRate { get; set; } = 30;

    /// <summary>
    /// Gets or sets the width of the analysis frame in pixels.
    /// Higher values improve small object detection but increase CPU usage.
    /// </summary>
    public int AnalysisWidth { get; set; } = 800;

    /// <summary>
    /// Gets or sets the height of the analysis frame in pixels.
    /// Higher values improve small object detection but increase CPU usage.
    /// </summary>
    public int AnalysisHeight { get; set; } = 600;

    /// <summary>
    /// Gets or sets how long to continue recording after motion stops (in seconds).
    /// </summary>
    public int PostMotionDurationSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the cooldown period in seconds before motion can trigger a new recording.
    /// </summary>
    public int CooldownSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the bounding box display settings.
    /// </summary>
    public BoundingBoxSettings BoundingBox { get; set; } = new();
}