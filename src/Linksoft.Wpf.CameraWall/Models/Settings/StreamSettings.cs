namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Represents streaming settings for a network camera.
/// </summary>
public partial class StreamSettings : ObservableObject
{
    [ObservableProperty]
    private bool useLowLatencyMode = true;

    [ObservableProperty]
    private int maxLatencyMs = 500;

    [ObservableProperty]
    private string rtspTransport = "tcp";

    [ObservableProperty]
    private int bufferDurationMs;

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public StreamSettings Clone()
        => new()
        {
            UseLowLatencyMode = UseLowLatencyMode,
            MaxLatencyMs = MaxLatencyMs,
            RtspTransport = RtspTransport,
            BufferDurationMs = BufferDurationMs,
        };

    /// <summary>
    /// Copies values from another instance.
    /// </summary>
    public void CopyFrom(StreamSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        UseLowLatencyMode = source.UseLowLatencyMode;
        MaxLatencyMs = source.MaxLatencyMs;
        RtspTransport = source.RtspTransport;
        BufferDurationMs = source.BufferDurationMs;
    }

    /// <summary>
    /// Determines whether the specified instance has the same values.
    /// </summary>
    public bool ValueEquals(StreamSettings? other)
    {
        if (other is null)
        {
            return false;
        }

        return UseLowLatencyMode == other.UseLowLatencyMode &&
               MaxLatencyMs == other.MaxLatencyMs &&
               string.Equals(RtspTransport, other.RtspTransport, StringComparison.Ordinal) &&
               BufferDurationMs == other.BufferDurationMs;
    }
}