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

    /// <summary>
    /// Gets or sets the media cleanup settings.
    /// </summary>
    public MediaCleanupSettings Cleanup { get; set; } = new();

    /// <summary>
    /// Gets or sets the playback overlay settings.
    /// </summary>
    public PlaybackOverlaySettings PlaybackOverlay { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable hourly segmentation of recordings.
    /// When enabled, recordings are automatically split at the top of each hour.
    /// </summary>
    public bool EnableHourlySegmentation { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum recording duration in minutes before automatic segmentation.
    /// This acts as a failsafe to ensure recordings don't exceed the specified duration.
    /// </summary>
    public int MaxRecordingDurationMinutes { get; set; } = 60;
}