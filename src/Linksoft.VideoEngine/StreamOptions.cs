namespace Linksoft.VideoEngine;

/// <summary>
/// Options for opening a video stream.
/// </summary>
public sealed class StreamOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to use low-latency mode.
    /// When enabled, reduces buffer sizes and enables low-delay decoding.
    /// </summary>
    public bool UseLowLatencyMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum allowed latency in milliseconds.
    /// The player will speed up to catch up if latency exceeds this value.
    /// A value of 0 disables latency management.
    /// </summary>
    public int MaxLatencyMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the RTSP transport protocol ("tcp" or "udp").
    /// </summary>
    public string RtspTransport { get; set; } = "tcp";

    /// <summary>
    /// Gets or sets the demuxer buffer duration in milliseconds.
    /// </summary>
    public int BufferDurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hardware-accelerated decoding is enabled.
    /// </summary>
    public bool HardwareAcceleration { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum vertical resolution (0 = no limit).
    /// Used to limit decoding resolution for performance.
    /// </summary>
    public int MaxVerticalResolution { get; set; }

    /// <inheritdoc />
    public override string ToString()
        => $"StreamOptions {{ Transport='{RtspTransport}', LowLatency={UseLowLatencyMode}, MaxLatency={MaxLatencyMs}ms, HwAccel={HardwareAcceleration} }}";
}