namespace Linksoft.Wpf.CameraWall.Models.Settings;

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

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public ConnectionSettings Clone()
        => new()
        {
            IpAddress = IpAddress,
            Protocol = Protocol,
            Port = Port,
            Path = Path,
        };

    /// <summary>
    /// Copies values from another instance.
    /// </summary>
    public void CopyFrom(ConnectionSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        IpAddress = source.IpAddress;
        Protocol = source.Protocol;
        Port = source.Port;
        Path = source.Path;
    }

    /// <summary>
    /// Determines whether the specified instance has the same values.
    /// </summary>
    public bool ValueEquals(ConnectionSettings? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(IpAddress, other.IpAddress, StringComparison.Ordinal) &&
               Protocol == other.Protocol &&
               Port == other.Port &&
               string.Equals(Path, other.Path, StringComparison.Ordinal);
    }
}