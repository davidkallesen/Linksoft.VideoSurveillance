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
    /// Note: AVI removed as it doesn't properly support H.264/H.265 codecs used by modern IP cameras.
    /// </summary>
    public static IDictionary<string, string> RecordingFormatItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["mp4"] = "MP4 (H.264)",
        ["mkv"] = "MKV (Matroska)",
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
    /// Gets the motion sensitivity options for dropdowns.
    /// Keys are sensitivity values (0-100), values are display text.
    /// </summary>
    public static IDictionary<string, string> MotionSensitivityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["10"] = "Low (10)",
        ["30"] = "Medium (30)",
        ["50"] = "High (50)",
        ["70"] = "Very High (70)",
    };

    /// <summary>
    /// Gets the bounding box thickness options for dropdowns.
    /// Keys are thickness in pixels, values are display text.
    /// </summary>
    public static IDictionary<string, string> BoundingBoxThicknessItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["1"] = "1 px",
        ["2"] = "2 px",
        ["3"] = "3 px",
        ["4"] = "4 px",
        ["5"] = "5 px",
    };

    /// <summary>
    /// Gets the bounding box minimum area options for dropdowns.
    /// Keys are area in pixels (at 320x240 analysis resolution), values are display text.
    /// Lower values detect smaller/distant objects but may increase false positives.
    /// </summary>
    public static IDictionary<string, string> BoundingBoxMinAreaItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["10"] = "10 px² (Tiny)",
        ["25"] = "25 px² (Small)",
        ["50"] = "50 px² (Medium)",
        ["100"] = "100 px² (Large)",
        ["200"] = "200 px² (Very Large)",
    };

    /// <summary>
    /// Gets the motion analysis resolution options for dropdowns.
    /// Keys are "WIDTHxHEIGHT", values are display text with performance hints.
    /// Higher resolutions improve small object detection but increase CPU usage.
    /// </summary>
    public static IDictionary<string, string> MotionAnalysisResolutionItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["320x240"] = "320x240 (Low CPU)",
        ["480x360"] = "480x360 (Balanced)",
        ["640x480"] = "640x480 (Better Detection)",
        ["800x600"] = "800x600 (High CPU)",
        ["960x720"] = "960x720 (Very High CPU)",
    };

    /// <summary>
    /// Gets the post-motion duration options for dropdowns (in seconds).
    /// </summary>
    public static IDictionary<string, string> PostMotionDurationItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["5"] = "5 seconds",
        ["10"] = "10 seconds",
        ["15"] = "15 seconds",
        ["30"] = "30 seconds",
        ["60"] = "1 minute",
    };

    /// <summary>
    /// Gets the media cleanup schedule options for dropdowns.
    /// </summary>
    public static IDictionary<string, string> MediaCleanupScheduleItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Disabled"] = "Disabled",
        ["OnStartup"] = "On Application Startup",
        ["OnStartupAndPeriodically"] = "On Startup and Every 6 Hours",
    };

    /// <summary>
    /// Gets the media retention period options for dropdowns (in days).
    /// </summary>
    public static IDictionary<string, string> MediaRetentionPeriodItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["7"] = "7 Days",
        ["14"] = "14 Days",
        ["30"] = "30 Days",
        ["60"] = "60 Days",
        ["90"] = "90 Days",
        ["180"] = "6 Months",
        ["365"] = "1 Year",
    };

    /// <summary>
    /// Gets the maximum recording duration options for dropdowns (in minutes).
    /// </summary>
    public static IDictionary<string, string> MaxRecordingDurationItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["15"] = "15 minutes",
        ["30"] = "30 minutes",
        ["60"] = "1 hour",
        ["120"] = "2 hours",
        ["180"] = "3 hours",
        ["240"] = "4 hours",
    };

    /// <summary>
    /// Gets the thumbnail tile count options for dropdowns.
    /// 1 = single image (320x240), 4 = 2x2 grid (640x480).
    /// </summary>
    public static IDictionary<string, string> ThumbnailTileCountItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["1"] = "1 tile (320x240)",
        ["4"] = "2x2 grid (640x480)",
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
    /// Gets the default motion sensitivity.
    /// </summary>
    public const int DefaultMotionSensitivity = 30;

    /// <summary>
    /// Gets the default post-motion duration in seconds.
    /// </summary>
    public const int DefaultPostMotionDuration = 10;

    /// <summary>
    /// Gets the default bounding box color.
    /// </summary>
    public const string DefaultBoundingBoxColor = "Red";

    /// <summary>
    /// Gets the default bounding box thickness.
    /// </summary>
    public const int DefaultBoundingBoxThickness = 2;

    /// <summary>
    /// Gets the default bounding box minimum area (at 320x240 analysis resolution).
    /// Lower values detect smaller/distant objects.
    /// </summary>
    public const int DefaultBoundingBoxMinArea = 25;

    /// <summary>
    /// Gets the default motion analysis resolution.
    /// </summary>
    public const string DefaultMotionAnalysisResolution = "320x240";

    /// <summary>
    /// Gets the default media cleanup schedule.
    /// </summary>
    public const string DefaultMediaCleanupSchedule = "Disabled";

    /// <summary>
    /// Gets the default recording retention period in days.
    /// </summary>
    public const int DefaultRecordingRetentionDays = 30;

    /// <summary>
    /// Gets the default snapshot retention period in days.
    /// </summary>
    public const int DefaultSnapshotRetentionDays = 7;

    /// <summary>
    /// Gets the default maximum recording duration in minutes.
    /// </summary>
    public const int DefaultMaxRecordingDuration = 60;

    /// <summary>
    /// Gets the default thumbnail tile count (4 = 2x2 grid).
    /// </summary>
    public const int DefaultThumbnailTileCount = 4;

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

    /// <summary>
    /// Parses a resolution string (e.g., "640x480") into width and height.
    /// </summary>
    /// <param name="resolution">The resolution string in "WIDTHxHEIGHT" format.</param>
    /// <returns>A tuple of (width, height), or (320, 240) if parsing fails.</returns>
    public static (int Width, int Height) ParseAnalysisResolution(
        string? resolution)
    {
        if (string.IsNullOrEmpty(resolution))
        {
            return (320, 240);
        }

        var parts = resolution.Split('x');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) &&
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
        {
            return (width, height);
        }

        return (320, 240);
    }

    /// <summary>
    /// Formats width and height into a resolution string (e.g., "640x480").
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>The resolution string in "WIDTHxHEIGHT" format.</returns>
    public static string FormatAnalysisResolution(
        int width,
        int height)
        => $"{width}x{height}";
}