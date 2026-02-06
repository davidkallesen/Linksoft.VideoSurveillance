namespace Linksoft.Wpf.CameraWall.Models.OverrideModels;

/// <summary>
/// Per-camera performance setting overrides.
/// Nullable properties indicate "use application default" when null.
/// </summary>
public class PerformanceOverrides
{
    public string? VideoQuality { get; set; }

    public bool? HardwareAcceleration { get; set; }

    public bool HasAnyOverride()
        => VideoQuality is not null ||
           HardwareAcceleration.HasValue;

    public PerformanceOverrides Clone()
        => new()
        {
            VideoQuality = VideoQuality,
            HardwareAcceleration = HardwareAcceleration,
        };

    public void CopyFrom(PerformanceOverrides? source)
    {
        VideoQuality = source?.VideoQuality;
        HardwareAcceleration = source?.HardwareAcceleration;
    }

    public bool ValueEquals(PerformanceOverrides? other)
    {
        if (other is null)
        {
            return !HasAnyOverride();
        }

        return VideoQuality == other.VideoQuality &&
               HardwareAcceleration == other.HardwareAcceleration;
    }
}