namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents the configuration for a network camera.
/// </summary>
public partial class CameraConfiguration : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the camera.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.IpAddressRequired))]
    private string ipAddress = string.Empty;

    [ObservableProperty]
    private CameraProtocol protocol = CameraProtocol.Rtsp;

    [ObservableProperty]
    [Range(1, 65535, ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.PortRangeError))]
    private int port = 554;

    [ObservableProperty]
    private string? path;

    [ObservableProperty]
    private string? userName;

    [ObservableProperty]
    private string? password;

    [ObservableProperty]
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.DisplayNameRequired))]
    [StringLength(256, ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.DisplayNameTooLong))]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string? description;

    [ObservableProperty]
    private OverlayPosition overlayPosition = OverlayPosition.TopLeft;

    [ObservableProperty]
    private bool canSwapLeft;

    [ObservableProperty]
    private bool canSwapRight;

    /// <summary>
    /// Builds the camera stream URI based on the configuration.
    /// </summary>
    /// <returns>The constructed URI for the camera stream.</returns>
    public Uri BuildUri()
    {
        var scheme = Protocol.ToScheme();

        var userInfo = !string.IsNullOrEmpty(UserName)
            ? $"{Uri.EscapeDataString(UserName)}:{Uri.EscapeDataString(Password ?? string.Empty)}@"
            : string.Empty;

        var normalizedPath = string.IsNullOrEmpty(Path)
            ? string.Empty
            : $"/{Path.TrimStart('/')}";

        return new Uri($"{scheme}://{userInfo}{IpAddress}:{Port}{normalizedPath}");
    }

    /// <summary>
    /// Returns the display name of the camera.
    /// </summary>
    /// <returns>The display name.</returns>
    public override string ToString()
        => DisplayName;
}