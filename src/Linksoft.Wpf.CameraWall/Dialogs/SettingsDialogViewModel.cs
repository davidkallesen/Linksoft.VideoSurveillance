namespace Linksoft.Wpf.CameraWall.Dialogs;

/// <summary>
/// View model for the settings dialog.
/// </summary>
[SuppressMessage("", "SA1124: Do not use regions", Justification = "OK")]
public partial class SettingsDialogViewModel : ViewModelDialogBase
{
    private readonly IApplicationSettingsService settingsService;

    // Original values for restoration on cancel
    private string originalThemeBase = "Dark";
    private string originalThemeAccent = "Blue";
    private string originalLanguage = "1033";

    public SettingsDialogViewModel(IApplicationSettingsService settingsService)
    {
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        LoadSettings();
    }

    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    public static string DialogTitle => Translations.Settings;

    #region General Tab Settings

    /// <summary>
    /// Gets or sets the selected language LCID (e.g., "1033" for en-US).
    /// </summary>
    [ObservableProperty]
    private string selectedLanguage = "1033";

    [ObservableProperty]
    private bool connectCamerasOnStartup = true;

    [ObservableProperty]
    private bool startMaximized;

    [ObservableProperty]
    private bool startRibbonCollapsed;

    #endregion

    #region Camera Display Tab Settings

    [ObservableProperty]
    private bool showCameraOverlayTitle = true;

    [ObservableProperty]
    private bool showCameraOverlayDescription = true;

    [ObservableProperty]
    private bool showCameraOverlayTime;

    [ObservableProperty]
    private bool showCameraOverlayConnectionStatus = true;

    [ObservableProperty]
    private string selectedOverlayOpacity = "0.7";

    public IDictionary<string, string> OverlayOpacityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["0.3"] = "30%",
        ["0.5"] = "50%",
        ["0.7"] = "70%",
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
    private DirectoryInfo? snapshotDirectory;

    #endregion

    #region Connection Tab Settings

    [ObservableProperty]
    private string selectedDefaultProtocol = "Rtsp";

    public IDictionary<string, string> DefaultProtocolItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Rtsp"] = "RTSP",
        ["Http"] = "HTTP",
    };

    [ObservableProperty]
    private int defaultPort = 554;

    [ObservableProperty]
    private int connectionTimeoutSeconds = 10;

    [ObservableProperty]
    private int reconnectDelaySeconds = 5;

    [ObservableProperty]
    private int maxReconnectAttempts = 3;

    [ObservableProperty]
    private bool autoReconnectOnFailure = true;

    [ObservableProperty]
    private bool showNotificationOnDisconnect = true;

    [ObservableProperty]
    private bool showNotificationOnReconnect;

    [ObservableProperty]
    private bool playNotificationSound;

    #endregion

    #region Performance Tab Settings

    [ObservableProperty]
    private string selectedVideoQuality = "Auto";

    public IDictionary<string, string> VideoQualityItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Auto"] = "Auto (Source Quality)",
        ["1080p"] = "1080p",
        ["720p"] = "720p",
        ["480p"] = "480p",
        ["360p"] = "360p",
    };

    [ObservableProperty]
    private bool hardwareAcceleration = true;

    [ObservableProperty]
    private bool lowLatencyMode;

    [ObservableProperty]
    private int bufferDurationMs = 500;

    [ObservableProperty]
    private string selectedRtspTransport = "tcp";

    public IDictionary<string, string> RtspTransportItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["tcp"] = "TCP",
        ["udp"] = "UDP",
    };

    [ObservableProperty]
    private int maxLatencyMs = 500;

    #endregion

    #region Recording Tab Settings

    [ObservableProperty]
    private DirectoryInfo? recordingPath;

    [ObservableProperty]
    private string selectedRecordingFormat = "mp4";

    public IDictionary<string, string> RecordingFormatItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["mp4"] = "MP4 (H.264)",
        ["mkv"] = "MKV (Matroska)",
        ["avi"] = "AVI",
    };

    [ObservableProperty]
    private bool enableRecordingOnMotion;

    #endregion

    #region Advanced Tab Settings

    [ObservableProperty]
    private bool enableDebugLogging;

    [ObservableProperty]
    private FileInfo? logFilePath;

    #endregion

    #region Commands

    [RelayCommand]
    private void Save()
    {
        SaveGeneralSettings();
        SaveCameraDisplaySettings();
        SaveConnectionSettings();
        SavePerformanceSettings();
        SaveRecordingSettings();
        SaveAdvancedSettings();
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    [RelayCommand]
    private void Cancel()
    {
        RestoreOriginalThemeAndLanguage();
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));
    }

    [RelayCommand]
    private void RestoreDefaults()
    {
        // General Tab
        SelectedLanguage = "1033"; // en-US
        ConnectCamerasOnStartup = true;
        StartMaximized = false;
        StartRibbonCollapsed = false;

        // Apply default theme/accent/language immediately
        ThemeManager.Current.ChangeThemeBaseColor(Application.Current, "Dark");
        ThemeManager.Current.ChangeThemeColorScheme(Application.Current, "Blue");
        CultureManager.UiCulture = new CultureInfo(1033);

        // Camera Display Tab
        ShowCameraOverlayTitle = true;
        ShowCameraOverlayDescription = true;
        ShowCameraOverlayTime = false;
        ShowCameraOverlayConnectionStatus = true;
        SelectedOverlayOpacity = "0.7";
        SelectedOverlayPosition = "TopLeft";
        AllowDragAndDropReorder = true;
        AutoSaveLayoutChanges = true;
        SnapshotDirectory = null;

        // Connection Tab
        SelectedDefaultProtocol = "Rtsp";
        DefaultPort = 554;
        ConnectionTimeoutSeconds = 10;
        ReconnectDelaySeconds = 5;
        MaxReconnectAttempts = 3;
        AutoReconnectOnFailure = true;
        ShowNotificationOnDisconnect = true;
        ShowNotificationOnReconnect = false;
        PlayNotificationSound = false;

        // Performance Tab
        SelectedVideoQuality = "Auto";
        HardwareAcceleration = true;
        LowLatencyMode = false;
        BufferDurationMs = 500;
        SelectedRtspTransport = "tcp";
        MaxLatencyMs = 500;

        // Recording Tab
        RecordingPath = null;
        SelectedRecordingFormat = "mp4";
        EnableRecordingOnMotion = false;

        // Advanced Tab
        EnableDebugLogging = false;
        LogFilePath = null;
    }

    #endregion

    #region Private Methods

    private void LoadSettings()
    {
        var general = settingsService.General;

        // Store original values for cancel restoration
        originalThemeBase = general.ThemeBase;
        originalThemeAccent = general.ThemeAccent;
        originalLanguage = general.Language;

        // Load General Tab settings
        SelectedLanguage = general.Language;
        ConnectCamerasOnStartup = general.ConnectCamerasOnStartup;
        StartMaximized = general.StartMaximized;
        StartRibbonCollapsed = general.StartRibbonCollapsed;

        // Load Camera Display Tab settings
        var cameraDisplay = settingsService.CameraDisplay;
        ShowCameraOverlayTitle = cameraDisplay.ShowOverlayTitle;
        ShowCameraOverlayDescription = cameraDisplay.ShowOverlayDescription;
        ShowCameraOverlayTime = cameraDisplay.ShowOverlayTime;
        ShowCameraOverlayConnectionStatus = cameraDisplay.ShowOverlayConnectionStatus;
        SelectedOverlayOpacity = cameraDisplay.OverlayOpacity.ToString("F1", CultureInfo.InvariantCulture);
        SelectedOverlayPosition = cameraDisplay.OverlayPosition.ToString();
        AllowDragAndDropReorder = cameraDisplay.AllowDragAndDropReorder;
        AutoSaveLayoutChanges = cameraDisplay.AutoSaveLayoutChanges;
        SnapshotDirectory = !string.IsNullOrEmpty(cameraDisplay.SnapshotDirectory)
            ? new DirectoryInfo(cameraDisplay.SnapshotDirectory)
            : null;

        // Load Connection Tab settings
        var connection = settingsService.Connection;
        SelectedDefaultProtocol = connection.DefaultProtocol.ToString();
        DefaultPort = connection.DefaultPort;
        ConnectionTimeoutSeconds = connection.ConnectionTimeoutSeconds;
        ReconnectDelaySeconds = connection.ReconnectDelaySeconds;
        MaxReconnectAttempts = connection.MaxReconnectAttempts;
        AutoReconnectOnFailure = connection.AutoReconnectOnFailure;
        ShowNotificationOnDisconnect = connection.ShowNotificationOnDisconnect;
        ShowNotificationOnReconnect = connection.ShowNotificationOnReconnect;
        PlayNotificationSound = connection.PlayNotificationSound;

        // Load Performance Tab settings
        var performance = settingsService.Performance;
        SelectedVideoQuality = performance.VideoQuality;
        HardwareAcceleration = performance.HardwareAcceleration;
        LowLatencyMode = performance.LowLatencyMode;
        BufferDurationMs = performance.BufferDurationMs;
        SelectedRtspTransport = performance.RtspTransport;
        MaxLatencyMs = performance.MaxLatencyMs;

        // Load Recording Tab settings
        var recording = settingsService.Recording;
        RecordingPath = !string.IsNullOrEmpty(recording.RecordingPath)
            ? new DirectoryInfo(recording.RecordingPath)
            : null;
        SelectedRecordingFormat = recording.RecordingFormat;
        EnableRecordingOnMotion = recording.EnableRecordingOnMotion;

        // Load Advanced Tab settings
        var advanced = settingsService.Advanced;
        EnableDebugLogging = advanced.EnableDebugLogging;
        LogFilePath = !string.IsNullOrEmpty(advanced.LogFilePath)
            ? new FileInfo(advanced.LogFilePath)
            : null;
    }

    private void SaveGeneralSettings()
    {
        // Get current theme/accent from ThemeManager (since selectors update it directly)
        var currentTheme = ThemeManager.Current.DetectTheme(Application.Current);
        var themeBase = currentTheme?.BaseColorScheme ?? "Dark";
        var themeAccent = currentTheme?.ColorScheme ?? "Blue";

        // Get language from CultureManager (since LabelLanguageSelector updates it directly via UpdateUiCultureOnChangeEvent)
        var language = CultureManager.UiCulture.LCID.ToString(CultureInfo.InvariantCulture);

        var settings = new GeneralSettings
        {
            ThemeBase = themeBase,
            ThemeAccent = themeAccent,
            Language = language,
            ConnectCamerasOnStartup = ConnectCamerasOnStartup,
            StartMaximized = StartMaximized,
            StartRibbonCollapsed = StartRibbonCollapsed,
        };

        settingsService.SaveGeneral(settings);
    }

    private void SaveCameraDisplaySettings()
    {
        _ = double.TryParse(SelectedOverlayOpacity, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity);
        _ = Enum.TryParse<OverlayPosition>(SelectedOverlayPosition, out var overlayPosition);

        var settings = new CameraDisplayAppSettings
        {
            ShowOverlayTitle = ShowCameraOverlayTitle,
            ShowOverlayDescription = ShowCameraOverlayDescription,
            ShowOverlayTime = ShowCameraOverlayTime,
            ShowOverlayConnectionStatus = ShowCameraOverlayConnectionStatus,
            OverlayOpacity = opacity,
            OverlayPosition = overlayPosition,
            AllowDragAndDropReorder = AllowDragAndDropReorder,
            AutoSaveLayoutChanges = AutoSaveLayoutChanges,
            SnapshotDirectory = SnapshotDirectory?.FullName,
        };

        settingsService.SaveCameraDisplay(settings);
    }

    private void SaveConnectionSettings()
    {
        _ = Enum.TryParse<CameraProtocol>(SelectedDefaultProtocol, out var protocol);

        var settings = new ConnectionAppSettings
        {
            DefaultProtocol = protocol,
            DefaultPort = DefaultPort,
            ConnectionTimeoutSeconds = ConnectionTimeoutSeconds,
            ReconnectDelaySeconds = ReconnectDelaySeconds,
            MaxReconnectAttempts = MaxReconnectAttempts,
            AutoReconnectOnFailure = AutoReconnectOnFailure,
            ShowNotificationOnDisconnect = ShowNotificationOnDisconnect,
            ShowNotificationOnReconnect = ShowNotificationOnReconnect,
            PlayNotificationSound = PlayNotificationSound,
        };

        settingsService.SaveConnection(settings);
    }

    private void SavePerformanceSettings()
    {
        var settings = new PerformanceSettings
        {
            VideoQuality = SelectedVideoQuality,
            HardwareAcceleration = HardwareAcceleration,
            LowLatencyMode = LowLatencyMode,
            BufferDurationMs = BufferDurationMs,
            RtspTransport = SelectedRtspTransport,
            MaxLatencyMs = MaxLatencyMs,
        };

        settingsService.SavePerformance(settings);
    }

    private void SaveRecordingSettings()
    {
        var settings = new RecordingSettings
        {
            RecordingPath = RecordingPath?.FullName,
            RecordingFormat = SelectedRecordingFormat,
            EnableRecordingOnMotion = EnableRecordingOnMotion,
        };

        settingsService.SaveRecording(settings);
    }

    private void SaveAdvancedSettings()
    {
        var settings = new AdvancedSettings
        {
            EnableDebugLogging = EnableDebugLogging,
            LogFilePath = LogFilePath?.FullName,
        };

        settingsService.SaveAdvanced(settings);
    }

    private void RestoreOriginalThemeAndLanguage()
    {
        // Restore theme
        ThemeManager.Current.ChangeThemeBaseColor(Application.Current, originalThemeBase);
        ThemeManager.Current.ChangeThemeColorScheme(Application.Current, originalThemeAccent);

        // Restore language
        if (int.TryParse(originalLanguage, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lcid))
        {
            CultureManager.UiCulture = new CultureInfo(lcid);
        }
    }

    #endregion
}