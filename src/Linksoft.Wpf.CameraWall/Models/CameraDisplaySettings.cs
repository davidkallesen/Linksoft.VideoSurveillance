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
}
