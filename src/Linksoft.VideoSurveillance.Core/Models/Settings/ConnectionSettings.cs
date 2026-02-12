namespace Linksoft.VideoSurveillance.Models.Settings;

/// <summary>
/// Represents connection settings for a network camera (Core POCO).
/// </summary>
public class ConnectionSettings
{
    [Required(ErrorMessage = "IP Address is required.")]
    public string IpAddress { get; set; } = string.Empty;

    public CameraProtocol Protocol { get; set; } = CameraProtocol.Rtsp;

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 554;

    public string? Path { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"ConnectionSettings {{ Protocol={Protocol}, IpAddress='{IpAddress}', Port={Port.ToString(CultureInfo.InvariantCulture)} }}";

    public ConnectionSettings Clone()
        => new()
        {
            IpAddress = IpAddress,
            Protocol = Protocol,
            Port = Port,
            Path = Path,
        };

    public void CopyFrom(ConnectionSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        IpAddress = source.IpAddress;
        Protocol = source.Protocol;
        Port = source.Port;
        Path = source.Path;
    }

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