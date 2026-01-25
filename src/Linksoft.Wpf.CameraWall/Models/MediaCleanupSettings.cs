namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Settings for automatic cleanup of old recordings and snapshots.
/// </summary>
public class MediaCleanupSettings
{
    /// <summary>
    /// Gets or sets the cleanup schedule (when cleanup runs).
    /// </summary>
    public MediaCleanupSchedule Schedule { get; set; } = MediaCleanupSchedule.Disabled;

    /// <summary>
    /// Gets or sets the number of days to retain recordings before deletion.
    /// </summary>
    public int RecordingRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to include snapshots in cleanup.
    /// </summary>
    public bool IncludeSnapshots { get; set; }

    /// <summary>
    /// Gets or sets the number of days to retain snapshots before deletion.
    /// </summary>
    public int SnapshotRetentionDays { get; set; } = 7;
}