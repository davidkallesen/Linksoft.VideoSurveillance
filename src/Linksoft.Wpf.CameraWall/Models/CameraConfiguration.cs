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

    [ObservableProperty(AfterChangedCallback = nameof(OnConnectionChanged))]
    private ConnectionSettings connection = new();

    [ObservableProperty(AfterChangedCallback = nameof(OnAuthenticationChanged))]
    private AuthenticationSettings authentication = new();

    [ObservableProperty(AfterChangedCallback = nameof(OnDisplayChanged))]
    private CameraDisplaySettings display = new();

    [ObservableProperty(AfterChangedCallback = nameof(OnStreamChanged))]
    private StreamSettings stream = new();

    [JsonIgnore]
    [ObservableProperty]
    private bool canSwapLeft;

    [JsonIgnore]
    [ObservableProperty]
    private bool canSwapRight;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraConfiguration"/> class.
    /// </summary>
    public CameraConfiguration()
    {
        SubscribeToNestedChanges(Connection);
        SubscribeToNestedChanges(Authentication);
        SubscribeToNestedChanges(Display);
        SubscribeToNestedChanges(Stream);
    }

    /// <summary>
    /// Builds the camera stream URI based on the configuration.
    /// </summary>
    /// <returns>The constructed URI for the camera stream.</returns>
    public Uri BuildUri()
    {
        var scheme = Connection.Protocol.ToScheme();

        var userInfo = !string.IsNullOrEmpty(Authentication.UserName)
            ? $"{Uri.EscapeDataString(Authentication.UserName)}:{Uri.EscapeDataString(Authentication.Password ?? string.Empty)}@"
            : string.Empty;

        var normalizedPath = string.IsNullOrEmpty(Connection.Path)
            ? string.Empty
            : $"/{Connection.Path.TrimStart('/')}";

        return new Uri($"{scheme}://{userInfo}{Connection.IpAddress}:{Connection.Port}{normalizedPath}");
    }

    /// <summary>
    /// Returns the display name of the camera.
    /// </summary>
    /// <returns>The display name.</returns>
    public override string ToString()
        => Display.DisplayName;

    private void OnConnectionChanged()
    {
        // Resubscribe to the new nested object's property changes
        SubscribeToNestedChanges(Connection);
    }

    private void OnAuthenticationChanged()
    {
        SubscribeToNestedChanges(Authentication);
    }

    private void OnDisplayChanged()
    {
        SubscribeToNestedChanges(Display);
    }

    private void OnStreamChanged()
    {
        SubscribeToNestedChanges(Stream);
    }

    private void SubscribeToNestedChanges(INotifyPropertyChanged? nested)
    {
        if (nested is not null)
        {
            nested.PropertyChanged += OnNestedPropertyChanged;
        }
    }

    private void OnNestedPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward the change notification for the parent property
        // WPF bindings to nested paths (e.g., Camera.Connection.IpAddress) work automatically
        // because WPF subscribes to each object's PropertyChanged in the path
        var parentPropertyName = sender switch
        {
            ConnectionSettings => nameof(Connection),
            AuthenticationSettings => nameof(Authentication),
            CameraDisplaySettings => nameof(Display),
            StreamSettings => nameof(Stream),
            _ => null,
        };

        if (parentPropertyName is not null)
        {
            OnPropertyChanged(parentPropertyName);
        }
    }
}
