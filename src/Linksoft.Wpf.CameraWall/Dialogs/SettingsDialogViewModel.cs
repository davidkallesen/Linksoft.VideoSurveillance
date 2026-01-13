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
    private bool allowDragAndDropReorder = true;

    [ObservableProperty]
    private bool autoSaveLayoutChanges = true;

    [ObservableProperty]
    private DirectoryInfo? snapshotDirectory;

    #endregion

    #region Connection Tab Settings

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

    #endregion

    #region Recording Tab Settings

    [ObservableProperty]
    private FileInfo? recordingPath;

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
        SaveDisplaySettings();
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
        AllowDragAndDropReorder = true;
        AutoSaveLayoutChanges = true;
        SnapshotDirectory = null;

        // Connection Tab
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

        // Load Display Tab settings
        var display = settingsService.Display;
        ShowCameraOverlayTitle = display.ShowOverlayTitle;
        ShowCameraOverlayDescription = display.ShowOverlayDescription;
        ShowCameraOverlayTime = display.ShowOverlayTime;
        ShowCameraOverlayConnectionStatus = display.ShowOverlayConnectionStatus;
        SelectedOverlayOpacity = display.OverlayOpacity.ToString("F1", CultureInfo.InvariantCulture);
        AllowDragAndDropReorder = display.AllowDragAndDropReorder;
        AutoSaveLayoutChanges = display.AutoSaveLayoutChanges;
        SnapshotDirectory = !string.IsNullOrEmpty(display.SnapshotDirectory)
            ? new DirectoryInfo(display.SnapshotDirectory)
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

    private void SaveDisplaySettings()
    {
        _ = double.TryParse(SelectedOverlayOpacity, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity);

        var settings = new DisplaySettings
        {
            ShowOverlayTitle = ShowCameraOverlayTitle,
            ShowOverlayDescription = ShowCameraOverlayDescription,
            ShowOverlayTime = ShowCameraOverlayTime,
            ShowOverlayConnectionStatus = ShowCameraOverlayConnectionStatus,
            OverlayOpacity = opacity,
            AllowDragAndDropReorder = AllowDragAndDropReorder,
            AutoSaveLayoutChanges = AutoSaveLayoutChanges,
            SnapshotDirectory = SnapshotDirectory?.FullName,
        };

        settingsService.SaveDisplay(settings);
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