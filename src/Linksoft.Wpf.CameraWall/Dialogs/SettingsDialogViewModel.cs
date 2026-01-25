namespace Linksoft.Wpf.CameraWall.Dialogs;

/// <summary>
/// View model for the settings dialog.
/// </summary>
[SuppressMessage("", "SA1124: Do not use regions", Justification = "OK")]
[SuppressMessage("", "S2325:Make properties static", Justification = "XAML binding requires instance properties")]
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
    private string selectedOverlayOpacity = DropDownItemsFactory.DefaultOverlayOpacity;

    public IDictionary<string, string> OverlayOpacityItems
        => DropDownItemsFactory.OverlayOpacityItems;

    [ObservableProperty]
    private string selectedOverlayPosition = DropDownItemsFactory.DefaultOverlayPosition;

    public IDictionary<string, string> OverlayPositionItems
        => DropDownItemsFactory.OverlayPositionItems;

    [ObservableProperty]
    private bool allowDragAndDropReorder = true;

    [ObservableProperty]
    private bool autoSaveLayoutChanges = true;

    [ObservableProperty]
    private DirectoryInfo? snapshotPath;

    #endregion

    #region Connection Tab Settings

    [ObservableProperty]
    private string selectedDefaultProtocol = DropDownItemsFactory.DefaultProtocol;

    public IDictionary<string, string> DefaultProtocolItems
        => DropDownItemsFactory.ProtocolItems;

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
    private string selectedVideoQuality = DropDownItemsFactory.DefaultVideoQuality;

    public IDictionary<string, string> VideoQualityItems
        => DropDownItemsFactory.VideoQualityItems;

    [ObservableProperty]
    private bool hardwareAcceleration = true;

    [ObservableProperty]
    private bool lowLatencyMode;

    [ObservableProperty]
    private int bufferDurationMs = 500;

    [ObservableProperty]
    private string selectedRtspTransport = DropDownItemsFactory.DefaultRtspTransport;

    public IDictionary<string, string> RtspTransportItems
        => DropDownItemsFactory.RtspTransportItems;

    [ObservableProperty]
    private int maxLatencyMs = 500;

    #endregion

    #region Recording Tab Settings

    [ObservableProperty]
    private DirectoryInfo? recordingPath;

    [ObservableProperty]
    private string selectedRecordingFormat = DropDownItemsFactory.DefaultRecordingFormat;

    public IDictionary<string, string> RecordingFormatItems
        => DropDownItemsFactory.RecordingFormatItems;

    [ObservableProperty]
    private bool enableRecordingOnMotion;

    [ObservableProperty]
    private bool enableRecordingOnConnect;

    // Motion Detection Settings
    [ObservableProperty]
    private int motionSensitivity = DropDownItemsFactory.DefaultMotionSensitivity;

    [ObservableProperty]
    private int postMotionDurationSeconds = DropDownItemsFactory.DefaultPostMotionDuration;

    [ObservableProperty]
    private int analysisFrameRate = 2;

    [ObservableProperty]
    private int cooldownSeconds = 5;

    // Recording Segmentation Settings
    [ObservableProperty]
    private bool enableHourlySegmentation = true;

    [ObservableProperty]
    private string selectedMaxRecordingDuration = DropDownItemsFactory.DefaultMaxRecordingDuration.ToString(CultureInfo.InvariantCulture);

    public IDictionary<string, string> MaxRecordingDurationItems
        => DropDownItemsFactory.MaxRecordingDurationItems;

    // Media Cleanup Settings
    [ObservableProperty(DependentPropertyNames = [nameof(IsCleanupEnabled), nameof(IsSnapshotRetentionEnabled)])]
    private string selectedCleanupSchedule = DropDownItemsFactory.DefaultMediaCleanupSchedule;

    public IDictionary<string, string> CleanupScheduleItems
        => DropDownItemsFactory.MediaCleanupScheduleItems;

    [ObservableProperty]
    private string selectedRecordingRetention = DropDownItemsFactory.DefaultRecordingRetentionDays.ToString(CultureInfo.InvariantCulture);

    public IDictionary<string, string> RecordingRetentionItems
        => DropDownItemsFactory.MediaRetentionPeriodItems;

    [ObservableProperty(DependentPropertyNames = [nameof(IsSnapshotRetentionEnabled)])]
    private bool includeSnapshotsInCleanup;

    [ObservableProperty]
    private string selectedSnapshotRetention = DropDownItemsFactory.DefaultSnapshotRetentionDays.ToString(CultureInfo.InvariantCulture);

    public IDictionary<string, string> SnapshotRetentionItems
        => DropDownItemsFactory.MediaRetentionPeriodItems;

    /// <summary>
    /// Gets a value indicating whether cleanup is enabled (schedule is not Disabled).
    /// </summary>
    public bool IsCleanupEnabled
        => !string.Equals(SelectedCleanupSchedule, "Disabled", StringComparison.Ordinal);

    /// <summary>
    /// Gets a value indicating whether snapshot retention dropdown should be enabled.
    /// </summary>
    public bool IsSnapshotRetentionEnabled
        => IsCleanupEnabled && IncludeSnapshotsInCleanup;

    // Playback Overlay Settings
    [ObservableProperty]
    private bool showPlaybackFilename = true;

    [ObservableProperty]
    private string selectedPlaybackFilenameColor = "White";

    [ObservableProperty]
    private bool showPlaybackTimestamp = true;

    [ObservableProperty]
    private string selectedPlaybackTimestampColor = "White";

    #endregion

    #region Advanced Tab Settings

    [ObservableProperty]
    private bool enableDebugLogging;

    [ObservableProperty]
    private DirectoryInfo? logPath;

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
        SnapshotPath = new DirectoryInfo(ApplicationPaths.DefaultSnapshotsPath);

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
        RecordingPath = new DirectoryInfo(ApplicationPaths.DefaultRecordingsPath);
        SelectedRecordingFormat = "mp4";
        EnableRecordingOnMotion = false;
        EnableRecordingOnConnect = false;
        MotionSensitivity = DropDownItemsFactory.DefaultMotionSensitivity;
        PostMotionDurationSeconds = DropDownItemsFactory.DefaultPostMotionDuration;
        AnalysisFrameRate = 2;
        CooldownSeconds = 5;

        // Recording Segmentation
        EnableHourlySegmentation = true;
        SelectedMaxRecordingDuration = DropDownItemsFactory.DefaultMaxRecordingDuration.ToString(CultureInfo.InvariantCulture);

        // Media Cleanup
        SelectedCleanupSchedule = DropDownItemsFactory.DefaultMediaCleanupSchedule;
        SelectedRecordingRetention = DropDownItemsFactory.DefaultRecordingRetentionDays.ToString(CultureInfo.InvariantCulture);
        IncludeSnapshotsInCleanup = false;
        SelectedSnapshotRetention = DropDownItemsFactory.DefaultSnapshotRetentionDays.ToString(CultureInfo.InvariantCulture);

        // Playback Overlay
        ShowPlaybackFilename = true;
        SelectedPlaybackFilenameColor = "White";
        ShowPlaybackTimestamp = true;
        SelectedPlaybackTimestampColor = "White";

        // Advanced Tab
        EnableDebugLogging = false;
        LogPath = new DirectoryInfo(ApplicationPaths.DefaultLogsPath);
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
        SnapshotPath = new DirectoryInfo(cameraDisplay.SnapshotPath);

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
        RecordingPath = new DirectoryInfo(recording.RecordingPath);
        SelectedRecordingFormat = recording.RecordingFormat;
        EnableRecordingOnMotion = recording.EnableRecordingOnMotion;
        EnableRecordingOnConnect = recording.EnableRecordingOnConnect;

        // Motion Detection Settings
        MotionSensitivity = recording.MotionDetection.Sensitivity;
        PostMotionDurationSeconds = recording.MotionDetection.PostMotionDurationSeconds;
        AnalysisFrameRate = recording.MotionDetection.AnalysisFrameRate;
        CooldownSeconds = recording.MotionDetection.CooldownSeconds;

        // Recording Segmentation Settings
        EnableHourlySegmentation = recording.EnableHourlySegmentation;
        SelectedMaxRecordingDuration = recording.MaxRecordingDurationMinutes.ToString(CultureInfo.InvariantCulture);

        // Media Cleanup Settings
        SelectedCleanupSchedule = recording.Cleanup.Schedule.ToString();
        SelectedRecordingRetention = recording.Cleanup.RecordingRetentionDays.ToString(CultureInfo.InvariantCulture);
        IncludeSnapshotsInCleanup = recording.Cleanup.IncludeSnapshots;
        SelectedSnapshotRetention = recording.Cleanup.SnapshotRetentionDays.ToString(CultureInfo.InvariantCulture);

        // Playback Overlay Settings
        ShowPlaybackFilename = recording.PlaybackOverlay.ShowFilename;
        SelectedPlaybackFilenameColor = recording.PlaybackOverlay.FilenameColor;
        ShowPlaybackTimestamp = recording.PlaybackOverlay.ShowTimestamp;
        SelectedPlaybackTimestampColor = recording.PlaybackOverlay.TimestampColor;

        // Load Advanced Tab settings
        var advanced = settingsService.Advanced;
        EnableDebugLogging = advanced.EnableDebugLogging;
        LogPath = new DirectoryInfo(advanced.LogPath);
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
            SnapshotPath = SnapshotPath?.FullName ?? ApplicationPaths.DefaultSnapshotsPath,
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
        _ = Enum.TryParse<MediaCleanupSchedule>(SelectedCleanupSchedule, out var cleanupSchedule);
        _ = int.TryParse(SelectedRecordingRetention, NumberStyles.Integer, CultureInfo.InvariantCulture, out var recordingRetention);
        _ = int.TryParse(SelectedSnapshotRetention, NumberStyles.Integer, CultureInfo.InvariantCulture, out var snapshotRetention);
        _ = int.TryParse(SelectedMaxRecordingDuration, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxRecordingDuration);

        var settings = new RecordingSettings
        {
            RecordingPath = RecordingPath?.FullName ?? ApplicationPaths.DefaultRecordingsPath,
            RecordingFormat = SelectedRecordingFormat,
            EnableRecordingOnMotion = EnableRecordingOnMotion,
            EnableRecordingOnConnect = EnableRecordingOnConnect,
            MotionDetection = new MotionDetectionSettings
            {
                Sensitivity = MotionSensitivity,
                PostMotionDurationSeconds = PostMotionDurationSeconds,
                AnalysisFrameRate = AnalysisFrameRate,
                CooldownSeconds = CooldownSeconds,
            },
            EnableHourlySegmentation = EnableHourlySegmentation,
            MaxRecordingDurationMinutes = maxRecordingDuration > 0 ? maxRecordingDuration : DropDownItemsFactory.DefaultMaxRecordingDuration,
            Cleanup = new MediaCleanupSettings
            {
                Schedule = cleanupSchedule,
                RecordingRetentionDays = recordingRetention > 0 ? recordingRetention : DropDownItemsFactory.DefaultRecordingRetentionDays,
                IncludeSnapshots = IncludeSnapshotsInCleanup,
                SnapshotRetentionDays = snapshotRetention > 0 ? snapshotRetention : DropDownItemsFactory.DefaultSnapshotRetentionDays,
            },
            PlaybackOverlay = new PlaybackOverlaySettings
            {
                ShowFilename = ShowPlaybackFilename,
                FilenameColor = SelectedPlaybackFilenameColor,
                ShowTimestamp = ShowPlaybackTimestamp,
                TimestampColor = SelectedPlaybackTimestampColor,
            },
        };

        settingsService.SaveRecording(settings);
    }

    private void SaveAdvancedSettings()
    {
        var settings = new AdvancedSettings
        {
            EnableDebugLogging = EnableDebugLogging,
            LogPath = LogPath?.FullName ?? ApplicationPaths.DefaultLogsPath,
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