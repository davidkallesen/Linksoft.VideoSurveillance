namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents display settings for a network camera.
/// </summary>
public partial class CameraDisplaySettings : ObservableObject
{
    [ObservableProperty]
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.DisplayNameRequired))]
    [StringLength(256, ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.DisplayNameTooLong))]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private OverlayPosition overlayPosition = OverlayPosition.TopLeft;

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public CameraDisplaySettings Clone()
        => new()
        {
            DisplayName = DisplayName,
            Description = Description,
            OverlayPosition = OverlayPosition,
        };

    /// <summary>
    /// Copies values from another instance.
    /// </summary>
    public void CopyFrom(CameraDisplaySettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        DisplayName = source.DisplayName;
        Description = source.Description;
        OverlayPosition = source.OverlayPosition;
    }

    /// <summary>
    /// Determines whether the specified instance has the same values.
    /// </summary>
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