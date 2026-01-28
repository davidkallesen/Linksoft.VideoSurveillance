namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Per-camera setting overrides that allow individual cameras to deviate from application-level defaults.
/// Nullable properties indicate "use application default" when null.
/// </summary>
public class CameraOverrides
{
    /// <summary>
    /// Gets or sets the connection timeout in seconds, or null to use application default.
    /// </summary>
    public int? ConnectionTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the delay between reconnection attempts in seconds, or null to use application default.
    /// </summary>
    public int? ReconnectDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of reconnection attempts, or null to use application default.
    /// </summary>
    public int? MaxReconnectAttempts { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically reconnect on failure, or null to use application default.
    /// </summary>
    public bool? AutoReconnectOnFailure { get; set; }

    /// <summary>
    /// Gets or sets whether to show a notification when disconnected, or null to use application default.
    /// </summary>
    public bool? ShowNotificationOnDisconnect { get; set; }

    /// <summary>
    /// Gets or sets whether to show a notification when reconnected, or null to use application default.
    /// </summary>
    public bool? ShowNotificationOnReconnect { get; set; }

    /// <summary>
    /// Gets or sets whether to play a notification sound, or null to use application default.
    /// </summary>
    public bool? PlayNotificationSound { get; set; }

    /// <summary>
    /// Gets or sets the video quality setting, or null to use application default.
    /// </summary>
    public string? VideoQuality { get; set; }

    /// <summary>
    /// Gets or sets whether hardware acceleration is enabled, or null to use application default.
    /// </summary>
    public bool? HardwareAcceleration { get; set; }

    /// <summary>
    /// Gets or sets whether to show the overlay title, or null to use application default.
    /// </summary>
    public bool? ShowOverlayTitle { get; set; }

    /// <summary>
    /// Gets or sets whether to show the overlay description, or null to use application default.
    /// </summary>
    public bool? ShowOverlayDescription { get; set; }

    /// <summary>
    /// Gets or sets whether to show the overlay time, or null to use application default.
    /// </summary>
    public bool? ShowOverlayTime { get; set; }

    /// <summary>
    /// Gets or sets whether to show the overlay connection status, or null to use application default.
    /// </summary>
    public bool? ShowOverlayConnectionStatus { get; set; }

    /// <summary>
    /// Gets or sets the overlay opacity (0.0 to 1.0), or null to use application default.
    /// </summary>
    public double? OverlayOpacity { get; set; }

    /// <summary>
    /// Gets or sets the recording path, or null to use application default.
    /// </summary>
    public string? RecordingPath { get; set; }

    /// <summary>
    /// Gets or sets the recording format (mp4, mkv, avi), or null to use application default.
    /// </summary>
    public string? RecordingFormat { get; set; }

    /// <summary>
    /// Gets or sets whether to enable recording on motion detection, or null to use application default.
    /// </summary>
    public bool? EnableRecordingOnMotion { get; set; }

    /// <summary>
    /// Gets or sets the motion detection sensitivity (0-100), or null to use application default.
    /// </summary>
    public int? MotionSensitivity { get; set; }

    /// <summary>
    /// Gets or sets the post-motion recording duration in seconds, or null to use application default.
    /// </summary>
    public int? PostMotionDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether to start recording on connect, or null to use application default.
    /// </summary>
    public bool? EnableRecordingOnConnect { get; set; }

    /// <summary>
    /// Gets or sets the minimum percentage of pixels that must change to trigger motion, or null to use application default.
    /// </summary>
    public double? MotionMinimumChangePercent { get; set; }

    /// <summary>
    /// Gets or sets the frame rate at which to analyze for motion (frames per second), or null to use application default.
    /// </summary>
    public int? MotionAnalysisFrameRate { get; set; }

    /// <summary>
    /// Gets or sets the width of the analysis frame in pixels, or null to use application default.
    /// Higher values improve small object detection but increase CPU usage.
    /// </summary>
    public int? MotionAnalysisWidth { get; set; }

    /// <summary>
    /// Gets or sets the height of the analysis frame in pixels, or null to use application default.
    /// Higher values improve small object detection but increase CPU usage.
    /// </summary>
    public int? MotionAnalysisHeight { get; set; }

    /// <summary>
    /// Gets or sets the cooldown period in seconds before motion can trigger a new recording, or null to use application default.
    /// </summary>
    public int? MotionCooldownSeconds { get; set; }

    /// <summary>
    /// Gets or sets whether to show motion bounding boxes in the main grid view, or null to use application default.
    /// </summary>
    public bool? ShowBoundingBoxInGrid { get; set; }

    /// <summary>
    /// Gets or sets whether to show motion bounding boxes in full screen mode, or null to use application default.
    /// </summary>
    public bool? ShowBoundingBoxInFullScreen { get; set; }

    /// <summary>
    /// Gets or sets the bounding box border color (well-known color name), or null to use application default.
    /// </summary>
    public string? BoundingBoxColor { get; set; }

    /// <summary>
    /// Gets or sets the bounding box border thickness in pixels, or null to use application default.
    /// </summary>
    public int? BoundingBoxThickness { get; set; }

    /// <summary>
    /// Gets or sets the smoothing factor for bounding box position (0.0-1.0), or null to use application default.
    /// </summary>
    public double? BoundingBoxSmoothing { get; set; }

    /// <summary>
    /// Gets or sets the minimum bounding box area in pixels to display, or null to use application default.
    /// </summary>
    public int? BoundingBoxMinArea { get; set; }

    /// <summary>
    /// Gets or sets the bounding box padding in pixels, or null to use application default.
    /// </summary>
    public int? BoundingBoxPadding { get; set; }

    /// <summary>
    /// Gets or sets the number of tiles in the recording thumbnail (1 or 4), or null to use application default.
    /// </summary>
    public int? ThumbnailTileCount { get; set; }

    /// <summary>
    /// Determines whether any override is set (non-null).
    /// </summary>
    /// <returns>True if at least one override is set; otherwise, false.</returns>
    public bool HasAnyOverride()
        => ConnectionTimeoutSeconds.HasValue ||
           ReconnectDelaySeconds.HasValue ||
           MaxReconnectAttempts.HasValue ||
           AutoReconnectOnFailure.HasValue ||
           ShowNotificationOnDisconnect.HasValue ||
           ShowNotificationOnReconnect.HasValue ||
           PlayNotificationSound.HasValue ||
           VideoQuality is not null ||
           HardwareAcceleration.HasValue ||
           ShowOverlayTitle.HasValue ||
           ShowOverlayDescription.HasValue ||
           ShowOverlayTime.HasValue ||
           ShowOverlayConnectionStatus.HasValue ||
           OverlayOpacity.HasValue ||
           RecordingPath is not null ||
           RecordingFormat is not null ||
           EnableRecordingOnMotion.HasValue ||
           MotionSensitivity.HasValue ||
           PostMotionDurationSeconds.HasValue ||
           EnableRecordingOnConnect.HasValue ||
           MotionMinimumChangePercent.HasValue ||
           MotionAnalysisFrameRate.HasValue ||
           MotionAnalysisWidth.HasValue ||
           MotionAnalysisHeight.HasValue ||
           MotionCooldownSeconds.HasValue ||
           ShowBoundingBoxInGrid.HasValue ||
           ShowBoundingBoxInFullScreen.HasValue ||
           BoundingBoxColor is not null ||
           BoundingBoxThickness.HasValue ||
           BoundingBoxSmoothing.HasValue ||
           BoundingBoxMinArea.HasValue ||
           BoundingBoxPadding.HasValue ||
           ThumbnailTileCount.HasValue;

    /// <summary>
    /// Creates a deep copy of this camera overrides.
    /// </summary>
    /// <returns>A new instance with the same values.</returns>
    public CameraOverrides Clone()
        => new()
        {
            ConnectionTimeoutSeconds = ConnectionTimeoutSeconds,
            ReconnectDelaySeconds = ReconnectDelaySeconds,
            MaxReconnectAttempts = MaxReconnectAttempts,
            AutoReconnectOnFailure = AutoReconnectOnFailure,
            ShowNotificationOnDisconnect = ShowNotificationOnDisconnect,
            ShowNotificationOnReconnect = ShowNotificationOnReconnect,
            PlayNotificationSound = PlayNotificationSound,
            VideoQuality = VideoQuality,
            HardwareAcceleration = HardwareAcceleration,
            ShowOverlayTitle = ShowOverlayTitle,
            ShowOverlayDescription = ShowOverlayDescription,
            ShowOverlayTime = ShowOverlayTime,
            ShowOverlayConnectionStatus = ShowOverlayConnectionStatus,
            OverlayOpacity = OverlayOpacity,
            RecordingPath = RecordingPath,
            RecordingFormat = RecordingFormat,
            EnableRecordingOnMotion = EnableRecordingOnMotion,
            MotionSensitivity = MotionSensitivity,
            PostMotionDurationSeconds = PostMotionDurationSeconds,
            EnableRecordingOnConnect = EnableRecordingOnConnect,
            MotionMinimumChangePercent = MotionMinimumChangePercent,
            MotionAnalysisFrameRate = MotionAnalysisFrameRate,
            MotionAnalysisWidth = MotionAnalysisWidth,
            MotionAnalysisHeight = MotionAnalysisHeight,
            MotionCooldownSeconds = MotionCooldownSeconds,
            ShowBoundingBoxInGrid = ShowBoundingBoxInGrid,
            ShowBoundingBoxInFullScreen = ShowBoundingBoxInFullScreen,
            BoundingBoxColor = BoundingBoxColor,
            BoundingBoxThickness = BoundingBoxThickness,
            BoundingBoxSmoothing = BoundingBoxSmoothing,
            BoundingBoxMinArea = BoundingBoxMinArea,
            BoundingBoxPadding = BoundingBoxPadding,
            ThumbnailTileCount = ThumbnailTileCount,
        };

    /// <summary>
    /// Copies values from another camera overrides instance.
    /// </summary>
    /// <param name="source">The source to copy from.</param>
    public void CopyFrom(CameraOverrides? source)
    {
        if (source is null)
        {
            // Reset all overrides to null (use defaults)
            ConnectionTimeoutSeconds = null;
            ReconnectDelaySeconds = null;
            MaxReconnectAttempts = null;
            AutoReconnectOnFailure = null;
            ShowNotificationOnDisconnect = null;
            ShowNotificationOnReconnect = null;
            PlayNotificationSound = null;
            VideoQuality = null;
            HardwareAcceleration = null;
            ShowOverlayTitle = null;
            ShowOverlayDescription = null;
            ShowOverlayTime = null;
            ShowOverlayConnectionStatus = null;
            OverlayOpacity = null;
            RecordingPath = null;
            RecordingFormat = null;
            EnableRecordingOnMotion = null;
            MotionSensitivity = null;
            PostMotionDurationSeconds = null;
            EnableRecordingOnConnect = null;
            MotionMinimumChangePercent = null;
            MotionAnalysisFrameRate = null;
            MotionAnalysisWidth = null;
            MotionAnalysisHeight = null;
            MotionCooldownSeconds = null;
            ShowBoundingBoxInGrid = null;
            ShowBoundingBoxInFullScreen = null;
            BoundingBoxColor = null;
            BoundingBoxThickness = null;
            BoundingBoxSmoothing = null;
            BoundingBoxMinArea = null;
            BoundingBoxPadding = null;
            ThumbnailTileCount = null;
            return;
        }

        ConnectionTimeoutSeconds = source.ConnectionTimeoutSeconds;
        ReconnectDelaySeconds = source.ReconnectDelaySeconds;
        MaxReconnectAttempts = source.MaxReconnectAttempts;
        AutoReconnectOnFailure = source.AutoReconnectOnFailure;
        ShowNotificationOnDisconnect = source.ShowNotificationOnDisconnect;
        ShowNotificationOnReconnect = source.ShowNotificationOnReconnect;
        PlayNotificationSound = source.PlayNotificationSound;
        VideoQuality = source.VideoQuality;
        HardwareAcceleration = source.HardwareAcceleration;
        ShowOverlayTitle = source.ShowOverlayTitle;
        ShowOverlayDescription = source.ShowOverlayDescription;
        ShowOverlayTime = source.ShowOverlayTime;
        ShowOverlayConnectionStatus = source.ShowOverlayConnectionStatus;
        OverlayOpacity = source.OverlayOpacity;
        RecordingPath = source.RecordingPath;
        RecordingFormat = source.RecordingFormat;
        EnableRecordingOnMotion = source.EnableRecordingOnMotion;
        MotionSensitivity = source.MotionSensitivity;
        PostMotionDurationSeconds = source.PostMotionDurationSeconds;
        EnableRecordingOnConnect = source.EnableRecordingOnConnect;
        MotionMinimumChangePercent = source.MotionMinimumChangePercent;
        MotionAnalysisFrameRate = source.MotionAnalysisFrameRate;
        MotionAnalysisWidth = source.MotionAnalysisWidth;
        MotionAnalysisHeight = source.MotionAnalysisHeight;
        MotionCooldownSeconds = source.MotionCooldownSeconds;
        ShowBoundingBoxInGrid = source.ShowBoundingBoxInGrid;
        ShowBoundingBoxInFullScreen = source.ShowBoundingBoxInFullScreen;
        BoundingBoxColor = source.BoundingBoxColor;
        BoundingBoxThickness = source.BoundingBoxThickness;
        BoundingBoxSmoothing = source.BoundingBoxSmoothing;
        BoundingBoxMinArea = source.BoundingBoxMinArea;
        BoundingBoxPadding = source.BoundingBoxPadding;
        ThumbnailTileCount = source.ThumbnailTileCount;
    }

    /// <summary>
    /// Determines whether the specified instance has the same values.
    /// </summary>
    /// <param name="other">The other instance to compare.</param>
    /// <returns>True if both have the same values; otherwise, false.</returns>
    public bool ValueEquals(CameraOverrides? other)
    {
        if (other is null)
        {
            return !HasAnyOverride();
        }

        return ConnectionTimeoutSeconds == other.ConnectionTimeoutSeconds &&
               ReconnectDelaySeconds == other.ReconnectDelaySeconds &&
               MaxReconnectAttempts == other.MaxReconnectAttempts &&
               AutoReconnectOnFailure == other.AutoReconnectOnFailure &&
               ShowNotificationOnDisconnect == other.ShowNotificationOnDisconnect &&
               ShowNotificationOnReconnect == other.ShowNotificationOnReconnect &&
               PlayNotificationSound == other.PlayNotificationSound &&
               VideoQuality == other.VideoQuality &&
               HardwareAcceleration == other.HardwareAcceleration &&
               ShowOverlayTitle == other.ShowOverlayTitle &&
               ShowOverlayDescription == other.ShowOverlayDescription &&
               ShowOverlayTime == other.ShowOverlayTime &&
               ShowOverlayConnectionStatus == other.ShowOverlayConnectionStatus &&
               NullableDoubleEquals(OverlayOpacity, other.OverlayOpacity) &&
               RecordingPath == other.RecordingPath &&
               RecordingFormat == other.RecordingFormat &&
               EnableRecordingOnMotion == other.EnableRecordingOnMotion &&
               MotionSensitivity == other.MotionSensitivity &&
               PostMotionDurationSeconds == other.PostMotionDurationSeconds &&
               EnableRecordingOnConnect == other.EnableRecordingOnConnect &&
               NullableDoubleEquals(MotionMinimumChangePercent, other.MotionMinimumChangePercent) &&
               MotionAnalysisFrameRate == other.MotionAnalysisFrameRate &&
               MotionAnalysisWidth == other.MotionAnalysisWidth &&
               MotionAnalysisHeight == other.MotionAnalysisHeight &&
               MotionCooldownSeconds == other.MotionCooldownSeconds &&
               ShowBoundingBoxInGrid == other.ShowBoundingBoxInGrid &&
               ShowBoundingBoxInFullScreen == other.ShowBoundingBoxInFullScreen &&
               BoundingBoxColor == other.BoundingBoxColor &&
               BoundingBoxThickness == other.BoundingBoxThickness &&
               NullableDoubleEquals(BoundingBoxSmoothing, other.BoundingBoxSmoothing) &&
               BoundingBoxMinArea == other.BoundingBoxMinArea &&
               BoundingBoxPadding == other.BoundingBoxPadding &&
               ThumbnailTileCount == other.ThumbnailTileCount;
    }

    private static bool NullableDoubleEquals(
        double? a,
        double? b,
        double tolerance = 1e-9)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return true;
        }

        if (!a.HasValue || !b.HasValue)
        {
            return false;
        }

        return Math.Abs(a.Value - b.Value) < tolerance;
    }
}