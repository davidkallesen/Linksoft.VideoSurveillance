namespace Linksoft.VideoSurveillance.Enums;

/// <summary>
/// Specifies the connection state of a camera stream.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// The camera is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The camera is in the process of connecting.
    /// </summary>
    Connecting,

    /// <summary>
    /// The camera is connected and streaming.
    /// </summary>
    Connected,

    /// <summary>
    /// The camera is attempting to reconnect after a failure.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The camera connection has encountered an error.
    /// </summary>
    Error,
}