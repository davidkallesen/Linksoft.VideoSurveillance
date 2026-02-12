namespace Linksoft.VideoSurveillance.Models.Settings;

/// <summary>
/// Settings for automatic cleanup of old recordings and snapshots.
/// </summary>
public class MediaCleanupSettings
{
    public MediaCleanupSchedule Schedule { get; set; } = MediaCleanupSchedule.Disabled;

    public int RecordingRetentionDays { get; set; } = 30;

    public bool IncludeSnapshots { get; set; }

    public int SnapshotRetentionDays { get; set; } = 7;

    /// <inheritdoc />
    public override string ToString()
        => $"MediaCleanupSettings {{ Schedule={Schedule}, RetentionDays={RecordingRetentionDays.ToString(CultureInfo.InvariantCulture)} }}";
}