namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Settings for camera display and grid layout.
/// </summary>
public class DisplaySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to show camera title in overlay.
    /// </summary>
    public bool ShowOverlayTitle { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show camera description in overlay.
    /// </summary>
    public bool ShowOverlayDescription { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show current time in overlay.
    /// </summary>
    public bool ShowOverlayTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show connection status in overlay.
    /// </summary>
    public bool ShowOverlayConnectionStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets the overlay opacity (0.0 to 1.0).
    /// </summary>
    public double OverlayOpacity { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets a value indicating whether drag and drop reordering is allowed.
    /// </summary>
    public bool AllowDragAndDropReorder { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether layout changes are auto-saved.
    /// </summary>
    public bool AutoSaveLayoutChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets the default directory for saving snapshots.
    /// </summary>
    public string? SnapshotDirectory { get; set; }
}