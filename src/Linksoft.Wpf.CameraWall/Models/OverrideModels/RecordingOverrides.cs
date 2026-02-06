namespace Linksoft.Wpf.CameraWall.Models.OverrideModels;

/// <summary>
/// Per-camera recording setting overrides.
/// Nullable properties indicate "use application default" when null.
/// </summary>
public class RecordingOverrides
{
    public string? RecordingPath { get; set; }

    public string? RecordingFormat { get; set; }

    public bool? EnableRecordingOnMotion { get; set; }

    public bool? EnableRecordingOnConnect { get; set; }

    public int? ThumbnailTileCount { get; set; }

    public bool? EnableTimelapse { get; set; }

    public string? TimelapseInterval { get; set; }

    public bool HasAnyOverride()
        => RecordingPath is not null ||
           RecordingFormat is not null ||
           EnableRecordingOnMotion.HasValue ||
           EnableRecordingOnConnect.HasValue ||
           ThumbnailTileCount.HasValue ||
           EnableTimelapse.HasValue ||
           TimelapseInterval is not null;

    public RecordingOverrides Clone()
        => new()
        {
            RecordingPath = RecordingPath,
            RecordingFormat = RecordingFormat,
            EnableRecordingOnMotion = EnableRecordingOnMotion,
            EnableRecordingOnConnect = EnableRecordingOnConnect,
            ThumbnailTileCount = ThumbnailTileCount,
            EnableTimelapse = EnableTimelapse,
            TimelapseInterval = TimelapseInterval,
        };

    public void CopyFrom(RecordingOverrides? source)
    {
        RecordingPath = source?.RecordingPath;
        RecordingFormat = source?.RecordingFormat;
        EnableRecordingOnMotion = source?.EnableRecordingOnMotion;
        EnableRecordingOnConnect = source?.EnableRecordingOnConnect;
        ThumbnailTileCount = source?.ThumbnailTileCount;
        EnableTimelapse = source?.EnableTimelapse;
        TimelapseInterval = source?.TimelapseInterval;
    }

    public bool ValueEquals(RecordingOverrides? other)
    {
        if (other is null)
        {
            return !HasAnyOverride();
        }

        return RecordingPath == other.RecordingPath &&
               RecordingFormat == other.RecordingFormat &&
               EnableRecordingOnMotion == other.EnableRecordingOnMotion &&
               EnableRecordingOnConnect == other.EnableRecordingOnConnect &&
               ThumbnailTileCount == other.ThumbnailTileCount &&
               EnableTimelapse == other.EnableTimelapse &&
               TimelapseInterval == other.TimelapseInterval;
    }
}