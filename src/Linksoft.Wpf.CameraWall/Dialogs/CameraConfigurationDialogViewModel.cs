// ReSharper disable InvertIf
namespace Linksoft.Wpf.CameraWall.Dialogs;

[SuppressMessage("", "S2325:Make properties static", Justification = "XAML binding requires instance properties")]
public partial class CameraConfigurationDialogViewModel : ViewModelDialogBase
{
    private readonly CameraConfiguration originalCamera;
    private readonly bool isNew;
    private readonly IReadOnlyCollection<(string IpAddress, string? Path)> existingEndpoints;
    private readonly IApplicationSettingsService settingsService;

    [ObservableProperty(AfterChangedCallback = nameof(OnIsTestingChanged))]
    private bool isTesting;

    private string? testResultInternal;

    public string TestResult => testResultInternal ?? Translations.NotTested;

    private void ClearTestResult()
    {
        if (testResultInternal is not null)
        {
            testResultInternal = null;
            OnPropertyChanged(nameof(TestResult));
        }
    }

    private void SetTestResult(string value)
    {
        if (testResultInternal != value)
        {
            testResultInternal = value;
            OnPropertyChanged(nameof(TestResult));
        }
    }

    [ObservableProperty]
    private string? ipAddressError;

    public CameraConfigurationDialogViewModel(
        CameraConfiguration camera,
        bool isNew,
        IReadOnlyCollection<(string IpAddress, string? Path)> existingEndpoints,
        IApplicationSettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(settingsService);

        originalCamera = camera;
        this.isNew = isNew;
        this.existingEndpoints = existingEndpoints ?? [];
        this.settingsService = settingsService;

        // Create a clone for editing - changes only apply when Save is clicked
        Camera = camera.Clone();

        Camera.PropertyChanged += OnCameraPropertyChanged;
        NetworkScanner.EntrySelected += OnNetworkScannerEntrySelected;
    }

    private static void OnIsTestingChanged()
        => CommandManager.InvalidateRequerySuggested();

    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    public CameraConfiguration Camera { get; }

    public string DialogTitle
        => isNew
            ? Translations.AddCamera
            : Translations.EditCamera;

    /// <summary>
    /// Gets a value indicating whether this is a new camera being added.
    /// </summary>
    public bool IsNew => isNew;

    /// <summary>
    /// Gets a value indicating whether an existing camera is being edited.
    /// When editing, connection settings (IP, port, protocol) are read-only.
    /// </summary>
    public bool IsEditing => !isNew;

    /// <summary>
    /// Gets a value indicating whether connection settings can be modified.
    /// Connection settings are only editable for new cameras.
    /// </summary>
    public bool CanEditConnectionSettings => isNew;

    public IDictionary<string, string> ProtocolItems { get; } = Enum<CameraProtocol>.ToDictionaryWithStringKey();

    public string SelectedProtocolKey
    {
        get => Camera.Connection.Protocol.ToString();
        set
        {
            if (Enum.TryParse<CameraProtocol>(value, out var protocol))
            {
                Camera.Connection.Protocol = protocol;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> OverlayPositionItems
        => DropDownItemsFactory.OverlayPositionItems;

    public string SelectedOverlayPositionKey
    {
        get => Camera.Display.OverlayPosition.ToString();
        set
        {
            if (Enum.TryParse<OverlayPosition>(value, out var position))
            {
                Camera.Display.OverlayPosition = position;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> RtspTransportItems
        => DropDownItemsFactory.RtspTransportItems;

    public IDictionary<string, string> VideoQualityItems
        => DropDownItemsFactory.VideoQualityItems;

    public IDictionary<string, string> RecordingFormatItems
        => DropDownItemsFactory.RecordingFormatItems;

    public IDictionary<string, string> OpacityItems
        => DropDownItemsFactory.OverlayOpacityItems;

    public string SelectedRtspTransportKey
    {
        get => Camera.Stream.RtspTransport;
        set
        {
            Camera.Stream.RtspTransport = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayTitle
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayTitle is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.CameraDisplay.ShowOverlayTitle = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.CameraDisplay.ShowOverlayTitle = settingsService.CameraDisplay.ShowOverlayTitle;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayTitle));
        }
    }

    public bool OverrideShowOverlayTitle
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayTitle ?? settingsService.CameraDisplay.ShowOverlayTitle;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.CameraDisplay.ShowOverlayTitle = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayDescription
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayDescription is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.CameraDisplay.ShowOverlayDescription = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.CameraDisplay.ShowOverlayDescription = settingsService.CameraDisplay.ShowOverlayDescription;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayDescription));
        }
    }

    public bool OverrideShowOverlayDescription
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayDescription ?? settingsService.CameraDisplay.ShowOverlayDescription;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.CameraDisplay.ShowOverlayDescription = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayTime
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayTime is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.CameraDisplay.ShowOverlayTime = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.CameraDisplay.ShowOverlayTime = settingsService.CameraDisplay.ShowOverlayTime;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayTime));
        }
    }

    public bool OverrideShowOverlayTime
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayTime ?? settingsService.CameraDisplay.ShowOverlayTime;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.CameraDisplay.ShowOverlayTime = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayConnectionStatus
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayConnectionStatus is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.CameraDisplay.ShowOverlayConnectionStatus = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.CameraDisplay.ShowOverlayConnectionStatus = settingsService.CameraDisplay.ShowOverlayConnectionStatus;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayConnectionStatus));
        }
    }

    public bool OverrideShowOverlayConnectionStatus
    {
        get => Camera.Overrides?.CameraDisplay.ShowOverlayConnectionStatus ?? settingsService.CameraDisplay.ShowOverlayConnectionStatus;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.CameraDisplay.ShowOverlayConnectionStatus = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultOverlayOpacity
    {
        get => Camera.Overrides?.CameraDisplay.OverlayOpacity is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.CameraDisplay.OverlayOpacity = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.CameraDisplay.OverlayOpacity = settingsService.CameraDisplay.OverlayOpacity;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideOverlayOpacity));
        }
    }

    public string OverrideOverlayOpacity
    {
        get
        {
            var opacity = Camera.Overrides?.CameraDisplay.OverlayOpacity ?? settingsService.CameraDisplay.OverlayOpacity;
            return opacity.ToString("F1", CultureInfo.InvariantCulture);
        }

        set
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity))
            {
                EnsureOverrides();
                Camera.Overrides!.CameraDisplay.OverlayOpacity = opacity;
                RaisePropertyChanged();
            }
        }
    }

    public bool UseDefaultConnectionTimeout
    {
        get => Camera.Overrides?.Connection.ConnectionTimeoutSeconds is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Connection.ConnectionTimeoutSeconds = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Connection.ConnectionTimeoutSeconds = settingsService.Connection.ConnectionTimeoutSeconds;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideConnectionTimeout));
        }
    }

    public int OverrideConnectionTimeout
    {
        get => Camera.Overrides?.Connection.ConnectionTimeoutSeconds ?? settingsService.Connection.ConnectionTimeoutSeconds;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Connection.ConnectionTimeoutSeconds = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultReconnectDelay
    {
        get => Camera.Overrides?.Connection.ReconnectDelaySeconds is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Connection.ReconnectDelaySeconds = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Connection.ReconnectDelaySeconds = settingsService.Connection.ReconnectDelaySeconds;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideReconnectDelay));
        }
    }

    public int OverrideReconnectDelay
    {
        get => Camera.Overrides?.Connection.ReconnectDelaySeconds ?? settingsService.Connection.ReconnectDelaySeconds;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Connection.ReconnectDelaySeconds = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultAutoReconnect
    {
        get => Camera.Overrides?.Connection.AutoReconnectOnFailure is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Connection.AutoReconnectOnFailure = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Connection.AutoReconnectOnFailure = settingsService.Connection.AutoReconnectOnFailure;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideAutoReconnect));
        }
    }

    public bool OverrideAutoReconnect
    {
        get => Camera.Overrides?.Connection.AutoReconnectOnFailure ?? settingsService.Connection.AutoReconnectOnFailure;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Connection.AutoReconnectOnFailure = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultVideoQuality
    {
        get => Camera.Overrides?.Performance.VideoQuality is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Performance.VideoQuality = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Performance.VideoQuality = settingsService.Performance.VideoQuality;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideVideoQuality));
        }
    }

    public string OverrideVideoQuality
    {
        get => Camera.Overrides?.Performance.VideoQuality ?? settingsService.Performance.VideoQuality;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Performance.VideoQuality = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultHardwareAcceleration
    {
        get => Camera.Overrides?.Performance.HardwareAcceleration is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Performance.HardwareAcceleration = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Performance.HardwareAcceleration = settingsService.Performance.HardwareAcceleration;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideHardwareAcceleration));
        }
    }

    public bool OverrideHardwareAcceleration
    {
        get => Camera.Overrides?.Performance.HardwareAcceleration ?? settingsService.Performance.HardwareAcceleration;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Performance.HardwareAcceleration = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingPath
    {
        get => Camera.Overrides?.Recording.RecordingPath is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.RecordingPath = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.RecordingPath = settingsService.Recording.RecordingPath ?? string.Empty;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingPath));
        }
    }

    public DirectoryInfo? OverrideRecordingPath
    {
        get
        {
            var path = Camera.Overrides?.Recording.RecordingPath ?? settingsService.Recording.RecordingPath;
            return string.IsNullOrEmpty(path) ? null : new DirectoryInfo(path);
        }

        set
        {
            EnsureOverrides();
            Camera.Overrides!.Recording.RecordingPath = value?.FullName;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingFormat
    {
        get => Camera.Overrides?.Recording.RecordingFormat is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.RecordingFormat = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.RecordingFormat = settingsService.Recording.RecordingFormat;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingFormat));
        }
    }

    public string OverrideRecordingFormat
    {
        get => Camera.Overrides?.Recording.RecordingFormat ?? settingsService.Recording.RecordingFormat;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Recording.RecordingFormat = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingOnMotion
    {
        get => Camera.Overrides?.Recording.EnableRecordingOnMotion is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.EnableRecordingOnMotion = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.EnableRecordingOnMotion = settingsService.Recording.EnableRecordingOnMotion;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingOnMotion));
        }
    }

    public bool OverrideRecordingOnMotion
    {
        get => Camera.Overrides?.Recording.EnableRecordingOnMotion ?? settingsService.Recording.EnableRecordingOnMotion;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Recording.EnableRecordingOnMotion = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultMotionSensitivity
    {
        get => Camera.Overrides?.MotionDetection.Sensitivity is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.Sensitivity = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.Sensitivity = settingsService.MotionDetection.Sensitivity;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideMotionSensitivity));
        }
    }

    public int OverrideMotionSensitivity
    {
        get => Camera.Overrides?.MotionDetection.Sensitivity ?? settingsService.MotionDetection.Sensitivity;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.Sensitivity = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultPostMotionDuration
    {
        get => Camera.Overrides?.MotionDetection.PostMotionDurationSeconds is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.PostMotionDurationSeconds = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.PostMotionDurationSeconds = settingsService.MotionDetection.PostMotionDurationSeconds;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverridePostMotionDuration));
        }
    }

    public int OverridePostMotionDuration
    {
        get => Camera.Overrides?.MotionDetection.PostMotionDurationSeconds ?? settingsService.MotionDetection.PostMotionDurationSeconds;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.PostMotionDurationSeconds = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingOnConnect
    {
        get => Camera.Overrides?.Recording.EnableRecordingOnConnect is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.EnableRecordingOnConnect = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.EnableRecordingOnConnect = settingsService.Recording.EnableRecordingOnConnect;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingOnConnect));
        }
    }

    public bool OverrideRecordingOnConnect
    {
        get => Camera.Overrides?.Recording.EnableRecordingOnConnect ?? settingsService.Recording.EnableRecordingOnConnect;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Recording.EnableRecordingOnConnect = value;
            RaisePropertyChanged();
        }
    }

    // Motion Detection Overrides
    public bool UseDefaultMinimumChangePercent
    {
        get => Camera.Overrides?.MotionDetection.MinimumChangePercent is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.MinimumChangePercent = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.MinimumChangePercent = settingsService.MotionDetection.MinimumChangePercent;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideMinimumChangePercent));
        }
    }

    public double OverrideMinimumChangePercent
    {
        get => Camera.Overrides?.MotionDetection.MinimumChangePercent ?? settingsService.MotionDetection.MinimumChangePercent;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.MinimumChangePercent = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultAnalysisFrameRate
    {
        get => Camera.Overrides?.MotionDetection.AnalysisFrameRate is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.AnalysisFrameRate = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.AnalysisFrameRate = settingsService.MotionDetection.AnalysisFrameRate;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideAnalysisFrameRate));
        }
    }

    public int OverrideAnalysisFrameRate
    {
        get => Camera.Overrides?.MotionDetection.AnalysisFrameRate ?? settingsService.MotionDetection.AnalysisFrameRate;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.AnalysisFrameRate = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultCooldownSeconds
    {
        get => Camera.Overrides?.MotionDetection.CooldownSeconds is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.CooldownSeconds = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.CooldownSeconds = settingsService.MotionDetection.CooldownSeconds;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideCooldownSeconds));
        }
    }

    public int OverrideCooldownSeconds
    {
        get => Camera.Overrides?.MotionDetection.CooldownSeconds ?? settingsService.MotionDetection.CooldownSeconds;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.CooldownSeconds = value;
            RaisePropertyChanged();
        }
    }

    // Bounding Box Overrides
    public bool UseDefaultShowBoundingBoxInGrid
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.ShowInGrid is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.BoundingBox.ShowInGrid = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.ShowInGrid = settingsService.MotionDetection.BoundingBox.ShowInGrid;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowBoundingBoxInGrid));
        }
    }

    public bool OverrideShowBoundingBoxInGrid
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.ShowInGrid ?? settingsService.MotionDetection.BoundingBox.ShowInGrid;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.BoundingBox.ShowInGrid = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowBoundingBoxInFullScreen
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.ShowInFullScreen is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.BoundingBox.ShowInFullScreen = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.ShowInFullScreen = settingsService.MotionDetection.BoundingBox.ShowInFullScreen;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowBoundingBoxInFullScreen));
        }
    }

    public bool OverrideShowBoundingBoxInFullScreen
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.ShowInFullScreen ?? settingsService.MotionDetection.BoundingBox.ShowInFullScreen;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.BoundingBox.ShowInFullScreen = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultBoundingBoxColor
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.Color is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.BoundingBox.Color = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.Color = settingsService.MotionDetection.BoundingBox.Color;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideBoundingBoxColor));
        }
    }

    public string OverrideBoundingBoxColor
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.Color ?? settingsService.MotionDetection.BoundingBox.Color;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.BoundingBox.Color = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultBoundingBoxThickness
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.Thickness is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.BoundingBox.Thickness = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.Thickness = settingsService.MotionDetection.BoundingBox.Thickness;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideBoundingBoxThickness));
        }
    }

    public string OverrideBoundingBoxThickness
    {
        get => (Camera.Overrides?.MotionDetection.BoundingBox.Thickness ?? settingsService.MotionDetection.BoundingBox.Thickness).ToString(CultureInfo.InvariantCulture);
        set
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var thickness))
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.Thickness = thickness;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> BoundingBoxThicknessItems
        => DropDownItemsFactory.BoundingBoxThicknessItems;

    public bool UseDefaultBoundingBoxMinArea
    {
        get => Camera.Overrides?.MotionDetection.BoundingBox.MinArea is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.BoundingBox.MinArea = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.MinArea = settingsService.MotionDetection.BoundingBox.MinArea;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideBoundingBoxMinArea));
        }
    }

    public string OverrideBoundingBoxMinArea
    {
        get => (Camera.Overrides?.MotionDetection.BoundingBox.MinArea ?? settingsService.MotionDetection.BoundingBox.MinArea).ToString(CultureInfo.InvariantCulture);
        set
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minArea))
            {
                EnsureOverrides();
                Camera.Overrides!.MotionDetection.BoundingBox.MinArea = minArea;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> BoundingBoxMinAreaItems
        => DropDownItemsFactory.BoundingBoxMinAreaItems;

    public IDictionary<string, string> AnalysisResolutionItems
        => DropDownItemsFactory.MotionAnalysisResolutionItems;

    public IDictionary<string, string> ThumbnailTileCountItems
        => DropDownItemsFactory.ThumbnailTileCountItems;

    public IDictionary<string, string> TimelapseIntervalItems
        => DropDownItemsFactory.TimelapseIntervalItems;

    public bool UseDefaultThumbnailTileCount
    {
        get => Camera.Overrides?.Recording.ThumbnailTileCount is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.ThumbnailTileCount = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.ThumbnailTileCount = settingsService.Recording.ThumbnailTileCount;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideThumbnailTileCount));
        }
    }

    public string OverrideThumbnailTileCount
    {
        get => (Camera.Overrides?.Recording.ThumbnailTileCount ?? settingsService.Recording.ThumbnailTileCount).ToString(CultureInfo.InvariantCulture);
        set
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tileCount))
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.ThumbnailTileCount = tileCount;
                RaisePropertyChanged();
            }
        }
    }

    public bool UseDefaultEnableTimelapse
    {
        get => Camera.Overrides?.Recording.EnableTimelapse is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.EnableTimelapse = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.EnableTimelapse = settingsService.Recording.EnableTimelapse;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideEnableTimelapse));
        }
    }

    public bool OverrideEnableTimelapse
    {
        get => Camera.Overrides?.Recording.EnableTimelapse ?? settingsService.Recording.EnableTimelapse;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Recording.EnableTimelapse = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultTimelapseInterval
    {
        get => Camera.Overrides?.Recording.TimelapseInterval is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.Recording.TimelapseInterval = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.Recording.TimelapseInterval = settingsService.Recording.TimelapseInterval;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideTimelapseInterval));
        }
    }

    public string OverrideTimelapseInterval
    {
        get => Camera.Overrides?.Recording.TimelapseInterval ?? settingsService.Recording.TimelapseInterval;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.Recording.TimelapseInterval = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultAnalysisResolution
    {
        get => Camera.Overrides?.MotionDetection.AnalysisWidth is null && Camera.Overrides?.MotionDetection.AnalysisHeight is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MotionDetection.AnalysisWidth = null;
                    Camera.Overrides.MotionDetection.AnalysisHeight = null;
                }
            }
            else
            {
                EnsureOverrides();
                var (width, height) = DropDownItemsFactory.ParseAnalysisResolution(
                    settingsService.MotionDetection.AnalysisWidth > 0 && settingsService.MotionDetection.AnalysisHeight > 0
                        ? DropDownItemsFactory.FormatAnalysisResolution(
                            settingsService.MotionDetection.AnalysisWidth,
                            settingsService.MotionDetection.AnalysisHeight)
                        : DropDownItemsFactory.DefaultMotionAnalysisResolution);
                Camera.Overrides!.MotionDetection.AnalysisWidth = width;
                Camera.Overrides!.MotionDetection.AnalysisHeight = height;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideAnalysisResolution));
        }
    }

    public string OverrideAnalysisResolution
    {
        get
        {
            var width = Camera.Overrides?.MotionDetection.AnalysisWidth ?? settingsService.MotionDetection.AnalysisWidth;
            var height = Camera.Overrides?.MotionDetection.AnalysisHeight ?? settingsService.MotionDetection.AnalysisHeight;
            if (width > 0 && height > 0)
            {
                return DropDownItemsFactory.FormatAnalysisResolution(width, height);
            }

            return DropDownItemsFactory.DefaultMotionAnalysisResolution;
        }

        set
        {
            var (width, height) = DropDownItemsFactory.ParseAnalysisResolution(value);
            EnsureOverrides();
            Camera.Overrides!.MotionDetection.AnalysisWidth = width;
            Camera.Overrides!.MotionDetection.AnalysisHeight = height;
            RaisePropertyChanged();
        }
    }

    private void EnsureOverrides()
    {
        Camera.Overrides ??= new CameraOverrides();
    }

    public NetworkScannerViewModel NetworkScanner { get; } = CreateNetworkScanner();

    private static NetworkScannerViewModel CreateNetworkScanner()
    {
        var scanner = new NetworkScannerViewModel
        {
            // Common camera ports: RTSP, HTTP, HTTP alt, RTSP alt
            PortsNumbers = [554, 80, 8080, 8554],
        };

        var localIpAddress = IPv4AddressHelper.GetLocalAddress();
        if (localIpAddress is not null)
        {
            // Use /24 subnet (255.255.255.0) which is typical for most local networks
            var (startIp, endIp) = IPv4AddressHelper.GetFirstAndLastAddressInRange(localIpAddress, cidrLength: 24);
            scanner.StartIpAddress = startIp.ToString();
            scanner.EndIpAddress = endIp.ToString();
        }

        return scanner;
    }

    private void OnNetworkScannerEntrySelected(
        object? sender,
        NetworkHostSelectedEventArgs e)
    {
        if (e.SelectedHost is null)
        {
            return;
        }

        // Populate IP address
        Camera.Connection.IpAddress = e.SelectedHost.IpAddress.ToString();

        // Set display name from hostname if available and display name is default
        if (!string.IsNullOrWhiteSpace(e.SelectedHost.Hostname) &&
            (string.IsNullOrWhiteSpace(Camera.Display.DisplayName) || Camera.Display.DisplayName == Translations.NewCamera))
        {
            Camera.Display.DisplayName = e.SelectedHost.Hostname;
        }

        // Auto-detect protocol and port from open ports
        var openPorts = e.SelectedHost.OpenPortNumbers.ToList();
        if (openPorts.Contains(554))
        {
            Camera.Connection.Protocol = CameraProtocol.Rtsp;
            Camera.Connection.Port = 554;
        }
        else if (openPorts.Contains(8554))
        {
            Camera.Connection.Protocol = CameraProtocol.Rtsp;
            Camera.Connection.Port = 8554;
        }
        else if (openPorts.Contains(80))
        {
            Camera.Connection.Protocol = CameraProtocol.Http;
            Camera.Connection.Port = 80;
        }
        else if (openPorts.Contains(8080))
        {
            Camera.Connection.Protocol = CameraProtocol.Http;
            Camera.Connection.Port = 8080;
        }
        else if (openPorts.Count > 0)
        {
            // Use first open port
            Camera.Connection.Port = openPorts[0];
        }

        // Notify property changes for combo boxes
        OnPropertyChanged(nameof(SelectedProtocolKey));
    }

    private void OnCameraPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        // Reset test result when connection or authentication properties change
        if (e.PropertyName is nameof(Camera.Connection) or nameof(Camera.Authentication))
        {
            ClearTestResult();
        }

        // Validate IP address + path uniqueness when connection changes
        if (e.PropertyName == nameof(Camera.Connection))
        {
            ValidateCameraEndpoint();
        }

        // Always invalidate commands when any property changes (for Save button CanExecute)
        CommandManager.InvalidateRequerySuggested();
    }

    private void ValidateCameraEndpoint()
    {
        if (string.IsNullOrWhiteSpace(Camera.Connection.IpAddress))
        {
            IpAddressError = null;
            return;
        }

        IpAddressError = !IsCameraEndpointUnique() ? Translations.IpAddressAlreadyExists : null;
    }

    private bool IsCameraEndpointUnique()
    {
        if (string.IsNullOrWhiteSpace(Camera.Connection.IpAddress))
        {
            return true;
        }

        var currentPath = Camera.Connection.Path;

        return !existingEndpoints.Any(e =>
            string.Equals(e.IpAddress, Camera.Connection.IpAddress, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                string.IsNullOrEmpty(e.Path) ? null : e.Path,
                string.IsNullOrEmpty(currentPath) ? null : currentPath,
                StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnection()
    {
        IsTesting = true;
        ClearTestResult();

        try
        {
            var uri = Camera.BuildUri();
            await TestStreamWithPlayerAsync(uri).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetTestResult(string.Format(CultureInfo.CurrentCulture, Translations.FailedWithStatus1, ex.Message));
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task TestStreamWithPlayerAsync(Uri uri)
    {
        using var player = CreateTestPlayer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var tcs = new TaskCompletionSource<bool>();

        SubscribeToPlayerStatus(player, tcs);

        try
        {
            player.Open(uri.ToString());
            await WaitForConnectionResultAsync(tcs, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            player.PropertyChanged -= OnPlayerStatusChanged;
            player.Stop();
        }

        void OnPlayerStatusChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Player.Status))
            {
                return;
            }

            switch (player.Status)
            {
                case Status.Playing:
                    tcs.TrySetResult(true);
                    break;
                case Status.Failed:
                    tcs.TrySetResult(false);
                    break;
            }
        }

        void SubscribeToPlayerStatus(
            Player p,
            TaskCompletionSource<bool> taskSource)
        {
            p.PropertyChanged += OnPlayerStatusChanged;
        }
    }

    private static Player CreateTestPlayer()
    {
        var config = new Config
        {
            Player =
            {
                AutoPlay = true,
            },
            Video =
            {
                BackColor = Colors.Black,
            },
            Audio =
            {
                Enabled = false,
            },
        };

        return new Player(config);
    }

    private async Task WaitForConnectionResultAsync(
        TaskCompletionSource<bool> tcs,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await tcs.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            SetTestResult(success ? Translations.ConnectionSuccessful : Translations.StreamOpenFailed);
        }
        catch (OperationCanceledException)
        {
            SetTestResult(Translations.ConnectionTimedOut);
        }
    }

    private bool CanTestConnection()
        => !string.IsNullOrWhiteSpace(Camera.Connection.IpAddress) && !IsTesting;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        // Copy edited values back to the original camera
        originalCamera.CopyFrom(Camera);

        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    private bool CanSave()
        => !string.IsNullOrWhiteSpace(Camera.Display.DisplayName) &&
           !string.IsNullOrWhiteSpace(Camera.Connection.IpAddress) &&
           IsCameraEndpointUnique() &&
           HasChanges();

    private bool HasChanges()
    {
        // For new cameras, always consider as having changes if fields are filled
        if (isNew)
        {
            return true;
        }

        // For existing cameras, compare with original to detect changes
        return !Camera.ValueEquals(originalCamera);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));
    }
}