namespace Linksoft.VideoSurveillance.Models.Settings;

/// <summary>
/// Settings for camera display and grid layout (application-level defaults).
/// </summary>
public class CameraDisplayAppSettings
{
    public bool ShowOverlayTitle { get; set; } = true;

    public bool ShowOverlayDescription { get; set; } = true;

    public bool ShowOverlayTime { get; set; }

    public bool ShowOverlayConnectionStatus { get; set; } = true;

    public double OverlayOpacity { get; set; } = 0.7;

    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopLeft;

    public bool AllowDragAndDropReorder { get; set; } = true;

    public bool AutoSaveLayoutChanges { get; set; } = true;

    public string SnapshotPath { get; set; } = ApplicationPaths.DefaultSnapshotsPath;

    /// <inheritdoc />
    public override string ToString()
        => $"CameraDisplayAppSettings {{ Title={ShowOverlayTitle}, Status={ShowOverlayConnectionStatus}, Opacity={OverlayOpacity.ToString(CultureInfo.InvariantCulture)} }}";
}