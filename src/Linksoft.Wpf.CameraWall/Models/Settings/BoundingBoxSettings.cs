namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Settings for motion detection bounding box display.
/// </summary>
public class BoundingBoxSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to show motion bounding boxes in the main grid view.
    /// </summary>
    public bool ShowInGrid { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show motion bounding boxes in full screen mode.
    /// </summary>
    public bool ShowInFullScreen { get; set; }

    /// <summary>
    /// Gets or sets the bounding box border color (well-known color name, e.g., "Red").
    /// </summary>
    public string Color { get; set; } = "Red";

    /// <summary>
    /// Gets or sets the bounding box border thickness in pixels.
    /// </summary>
    public int Thickness { get; set; } = 2;

    /// <summary>
    /// Gets or sets the minimum bounding box area in pixels (analysis resolution) to display.
    /// Helps filter out noise. Lower values detect smaller/distant objects.
    /// At 320x240 analysis resolution: 25 ≈ 5x5 pixels, 100 ≈ 10x10 pixels.
    /// </summary>
    public int MinArea { get; set; } = 10;

    /// <summary>
    /// Gets or sets the bounding box padding in pixels to add around detected motion area.
    /// </summary>
    public int Padding { get; set; } = 4;

    /// <summary>
    /// Gets or sets the smoothing factor for bounding box position (0.0-1.0).
    /// Higher values = more smoothing, less jitter. 0 = no smoothing.
    /// </summary>
    public double Smoothing { get; set; } = 0.3;
}