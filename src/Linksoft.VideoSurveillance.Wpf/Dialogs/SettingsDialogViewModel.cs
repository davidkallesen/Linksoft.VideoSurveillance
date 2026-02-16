namespace Linksoft.VideoSurveillance.Wpf.Dialogs;

/// <summary>
/// View model for the settings dialog.
/// Loads/saves settings via REST API (flat AppSettings record).
/// </summary>
[SuppressMessage("", "SA1124: Do not use regions", Justification = "OK")]
[SuppressMessage("", "S2325:Make properties static", Justification = "XAML binding requires instance properties")]
public partial class SettingsDialogViewModel : ViewModelBase
{
    private readonly GatewayService gatewayService;

    // Snapshot of full API settings for preserving server-only fields on save
    private AppSettings? originalApiSettings;

    // Original theme values for restoration on cancel
    private string originalThemeBase = "Dark";
    private string originalThemeAccent = "Blue";

    public SettingsDialogViewModel(GatewayService gatewayService)
    {
        this.gatewayService = gatewayService ?? throw new ArgumentNullException(nameof(gatewayService));
    }

    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    #region General Tab

    [ObservableProperty]
    private string selectedThemeBase = "Dark";

    public IDictionary<string, string> ThemeBaseItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Dark"] = "Dark",
        ["Light"] = "Light",
    };

    [ObservableProperty]
    private string themeAccent = "Blue";

    [ObservableProperty]
    private bool connectOnStartup = true;

    [ObservableProperty]
    private bool startMaximized;

    #endregion

    #region Camera Display Tab

    [ObservableProperty]
    private bool showOverlayTitle = true;

    [ObservableProperty]
    private bool showOverlayDescription = true;

    [ObservableProperty]
    private bool showOverlayTime;

    [ObservableProperty]
    private bool showOverlayConnectionStatus = true;

    [ObservableProperty]
    private string selectedOverlayOpacity = "0.7";

    public IDictionary<string, string> OverlayOpacityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
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

    [ObservableProperty]
    private string selectedOverlayPosition = "TopLeft";

    public IDictionary<string, string> OverlayPositionItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["TopLeft"] = "Top Left",
        ["TopRight"] = "Top Right",
        ["BottomLeft"] = "Bottom Left",
        ["BottomRight"] = "Bottom Right",
    };

    [ObservableProperty]
    private bool allowDragAndDropReorder = true;

    [ObservableProperty]
    private bool autoSaveLayoutChanges = true;

    [ObservableProperty]
    private string snapshotPath = string.Empty;

    #endregion

    #region Connection Tab

    [ObservableProperty]
    private string selectedDefaultProtocol = "Rtsp";

    public IDictionary<string, string> DefaultProtocolItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Rtsp"] = "RTSP",
        ["Http"] = "HTTP",
        ["Https"] = "HTTPS",
    };

    [ObservableProperty]
    private int defaultPort = 554;

    [ObservableProperty]
    private int connectionTimeoutSeconds = 10;

    [ObservableProperty]
    private int reconnectDelaySeconds = 5;

    [ObservableProperty]
    private bool autoReconnectOnFailure = true;

    [ObservableProperty]
    private bool showNotificationOnDisconnect = true;

    [ObservableProperty]
    private bool showNotificationOnReconnect;

    [ObservableProperty]
    private bool playNotificationSound;

    #endregion

    #region Performance Tab

    [ObservableProperty]
    private string selectedVideoQuality = "Auto";

    public IDictionary<string, string> VideoQualityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Auto"] = "Auto",
        ["Low"] = "Low",
        ["Medium"] = "Medium",
        ["High"] = "High",
        ["Ultra"] = "Ultra",
    };

    [ObservableProperty]
    private bool hardwareAcceleration = true;

    [ObservableProperty]
    private bool lowLatencyMode;

    [ObservableProperty]
    private int bufferDurationMs = 500;

    [ObservableProperty]
    private string selectedRtspTransport = "Tcp";

    public IDictionary<string, string> RtspTransportItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Tcp"] = "TCP",
        ["Udp"] = "UDP",
    };

    [ObservableProperty]
    private int maxLatencyMs = 1000;

    #endregion

    #region Recording Tab

    [ObservableProperty]
    private string recordingPath = string.Empty;

    [ObservableProperty]
    private string selectedRecordingFormat = "Mp4";

    public IDictionary<string, string> RecordingFormatItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Mp4"] = "MP4",
        ["Mkv"] = "MKV",
        ["Avi"] = "AVI",
    };

    [ObservableProperty]
    private bool enableRecordingOnMotion;

    [ObservableProperty]
    private bool enableRecordingOnConnect;

    [ObservableProperty]
    private bool enableHourlySegmentation;

    [ObservableProperty]
    private int maxRecordingDurationMinutes = 60;

    #endregion

    #region Advanced Tab

    [ObservableProperty]
    private bool enableDebugLogging;

    [ObservableProperty]
    private string logPath = string.Empty;

    #endregion

    #region Load / Save

    /// <summary>
    /// Loads settings from the API server.
    /// </summary>
    public async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await gatewayService
                .GetSettingsAsync()
                .ConfigureAwait(true);

            if (settings is null)
            {
                return;
            }

            originalApiSettings = settings;

            // Store original theme for cancel restoration
            originalThemeBase = settings.ThemeBase?.ToString() ?? "Dark";
            originalThemeAccent = settings.ThemeAccent ?? "Blue";

            // General
            SelectedThemeBase = settings.ThemeBase?.ToString() ?? "Dark";
            ThemeAccent = settings.ThemeAccent ?? "Blue";
            ConnectOnStartup = settings.ConnectOnStartup;
            StartMaximized = settings.StartMaximized;

            // Camera Display
            ShowOverlayTitle = settings.ShowOverlayTitle;
            ShowOverlayDescription = settings.ShowOverlayDescription;
            ShowOverlayTime = settings.ShowOverlayTime;
            ShowOverlayConnectionStatus = settings.ShowOverlayConnectionStatus;
            SelectedOverlayOpacity = settings.OverlayOpacity.ToString("F1", CultureInfo.InvariantCulture);
            SelectedOverlayPosition = settings.OverlayPosition?.ToString() ?? "TopLeft";
            AllowDragAndDropReorder = settings.AllowDragAndDropReorder;
            AutoSaveLayoutChanges = settings.AutoSaveLayoutChanges;
            SnapshotPath = settings.SnapshotPath ?? string.Empty;

            // Connection
            SelectedDefaultProtocol = settings.DefaultProtocol?.ToString() ?? "Rtsp";
            DefaultPort = settings.DefaultPort;
            ConnectionTimeoutSeconds = settings.ConnectionTimeoutSeconds;
            ReconnectDelaySeconds = settings.ReconnectDelaySeconds;
            AutoReconnectOnFailure = settings.AutoReconnectOnFailure;
            ShowNotificationOnDisconnect = settings.ShowNotificationOnDisconnect;
            ShowNotificationOnReconnect = settings.ShowNotificationOnReconnect;
            PlayNotificationSound = settings.PlayNotificationSound;

            // Performance
            SelectedVideoQuality = settings.VideoQuality?.ToString() ?? "Auto";
            HardwareAcceleration = settings.HardwareAcceleration;
            LowLatencyMode = settings.LowLatencyMode;
            BufferDurationMs = settings.BufferDurationMs;
            SelectedRtspTransport = settings.RtspTransport?.ToString() ?? "Tcp";
            MaxLatencyMs = settings.MaxLatencyMs;

            // Recording
            RecordingPath = settings.RecordingPath ?? string.Empty;
            SelectedRecordingFormat = settings.RecordingFormat?.ToString() ?? "Mp4";
            EnableRecordingOnMotion = settings.EnableRecordingOnMotion;
            EnableRecordingOnConnect = settings.EnableRecordingOnConnect;
            EnableHourlySegmentation = settings.EnableHourlySegmentation;
            MaxRecordingDurationMinutes = settings.MaxRecordingDurationMinutes;

            // Advanced
            EnableDebugLogging = settings.EnableDebugLogging;
            LogPath = settings.LogPath ?? string.Empty;
        }
        catch
        {
            // Failed to load settings - use defaults
        }
    }

    #endregion

    #region Commands

    [RelayCommand("Save")]
    private async Task SaveAsync()
    {
        try
        {
            var settings = BuildApiSettings();
            await gatewayService
                .UpdateSettingsAsync(settings)
                .ConfigureAwait(true);
        }
        catch
        {
            // Save failed - could show error but settings dialog will close
        }

        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    [RelayCommand]
    private void Cancel()
    {
        // Restore original theme on cancel
        RestoreOriginalTheme();
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));
    }

    #endregion

    #region Theme Preview

    /// <summary>
    /// Applies theme preview. Called from code-behind when theme selection changes.
    /// </summary>
    public void ApplyThemePreview()
    {
        var themeBase = SelectedThemeBase;
        var accent = ThemeAccent;

        if (string.IsNullOrEmpty(themeBase) || string.IsNullOrEmpty(accent))
        {
            return;
        }

        try
        {
            ThemeManager.Current.ChangeThemeBaseColor(Application.Current, themeBase);
            ThemeManager.Current.ChangeThemeColorScheme(Application.Current, accent);
        }
        catch
        {
            // Theme change failed - ignore
        }
    }

    private void RestoreOriginalTheme()
    {
        try
        {
            ThemeManager.Current.ChangeThemeBaseColor(Application.Current, originalThemeBase);
            ThemeManager.Current.ChangeThemeColorScheme(Application.Current, originalThemeAccent);
        }
        catch
        {
            // Restore failed - ignore
        }
    }

    #endregion

    #region Private Helpers

    private AppSettings BuildApiSettings()
    {
        // Parse enum values from string selections
        _ = Enum.TryParse<AppSettingsThemeBase>(SelectedThemeBase, out var themeBase);
        _ = Enum.TryParse<AppSettingsOverlayPosition>(SelectedOverlayPosition, out var overlayPosition);
        _ = Enum.TryParse<AppSettingsDefaultProtocol>(SelectedDefaultProtocol, out var protocol);
        _ = Enum.TryParse<AppSettingsVideoQuality>(SelectedVideoQuality, out var videoQuality);
        _ = Enum.TryParse<AppSettingsRtspTransport>(SelectedRtspTransport, out var rtspTransport);
        _ = Enum.TryParse<AppSettingsRecordingFormat>(SelectedRecordingFormat, out var recordingFormat);
        _ = double.TryParse(SelectedOverlayOpacity, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity);

        // Preserve server-only fields from original settings
        var orig = originalApiSettings;

        return new AppSettings(
            ThemeBase: themeBase,
            ThemeAccent: ThemeAccent,
            Language: orig?.Language ?? "1033",
            ConnectOnStartup: ConnectOnStartup,
            StartMaximized: StartMaximized,
            ShowOverlayTitle: ShowOverlayTitle,
            ShowOverlayDescription: ShowOverlayDescription,
            ShowOverlayTime: ShowOverlayTime,
            ShowOverlayConnectionStatus: ShowOverlayConnectionStatus,
            OverlayOpacity: opacity,
            OverlayPosition: overlayPosition,
            AllowDragAndDropReorder: AllowDragAndDropReorder,
            AutoSaveLayoutChanges: AutoSaveLayoutChanges,
            SnapshotPath: SnapshotPath,
            DefaultProtocol: protocol,
            DefaultPort: DefaultPort,
            ConnectionTimeoutSeconds: ConnectionTimeoutSeconds,
            ReconnectDelaySeconds: ReconnectDelaySeconds,
            AutoReconnectOnFailure: AutoReconnectOnFailure,
            ShowNotificationOnDisconnect: ShowNotificationOnDisconnect,
            ShowNotificationOnReconnect: ShowNotificationOnReconnect,
            PlayNotificationSound: PlayNotificationSound,
            VideoQuality: videoQuality,
            HardwareAcceleration: HardwareAcceleration,
            LowLatencyMode: LowLatencyMode,
            BufferDurationMs: BufferDurationMs,
            RtspTransport: rtspTransport,
            MaxLatencyMs: MaxLatencyMs,
            MotionSensitivity: orig?.MotionSensitivity ?? 30,
            MinimumChangePercent: orig?.MinimumChangePercent ?? 0.5,
            AnalysisFrameRate: orig?.AnalysisFrameRate ?? 5,
            AnalysisWidth: orig?.AnalysisWidth ?? 320,
            AnalysisHeight: orig?.AnalysisHeight ?? 240,
            PostMotionDurationSeconds: orig?.PostMotionDurationSeconds ?? 5,
            CooldownSeconds: orig?.CooldownSeconds ?? 3,
            BoundingBoxShowInGrid: orig?.BoundingBoxShowInGrid ?? false,
            BoundingBoxShowInFullScreen: orig?.BoundingBoxShowInFullScreen ?? true,
            BoundingBoxColor: orig?.BoundingBoxColor ?? "#FF0000",
            BoundingBoxThickness: orig?.BoundingBoxThickness ?? 2,
            BoundingBoxMinArea: orig?.BoundingBoxMinArea ?? 500,
            BoundingBoxPadding: orig?.BoundingBoxPadding ?? 10,
            BoundingBoxSmoothing: orig?.BoundingBoxSmoothing ?? 0.3,
            RecordingPath: RecordingPath,
            RecordingFormat: recordingFormat,
            EnableRecordingOnMotion: EnableRecordingOnMotion,
            EnableRecordingOnConnect: EnableRecordingOnConnect,
            EnableHourlySegmentation: EnableHourlySegmentation,
            MaxRecordingDurationMinutes: MaxRecordingDurationMinutes,
            ThumbnailTileCount: orig?.ThumbnailTileCount ?? 4,
            CleanupSchedule: orig?.CleanupSchedule,
            RecordingRetentionDays: orig?.RecordingRetentionDays ?? 30,
            CleanupIncludeSnapshots: orig?.CleanupIncludeSnapshots ?? false,
            SnapshotRetentionDays: orig?.SnapshotRetentionDays ?? 30,
            PlaybackShowFilename: orig?.PlaybackShowFilename ?? true,
            PlaybackFilenameColor: orig?.PlaybackFilenameColor ?? "#FFFFFF",
            PlaybackShowTimestamp: orig?.PlaybackShowTimestamp ?? true,
            PlaybackTimestampColor: orig?.PlaybackTimestampColor ?? "#FFFFFF",
            EnableDebugLogging: EnableDebugLogging,
            LogPath: LogPath);
    }

    #endregion
}
