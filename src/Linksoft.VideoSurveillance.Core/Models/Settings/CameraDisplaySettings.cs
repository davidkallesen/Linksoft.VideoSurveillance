namespace Linksoft.VideoSurveillance.Models.Settings;

/// <summary>
/// Represents display settings for a network camera (Core POCO).
/// </summary>
public class CameraDisplaySettings
{
    [Required(ErrorMessage = "Display Name is required.")]
    [StringLength(256, ErrorMessage = "Display Name cannot exceed 256 characters.")]
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopLeft;

    /// <inheritdoc />
    public override string ToString()
        => $"CameraDisplaySettings {{ DisplayName='{DisplayName}', Overlay={OverlayPosition} }}";

    public CameraDisplaySettings Clone()
        => new()
        {
            DisplayName = DisplayName,
            Description = Description,
            OverlayPosition = OverlayPosition,
        };

    public void CopyFrom(CameraDisplaySettings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        DisplayName = source.DisplayName;
        Description = source.Description;
        OverlayPosition = source.OverlayPosition;
    }

    public bool ValueEquals(CameraDisplaySettings? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
               string.Equals(Description, other.Description, StringComparison.Ordinal) &&
               OverlayPosition == other.OverlayPosition;
    }
}