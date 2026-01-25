namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Settings for the recording playback overlay display.
/// </summary>
public class PlaybackOverlaySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to show the filename in the playback overlay.
    /// </summary>
    public bool ShowFilename { get; set; } = true;

    /// <summary>
    /// Gets or sets the color for the filename text.
    /// </summary>
    public string FilenameColor { get; set; } = "White";

    /// <summary>
    /// Gets or sets a value indicating whether to show the timestamp in the playback overlay.
    /// </summary>
    public bool ShowTimestamp { get; set; } = true;

    /// <summary>
    /// Gets or sets the color for the timestamp text.
    /// </summary>
    public string TimestampColor { get; set; } = "White";
}