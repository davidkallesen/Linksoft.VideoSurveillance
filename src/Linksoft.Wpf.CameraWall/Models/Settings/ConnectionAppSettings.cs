namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Application-level connection settings and defaults for new cameras.
/// </summary>
public class ConnectionAppSettings
{
    /// <summary>
    /// Gets or sets the default protocol for new cameras.
    /// </summary>
    public CameraProtocol DefaultProtocol { get; set; } = CameraProtocol.Rtsp;

    /// <summary>
    /// Gets or sets the default port for new cameras.
    /// </summary>
    public int DefaultPort { get; set; } = 554;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the delay between reconnection attempts in seconds.
    /// </summary>
    public int ReconnectDelaySeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically reconnect on failure.
    /// </summary>
    public bool AutoReconnectOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show a notification when a camera disconnects.
    /// </summary>
    public bool ShowNotificationOnDisconnect { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to show a notification when a camera reconnects.
    /// </summary>
    public bool ShowNotificationOnReconnect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to play a sound on notifications.
    /// </summary>
    public bool PlayNotificationSound { get; set; }
}