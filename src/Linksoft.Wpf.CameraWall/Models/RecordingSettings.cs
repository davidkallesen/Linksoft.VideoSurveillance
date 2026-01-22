namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Application-level recording settings.
/// </summary>
public class RecordingSettings
{
    /// <summary>
    /// Gets or sets the default recording path.
    /// </summary>
    public string RecordingPath { get; set; } = ApplicationPaths.DefaultRecordingsPath;

    /// <summary>
    /// Gets or sets the default recording format (mp4, mkv, avi).
    /// </summary>
    public string RecordingFormat { get; set; } = "mp4";

    /// <summary>
    /// Gets or sets a value indicating whether to enable recording on motion detection.
    /// </summary>
    public bool EnableRecordingOnMotion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to start recording automatically when cameras connect.
    /// </summary>
    public bool EnableRecordingOnConnect { get; set; }

    /// <summary>
    /// Gets or sets the motion detection settings.
    /// </summary>
    public MotionDetectionSettings MotionDetection { get; set; } = new();
}