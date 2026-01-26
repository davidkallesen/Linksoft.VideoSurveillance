namespace Linksoft.Wpf.CameraWall.Models.Settings;

/// <summary>
/// Application-level performance settings and defaults for video playback.
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Gets or sets the default video quality setting.
    /// </summary>
    public string VideoQuality { get; set; } = "Auto";

    /// <summary>
    /// Gets or sets a value indicating whether hardware acceleration is enabled.
    /// </summary>
    public bool HardwareAcceleration { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether low latency mode is enabled by default.
    /// </summary>
    public bool LowLatencyMode { get; set; }

    /// <summary>
    /// Gets or sets the default buffer duration in milliseconds.
    /// </summary>
    public int BufferDurationMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the default RTSP transport protocol (tcp or udp).
    /// </summary>
    public string RtspTransport { get; set; } = "tcp";

    /// <summary>
    /// Gets or sets the default maximum latency in milliseconds.
    /// </summary>
    public int MaxLatencyMs { get; set; } = 500;
}