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
           EnableRecordingOnConnect.HasValue;

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
               EnableRecordingOnConnect == other.EnableRecordingOnConnect;
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