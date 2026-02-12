namespace Linksoft.VideoSurveillance.Models.Overrides;

/// <summary>
/// Per-camera display setting overrides.
/// </summary>
public class CameraDisplayOverrides
{
    public bool? ShowOverlayTitle { get; set; }

    public bool? ShowOverlayDescription { get; set; }

    public bool? ShowOverlayTime { get; set; }

    public bool? ShowOverlayConnectionStatus { get; set; }

    public double? OverlayOpacity { get; set; }

    public bool HasAnyOverride()
        => ShowOverlayTitle.HasValue ||
           ShowOverlayDescription.HasValue ||
           ShowOverlayTime.HasValue ||
           ShowOverlayConnectionStatus.HasValue ||
           OverlayOpacity.HasValue;

    /// <inheritdoc />
    public override string ToString()
    {
        var count = new[] { ShowOverlayTitle.HasValue, ShowOverlayDescription.HasValue, ShowOverlayTime.HasValue, ShowOverlayConnectionStatus.HasValue, OverlayOpacity.HasValue }.Count(v => v);
        return $"CameraDisplayOverrides {{ NonNullOverrides={count.ToString(CultureInfo.InvariantCulture)} }}";
    }

    public CameraDisplayOverrides Clone()
        => new()
        {
            ShowOverlayTitle = ShowOverlayTitle,
            ShowOverlayDescription = ShowOverlayDescription,
            ShowOverlayTime = ShowOverlayTime,
            ShowOverlayConnectionStatus = ShowOverlayConnectionStatus,
            OverlayOpacity = OverlayOpacity,
        };

    public void CopyFrom(CameraDisplayOverrides? source)
    {
        ShowOverlayTitle = source?.ShowOverlayTitle;
        ShowOverlayDescription = source?.ShowOverlayDescription;
        ShowOverlayTime = source?.ShowOverlayTime;
        ShowOverlayConnectionStatus = source?.ShowOverlayConnectionStatus;
        OverlayOpacity = source?.OverlayOpacity;
    }

    public bool ValueEquals(CameraDisplayOverrides? other)
    {
        if (other is null)
        {
            return !HasAnyOverride();
        }

        return ShowOverlayTitle == other.ShowOverlayTitle &&
               ShowOverlayDescription == other.ShowOverlayDescription &&
               ShowOverlayTime == other.ShowOverlayTime &&
               ShowOverlayConnectionStatus == other.ShowOverlayConnectionStatus &&
               OverlayOpacity.IsEqual(other.OverlayOpacity);
    }
}