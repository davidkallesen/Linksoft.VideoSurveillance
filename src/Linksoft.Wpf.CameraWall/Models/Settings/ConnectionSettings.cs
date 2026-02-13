using CoreSettings = Linksoft.VideoSurveillance.Models.Settings;

namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Wraps <see cref="CoreSettings.ConnectionSettings"/> with change notification for WPF binding.
/// </summary>
public partial class ConnectionSettings : ObservableObject
{
    internal CoreSettings.ConnectionSettings Core { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionSettings"/> class.
    /// </summary>
    public ConnectionSettings()
        : this(new CoreSettings.ConnectionSettings())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionSettings"/> class
    /// wrapping the specified Core instance.
    /// </summary>
    internal ConnectionSettings(CoreSettings.ConnectionSettings core)
    {
        Core = core ?? throw new ArgumentNullException(nameof(core));
    }

    /// <summary>
    /// Gets or sets the IP address of the camera.
    /// </summary>
    [Required(ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.IpAddressRequired))]
    public string IpAddress
    {
        get => Core.IpAddress;
        set
        {
            if (string.Equals(Core.IpAddress, value, StringComparison.Ordinal))
            {
                return;
            }

            Core.IpAddress = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the protocol used to connect.
    /// </summary>
    public CameraProtocol Protocol
    {
        get => Core.Protocol;
        set
        {
            if (Core.Protocol == value)
            {
                return;
            }

            Core.Protocol = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    [Range(1, 65535, ErrorMessageResourceType = typeof(Translations), ErrorMessageResourceName = nameof(Translations.PortRangeError))]
    public int Port
    {
        get => Core.Port;
        set
        {
            if (Core.Port == value)
            {
                return;
            }

            Core.Port = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the stream path.
    /// </summary>
    public string? Path
    {
        get => Core.Path;
        set
        {
            if (string.Equals(Core.Path, value, StringComparison.Ordinal))
            {
                return;
            }

            Core.Path = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public ConnectionSettings Clone()
        => new(Core.Clone());

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
        => other is not null && Core.ValueEquals(other.Core);
}