namespace Linksoft.Wpf.CameraWall.Factories;

/// <summary>
/// Factory for creating common dropdown/combobox items used across dialogs.
/// Centralizes definitions to ensure consistency and simplify maintenance.
/// </summary>
public static class DropDownItemsFactory
{
    /// <summary>
    /// Gets the video quality options for dropdowns.
    /// Keys are the stored values, values are display text.
    /// </summary>
    public static IDictionary<string, string> VideoQualityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Auto"] = "Auto (Source Quality)",
        ["1080p"] = "1080p",
        ["720p"] = "720p",
        ["480p"] = "480p",
        ["360p"] = "360p",
    };

    /// <summary>
    /// Gets the RTSP transport protocol options for dropdowns.
    /// </summary>
    public static IDictionary<string, string> RtspTransportItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["tcp"] = "TCP",
        ["udp"] = "UDP",
    };

    /// <summary>
    /// Gets the recording format options for dropdowns.
    /// </summary>
    public static IDictionary<string, string> RecordingFormatItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["mp4"] = "MP4 (H.264)",
        ["mkv"] = "MKV (Matroska)",
        ["avi"] = "AVI",
    };

    /// <summary>
    /// Gets the overlay position options with friendly display names.
    /// </summary>
    public static IDictionary<string, string> OverlayPositionItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["TopLeft"] = "Top Left",
        ["TopRight"] = "Top Right",
        ["BottomLeft"] = "Bottom Left",
        ["BottomRight"] = "Bottom Right",
    };

    /// <summary>
    /// Gets the overlay opacity options for dropdowns.
    /// Keys are decimal values as strings, values are percentage display text.
    /// </summary>
    public static IDictionary<string, string> OverlayOpacityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["0.0"] = "0%",
        ["0.1"] = "10%",
        ["0.2"] = "20%",
        ["0.3"] = "30%",
        ["0.4"] = "40%",
        ["0.5"] = "50%",
        ["0.6"] = "60%",
        ["0.7"] = "70%",
        ["0.8"] = "80%",
        ["0.9"] = "90%",
        ["1.0"] = "100%",
    };

    /// <summary>
    /// Gets the camera protocol options for dropdowns.
    /// </summary>
    public static IDictionary<string, string> ProtocolItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Rtsp"] = "RTSP",
        ["Http"] = "HTTP",
    };

    /// <summary>
    /// Gets the default video quality value.
    /// </summary>
    public const string DefaultVideoQuality = "Auto";

    /// <summary>
    /// Gets the default RTSP transport protocol.
    /// </summary>
    public const string DefaultRtspTransport = "tcp";

    /// <summary>
    /// Gets the default recording format.
    /// </summary>
    public const string DefaultRecordingFormat = "mp4";

    /// <summary>
    /// Gets the default overlay position.
    /// </summary>
    public const string DefaultOverlayPosition = "TopLeft";

    /// <summary>
    /// Gets the default overlay opacity.
    /// </summary>
    public const string DefaultOverlayOpacity = "0.7";

    /// <summary>
    /// Gets the default camera protocol.
    /// </summary>
    public const string DefaultProtocol = "Rtsp";

    /// <summary>
    /// Converts a video quality setting to the maximum vertical resolution in pixels.
    /// Returns 0 for Auto (no limit).
    /// </summary>
    /// <param name="videoQuality">The video quality setting (e.g., "Auto", "1080p", "720p").</param>
    /// <returns>The maximum vertical resolution in pixels, or 0 for no limit.</returns>
    public static int GetMaxResolutionFromQuality(string videoQuality)
        => videoQuality switch
        {
            // Resolution-based format
            "360p" => 360,
            "480p" => 480,
            "720p" => 720,
            "1080p" => 1080,

            // Legacy format (backward compatibility for existing camera overrides)
            "Low" => 480,
            "Medium" => 720,
            "High" => 1080,

            // Auto = no limit
            _ => 0,
        };
}