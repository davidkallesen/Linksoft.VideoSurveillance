namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents connection settings for a network camera.
/// </summary>
public partial class ConnectionSettings : ObservableObject
{
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
}
