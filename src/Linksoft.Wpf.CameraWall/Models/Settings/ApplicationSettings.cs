namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Root container for all application settings.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Gets or sets the general settings (theme, language, startup).
    /// </summary>
    public GeneralSettings General { get; set; } = new();

    /// <summary>
    /// Gets or sets the camera display settings (overlay, grid layout).
    /// </summary>
    public CameraDisplayAppSettings CameraDisplay { get; set; } = new();

    /// <summary>
    /// Gets or sets the connection settings (defaults, timeouts, reconnect).
    /// </summary>
    public ConnectionAppSettings Connection { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance settings (video quality, hardware acceleration).
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets the motion detection settings (sensitivity, bounding box display).
    /// </summary>
    public MotionDetectionSettings MotionDetection { get; set; } = new();

    /// <summary>
    /// Gets or sets the recording settings (path, format).
    /// </summary>
    public RecordingSettings Recording { get; set; } = new();

    /// <summary>
    /// Gets or sets the advanced settings (logging, debugging).
    /// </summary>
    public AdvancedSettings Advanced { get; set; } = new();
}