// ReSharper disable UnusedMember.Local
// ReSharper disable InvertIf
#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace Linksoft.Wpf.CameraWall.UserControls;

/// <summary>
/// Control for displaying a single camera stream with overlay and context menu.
/// </summary>
public partial class CameraTile : IDisposable
{
    [DependencyProperty(PropertyChangedCallback = nameof(OnCameraChanged))]
    private CameraConfiguration? camera;

    [DependencyProperty(DefaultValue = ConnectionState.Disconnected, PropertyChangedCallback = nameof(OnConnectionStateChanged))]
    private ConnectionState connectionState;

    [DependencyProperty(PropertyChangedCallback = nameof(OnIsSelectedChanged))]
    private bool isSelected;

    [DependencyProperty(PropertyChangedCallback = nameof(OnCameraNameChanged))]
    private string cameraName = string.Empty;

    [DependencyProperty(PropertyChangedCallback = nameof(OnCameraDescriptionChanged))]
    private string cameraDescription = string.Empty;

    [DependencyProperty]
    private Player? player;

    [DependencyProperty(DefaultValue = false)]
    private bool isPlayerLent;

    [DependencyProperty(DefaultValue = HorizontalAlignment.Left)]
    private HorizontalAlignment overlayHorizontalAlignment;

    [DependencyProperty(DefaultValue = VerticalAlignment.Top)]
    private VerticalAlignment overlayVerticalAlignment;

    [DependencyProperty(DefaultValue = nameof(Brushes.Transparent))]
    private Brush selectionBorderBrush = Brushes.Transparent;

    [DependencyProperty(DefaultValue = true, PropertyChangedCallback = nameof(OnOverlaySettingsChanged))]
    private bool showOverlayTitle;

    [DependencyProperty(DefaultValue = true, PropertyChangedCallback = nameof(OnOverlaySettingsChanged))]
    private bool showOverlayDescription;

    [DependencyProperty(DefaultValue = true, PropertyChangedCallback = nameof(OnOverlaySettingsChanged))]
    private bool showOverlayConnectionStatus;

    [DependencyProperty(DefaultValue = false, PropertyChangedCallback = nameof(OnOverlaySettingsChanged))]
    private bool showOverlayTime;

    [DependencyProperty(DefaultValue = 0.6, PropertyChangedCallback = nameof(OnOverlaySettingsChanged))]
    private double overlayOpacity;

    [DependencyProperty]
    private string? snapshotPath;

    [DependencyProperty(DefaultValue = true)]
    private bool autoConnectOnLoad;

    // Connection settings
    [DependencyProperty(DefaultValue = 10)]
    private int connectionTimeoutSeconds;

    [DependencyProperty(DefaultValue = 5)]
    private int reconnectDelaySeconds;

    [DependencyProperty(DefaultValue = 3)]
    private int maxReconnectAttempts;

    [DependencyProperty(DefaultValue = true)]
    private bool autoReconnectOnFailure;

    // Notification settings
    [DependencyProperty(DefaultValue = true)]
    private bool showNotificationOnDisconnect;

    [DependencyProperty(DefaultValue = false)]
    private bool showNotificationOnReconnect;

    [DependencyProperty(DefaultValue = false)]
    private bool playNotificationSound;

    // Performance settings
    [DependencyProperty(DefaultValue = "Auto", PropertyChangedCallback = nameof(OnPerformanceSettingChanged))]
    private string videoQuality = "Auto";

    [DependencyProperty(DefaultValue = true, PropertyChangedCallback = nameof(OnPerformanceSettingChanged))]
    private bool hardwareAcceleration;

    // Recording settings
    [DependencyProperty(DefaultValue = RecordingState.Idle, PropertyChangedCallback = nameof(OnRecordingStateChanged))]
    private RecordingState recordingState;

    [DependencyProperty]
    private TimeSpan recordingDuration;

    [DependencyProperty(DefaultValue = false)]
    private bool isMotionDetected;

    [DependencyProperty(DefaultValue = false, PropertyChangedCallback = nameof(OnRecordingOnMotionSettingChanged))]
    private bool enableRecordingOnMotion;

    [DependencyProperty(DefaultValue = false)]
    private bool enableRecordingOnConnect;

    // Bounding box settings
    [DependencyProperty(DefaultValue = false, PropertyChangedCallback = nameof(OnBoundingBoxSettingsChanged))]
    private bool showBoundingBoxInGrid;

    [DependencyProperty(DefaultValue = "Red", PropertyChangedCallback = nameof(OnBoundingBoxSettingsChanged))]
    private string boundingBoxColor = "Red";

    [DependencyProperty(DefaultValue = 2, PropertyChangedCallback = nameof(OnBoundingBoxSettingsChanged))]
    private int boundingBoxThickness;

    [DependencyProperty(DefaultValue = 0.3, PropertyChangedCallback = nameof(OnBoundingBoxSettingsChanged))]
    private double boundingBoxSmoothing;

    // Motion detection settings
    [DependencyProperty(DefaultValue = 30)]
    private int motionSensitivity;

    [DependencyProperty(DefaultValue = 2.0)]
    private double motionMinimumChangePercent;

    [DependencyProperty(DefaultValue = 2)]
    private int motionAnalysisFrameRate;

    [DependencyProperty(DefaultValue = 10)]
    private int motionPostDurationSeconds;

    [DependencyProperty(DefaultValue = 5)]
    private int motionCooldownSeconds;

    [DependencyProperty(DefaultValue = 100)]
    private int motionBoundingBoxMinArea;

    [DependencyProperty(DefaultValue = 4)]
    private int motionBoundingBoxPadding;

    // Recording services
    private IRecordingService? recordingService;
    private IMotionDetectionService? motionDetectionService;
    private DispatcherTimer? recordingDurationTimer;

    private bool disposed;
    private bool isReconnecting;
    private bool isSwapping;
    private bool isDragging;
    private Point dragStartPoint;
    private ConnectionState previousState = ConnectionState.Disconnected;
    private CameraOverlay? cachedOverlay;
    private MotionBoundingBoxOverlay? cachedMotionOverlay;
    private DispatcherTimer? reconnectTimer;
    private DispatcherTimer? autoReconnectTimer;
    private int currentReconnectAttempt;

    // Track previous performance override values to detect changes that require reconnection
    private string? previousOverrideVideoQuality;
    private bool? previousOverrideHardwareAcceleration;

    // Track previous motion detection override values to detect changes that require restart
    private bool? previousOverrideEnableRecordingOnMotion;
    private bool? previousOverrideShowBoundingBoxInGrid;
    private bool? previousOverrideShowBoundingBoxInFullScreen;
    private string? previousOverrideBoundingBoxColor;
    private int? previousOverrideBoundingBoxThickness;
    private int? previousOverrideBoundingBoxMinArea;
    private int? previousOverrideMotionSensitivity;
    private double? previousOverrideMotionMinimumChangePercent;
    private int? previousOverrideMotionAnalysisFrameRate;
    private int? previousOverrideMotionAnalysisWidth;
    private int? previousOverrideMotionAnalysisHeight;
    private int? previousOverrideMotionCooldownSeconds;
    private int? previousOverridePostMotionDurationSeconds;

    /// <summary>
    /// Gets whether motion detection should run (for recording or bounding box display).
    /// Uses effective values considering per-camera overrides.
    /// </summary>
    private bool ShouldRunMotionDetection =>
        GetEffectiveEnableRecordingOnMotion() || GetEffectiveShowBoundingBoxInGrid();

    /// <summary>
    /// Gets the effective EnableRecordingOnMotion value, considering camera override.
    /// </summary>
    private bool GetEffectiveEnableRecordingOnMotion()
        => Camera?.Overrides?.EnableRecordingOnMotion ?? EnableRecordingOnMotion;

    /// <summary>
    /// Gets the effective ShowBoundingBoxInGrid value, considering camera override.
    /// </summary>
    private bool GetEffectiveShowBoundingBoxInGrid()
        => Camera?.Overrides?.ShowBoundingBoxInGrid ?? ShowBoundingBoxInGrid;

    /// <summary>
    /// Gets the effective ShowBoundingBoxInFullScreen value, considering camera override.
    /// </summary>
    private bool GetEffectiveShowBoundingBoxInFullScreen()
        => Camera?.Overrides?.ShowBoundingBoxInFullScreen ?? false;

    /// <summary>
    /// Gets the effective BoundingBoxColor value, considering camera override.
    /// </summary>
    private string GetEffectiveBoundingBoxColor()
        => Camera?.Overrides?.BoundingBoxColor ?? BoundingBoxColor;

    /// <summary>
    /// Gets the effective BoundingBoxThickness value, considering camera override.
    /// </summary>
    private int GetEffectiveBoundingBoxThickness()
        => Camera?.Overrides?.BoundingBoxThickness ?? BoundingBoxThickness;

    /// <summary>
    /// Gets the effective BoundingBoxSmoothing value, considering camera override.
    /// </summary>
    private double GetEffectiveBoundingBoxSmoothing()
        => Camera?.Overrides?.BoundingBoxSmoothing ?? BoundingBoxSmoothing;

    /// <summary>
    /// Gets the effective BoundingBoxMinArea value, considering camera override.
    /// </summary>
    private int GetEffectiveBoundingBoxMinArea()
        => Camera?.Overrides?.BoundingBoxMinArea ?? MotionBoundingBoxMinArea;

    /// <summary>
    /// Gets the effective BoundingBoxPadding value, considering camera override.
    /// </summary>
    private int GetEffectiveBoundingBoxPadding()
        => Camera?.Overrides?.BoundingBoxPadding ?? MotionBoundingBoxPadding;

    /// <summary>
    /// Gets the effective MotionSensitivity value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionSensitivity()
        => Camera?.Overrides?.MotionSensitivity ?? MotionSensitivity;

    /// <summary>
    /// Gets the effective MotionMinimumChangePercent value, considering camera override.
    /// </summary>
    private double GetEffectiveMotionMinimumChangePercent()
        => Camera?.Overrides?.MotionMinimumChangePercent ?? MotionMinimumChangePercent;

    /// <summary>
    /// Gets the effective MotionAnalysisFrameRate value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionAnalysisFrameRate()
        => Camera?.Overrides?.MotionAnalysisFrameRate ?? MotionAnalysisFrameRate;

    /// <summary>
    /// Gets the effective MotionPostDurationSeconds value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionPostDurationSeconds()
        => Camera?.Overrides?.PostMotionDurationSeconds ?? MotionPostDurationSeconds;

    /// <summary>
    /// Gets the effective MotionCooldownSeconds value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionCooldownSeconds()
        => Camera?.Overrides?.MotionCooldownSeconds ?? MotionCooldownSeconds;

    /// <summary>
    /// Gets the effective motion analysis resolution, considering camera override.
    /// Returns width and height from override if set, otherwise uses service default.
    /// </summary>
    private (int Width, int Height) GetEffectiveMotionAnalysisResolution()
    {
        var overrideWidth = Camera?.Overrides?.MotionAnalysisWidth;
        var overrideHeight = Camera?.Overrides?.MotionAnalysisHeight;

        if (overrideWidth.HasValue && overrideHeight.HasValue && overrideWidth.Value > 0 && overrideHeight.Value > 0)
        {
            return (overrideWidth.Value, overrideHeight.Value);
        }

        // Fall back to service default
        if (motionDetectionService is not null && Camera is not null)
        {
            return motionDetectionService.GetAnalysisResolution(Camera.Id);
        }

        // Last resort: default values
        return (320, 240);
    }

    /// <summary>
    /// Occurs when a full screen request is made.
    /// </summary>
    public event EventHandler<FullScreenRequestedEventArgs>? FullScreenRequested;

    /// <summary>
    /// Occurs when a swap left request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? SwapLeftRequested;

    /// <summary>
    /// Occurs when a swap right request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? SwapRightRequested;

    /// <summary>
    /// Occurs when the connection state changes.
    /// </summary>
    public event EventHandler<CameraConnectionChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Occurs when an edit camera request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? EditCameraRequested;

    /// <summary>
    /// Occurs when a delete camera request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? DeleteCameraRequested;

    /// <summary>
    /// Occurs when a camera is dropped onto this tile.
    /// </summary>
    public event EventHandler<CameraConfiguration>? CameraDropped;

    /// <summary>
    /// Occurs when the recording state changes.
    /// </summary>
    public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChangedEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraTile"/> class.
    /// </summary>
    public CameraTile()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        VideoPlayer.OverlayCreated += OnOverlayCreated;
        VideoPlayer.SurfaceCreated += OnSurfaceCreated;
    }

    /// <summary>
    /// Initializes the recording and motion detection services.
    /// </summary>
    /// <param name="recordingService">The recording service.</param>
    /// <param name="motionDetectionService">The motion detection service.</param>
    public void InitializeServices(
        IRecordingService? recordingService,
        IMotionDetectionService? motionDetectionService)
    {
        // Unsubscribe from previous services
        if (this.recordingService is not null)
        {
            this.recordingService.RecordingStateChanged -= OnServiceRecordingStateChanged;
        }

        if (this.motionDetectionService is not null)
        {
            this.motionDetectionService.MotionDetected -= OnMotionDetected;
        }

        this.recordingService = recordingService;
        this.motionDetectionService = motionDetectionService;

        // Subscribe to new services
        if (this.recordingService is not null)
        {
            this.recordingService.RecordingStateChanged += OnServiceRecordingStateChanged;
        }

        if (this.motionDetectionService is not null)
        {
            this.motionDetectionService.MotionDetected += OnMotionDetected;
        }

        // Start motion detection if camera is already connected and settings require it
        // This handles the race condition where camera connects before services are initialized
        if (ConnectionState == ConnectionState.Connected && ShouldRunMotionDetection)
        {
            StartMotionDetection();
        }
    }

    /// <summary>
    /// Starts playing the camera stream.
    /// </summary>
    public void Play()
    {
        if (Camera is null || Player is null)
        {
            return;
        }

        try
        {
            var uri = Camera.BuildUri();
            Player.Open(uri.ToString());
        }
        catch (Exception ex)
        {
            UpdateConnectionState(ConnectionState.ConnectionFailed, ex.Message);
        }
    }

    /// <summary>
    /// Stops the camera stream.
    /// </summary>
    public void Stop()
    {
        Player?.Stop();
    }

    /// <summary>
    /// Prepares the tile for a swap operation.
    /// During swap, the player will not be disposed when the camera changes.
    /// </summary>
    public void PrepareForSwap()
    {
        isSwapping = true;
    }

    /// <summary>
    /// Completes the swap operation, restoring normal behavior.
    /// </summary>
    public void CompleteSwap()
    {
        isSwapping = false;
    }

    /// <summary>
    /// Lends the player to another control (e.g., fullscreen window).
    /// The player is removed from this tile and the tile shows a placeholder.
    /// </summary>
    /// <returns>The player instance, or null if no player is available.</returns>
    public Player? LendPlayer()
    {
        if (Player is null || IsPlayerLent)
        {
            return null;
        }

        var lentPlayer = Player;
        Player = null;
        IsPlayerLent = true;
        return lentPlayer;
    }

    /// <summary>
    /// Returns a previously lent player to this tile.
    /// </summary>
    /// <param name="returnedPlayer">The player to return.</param>
    public void ReturnPlayer(Player? returnedPlayer)
    {
        if (returnedPlayer is null)
        {
            IsPlayerLent = false;
            return;
        }

        Player = returnedPlayer;
        IsPlayerLent = false;
    }

    /// <summary>
    /// Reconnects the camera stream.
    /// </summary>
    public void Reconnect()
    {
        if (Camera is null || Player is null)
        {
            return;
        }

        // Set flag to skip Disconnected state during reconnection
        isReconnecting = true;

        // Update state immediately
        UpdateConnectionState(ConnectionState.Connecting);

        // Defer player operations to allow UI to render the "Connecting" state first
        // Player.Open() can block the UI thread, so we use BeginInvoke to let the render pass complete
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            ReconnectPlayer);
    }

    /// <summary>
    /// Disposes of the control resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            // Fire disconnect event if camera was connected
            if (Camera is not null && ConnectionState != ConnectionState.Disconnected)
            {
                var oldState = ConnectionState;
                ConnectionState = ConnectionState.Disconnected;
                ConnectionStateChanged?.Invoke(
                    this,
                    new CameraConnectionChangedEventArgs(Camera, oldState, ConnectionState.Disconnected));
            }

            reconnectTimer?.Stop();
            reconnectTimer = null;

            autoReconnectTimer?.Stop();
            autoReconnectTimer = null;

            recordingDurationTimer?.Stop();
            recordingDurationTimer = null;

            // Stop motion detection
            StopMotionDetection();

            // Unsubscribe from services
            if (recordingService is not null)
            {
                recordingService.RecordingStateChanged -= OnServiceRecordingStateChanged;
            }

            if (motionDetectionService is not null)
            {
                motionDetectionService.MotionDetected -= OnMotionDetected;
            }

            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
            VideoPlayer.OverlayCreated -= OnOverlayCreated;
            VideoPlayer.SurfaceCreated -= OnSurfaceCreated;

            if (VideoPlayer.Overlay is not null)
            {
                VideoPlayer.Overlay.PreviewMouseRightButtonUp -= OnMouseRightButtonUp;
            }

            if (VideoPlayer.Surface is not null)
            {
                VideoPlayer.Surface.PreviewMouseRightButtonUp -= OnMouseRightButtonUp;
            }

            if (Camera is not null)
            {
                Camera.PropertyChanged -= OnCameraPropertyChanged;
            }

            Player?.Dispose();
            Player = null;
        }

        disposed = true;
    }

    private void OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        // Try to subscribe if windows already exist
        SubscribeToMouseEvents();

        // Delay overlay caching to ensure the visual tree is fully built
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            CacheOverlayReference);
    }

    private void OnUnloaded(
        object sender,
        RoutedEventArgs e)
    {
        Dispose();
    }

    private void OnOverlayCreated(
        object? sender,
        EventArgs e)
    {
        SubscribeToMouseEvents();

        // Delay overlay caching to ensure the visual tree is fully built
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            CacheOverlayReference);
    }

    private void OnSurfaceCreated(
        object? sender,
        EventArgs e)
        => SubscribeToMouseEvents();

    private void OnMouseRightButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        // Don't show context menu while reconnecting
        if (isReconnecting)
        {
            e.Handled = true;
            return;
        }

        var contextMenu = new ContextMenu();

        contextMenu.Items.Add(new MenuItem { Header = Translations.ShowInFullScreen, Command = FullScreenCommand });
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(new MenuItem { Header = Translations.SwapLeft, Command = SwapLeftCommand });
        contextMenu.Items.Add(new MenuItem { Header = Translations.SwapRight, Command = SwapRightCommand });
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(new MenuItem { Header = Translations.TakeSnapshot, Command = SnapshotCommand });

        // Recording menu items
        if (RecordingState == RecordingState.Idle)
        {
            contextMenu.Items.Add(new MenuItem { Header = Translations.StartRecording, Command = StartRecordingCommand });
        }
        else
        {
            contextMenu.Items.Add(new MenuItem { Header = Translations.StopRecording, Command = StopRecordingCommand });
        }

        contextMenu.Items.Add(new MenuItem { Header = Translations.Reconnect, Command = ReconnectCommand });
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(new MenuItem { Header = Translations.EditCamera, Command = EditCameraCommand });

        contextMenu.IsOpen = true;
        e.Handled = true;
    }

    private static void OnCameraChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile)
        {
            if (e.OldValue is CameraConfiguration oldCamera)
            {
                oldCamera.PropertyChanged -= tile.OnCameraPropertyChanged;
            }

            if (e.NewValue is CameraConfiguration newCamera)
            {
                newCamera.PropertyChanged += tile.OnCameraPropertyChanged;
            }

            tile.OnCameraChangedInternal(e.NewValue as CameraConfiguration);
        }
    }

    private static void OnIsSelectedChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile)
        {
            tile.SelectionBorderBrush = (bool)e.NewValue
                ? Brushes.DodgerBlue
                : Brushes.Transparent;
        }
    }

    private static void OnConnectionStateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile && e.NewValue is ConnectionState newState)
        {
            tile.UpdateOverlayConnectionState(newState);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private static void OnCameraNameChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile && e.NewValue is string newName)
        {
            tile.UpdateOverlayTitle(newName);
        }
    }

    private static void OnCameraDescriptionChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile)
        {
            tile.UpdateOverlayDescription(e.NewValue as string ?? string.Empty);
        }
    }

    private static void OnOverlaySettingsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile)
        {
            tile.ApplyOverlaySettings();
        }
    }

    private static void OnPerformanceSettingChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        // Performance settings require player recreation - recreate if currently connected
        if (d is not CameraTile tile)
        {
            return;
        }

        // Use Dispatcher to ensure we're on the UI thread and to defer the recreation
        // until after the binding updates have completed
        _ = tile.Dispatcher.BeginInvoke(() =>
        {
            if (tile.ConnectionState == ConnectionState.Connected && tile.Camera is not null)
            {
                tile.RecreatePlayer();
            }
        });
    }

    private static void OnBoundingBoxSettingsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not CameraTile tile)
        {
            return;
        }

        // Start or stop motion detection based on current settings
        // This handles the case where ShowBoundingBoxInGrid changes while camera is connected
        if (tile.ConnectionState != ConnectionState.Connected)
        {
            // Not connected - just apply settings for visual update
            tile.ApplyBoundingBoxSettings();
            return;
        }

        if (tile.ShouldRunMotionDetection)
        {
            // StartMotionDetection will call ApplyBoundingBoxSettings after starting
            tile.StartMotionDetection();
        }
        else
        {
            tile.StopMotionDetection();
            tile.ApplyBoundingBoxSettings();
        }
    }

    private static void OnRecordingOnMotionSettingChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not CameraTile tile)
        {
            return;
        }

        // Update overlay to show/hide recording indicator row
        tile.UpdateOverlayRecordingOnMotion();

        // Start or stop motion detection based on current settings
        // This handles the case where EnableRecordingOnMotion changes while camera is connected
        if (tile.ConnectionState == ConnectionState.Connected)
        {
            if (tile.ShouldRunMotionDetection)
            {
                tile.StartMotionDetection();
            }
            else
            {
                tile.StopMotionDetection();
            }
        }
    }

    private void OnCameraPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(CameraConfiguration.CanSwapLeft):
                case nameof(CameraConfiguration.CanSwapRight):
                    CommandManager.InvalidateRequerySuggested();
                    break;
                case nameof(CameraConfiguration.Display):
                    CameraName = Camera?.Display.DisplayName ?? string.Empty;
                    CameraDescription = Camera?.Display.Description ?? string.Empty;
                    if (Camera is not null)
                    {
                        UpdateOverlayPosition(Camera.Display.OverlayPosition);
                    }

                    break;
                case nameof(CameraConfiguration.Connection):
                case nameof(CameraConfiguration.Authentication):
                case nameof(CameraConfiguration.Stream):
                    // These settings require player recreation to take effect
                    RecreatePlayer();
                    break;
                case nameof(CameraConfiguration.Overrides):
                    // Only reconnect if performance-related overrides changed
                    // Display overrides (overlay settings) don't require reconnection
                    HandleOverridesChanged();
                    break;
            }
        });
    }

    private void HandleOverridesChanged()
    {
        var currentVideoQuality = Camera?.Overrides?.VideoQuality;
        var currentHardwareAcceleration = Camera?.Overrides?.HardwareAcceleration;

        var performanceChanged =
            currentVideoQuality != previousOverrideVideoQuality ||
            currentHardwareAcceleration != previousOverrideHardwareAcceleration;

        // Update tracked values
        previousOverrideVideoQuality = currentVideoQuality;
        previousOverrideHardwareAcceleration = currentHardwareAcceleration;

        if (performanceChanged)
        {
            // Performance settings changed - need to recreate player
            RecreatePlayer();
        }

        // Check for motion detection override changes
        var currentEnableRecordingOnMotion = Camera?.Overrides?.EnableRecordingOnMotion;
        var currentShowBoundingBoxInGrid = Camera?.Overrides?.ShowBoundingBoxInGrid;
        var currentShowBoundingBoxInFullScreen = Camera?.Overrides?.ShowBoundingBoxInFullScreen;
        var currentBoundingBoxColor = Camera?.Overrides?.BoundingBoxColor;
        var currentBoundingBoxThickness = Camera?.Overrides?.BoundingBoxThickness;
        var currentBoundingBoxMinArea = Camera?.Overrides?.BoundingBoxMinArea;
        var currentMotionSensitivity = Camera?.Overrides?.MotionSensitivity;
        var currentMotionMinimumChangePercent = Camera?.Overrides?.MotionMinimumChangePercent;
        var currentMotionAnalysisFrameRate = Camera?.Overrides?.MotionAnalysisFrameRate;
        var currentMotionAnalysisWidth = Camera?.Overrides?.MotionAnalysisWidth;
        var currentMotionAnalysisHeight = Camera?.Overrides?.MotionAnalysisHeight;
        var currentMotionCooldownSeconds = Camera?.Overrides?.MotionCooldownSeconds;
        var currentPostMotionDurationSeconds = Camera?.Overrides?.PostMotionDurationSeconds;

        var motionDetectionChanged =
            currentEnableRecordingOnMotion != previousOverrideEnableRecordingOnMotion ||
            currentShowBoundingBoxInGrid != previousOverrideShowBoundingBoxInGrid ||
            currentShowBoundingBoxInFullScreen != previousOverrideShowBoundingBoxInFullScreen ||
            currentBoundingBoxColor != previousOverrideBoundingBoxColor ||
            currentBoundingBoxThickness != previousOverrideBoundingBoxThickness ||
            currentBoundingBoxMinArea != previousOverrideBoundingBoxMinArea ||
            currentMotionSensitivity != previousOverrideMotionSensitivity ||
            !currentMotionMinimumChangePercent.IsEqual(previousOverrideMotionMinimumChangePercent) ||
            currentMotionAnalysisFrameRate != previousOverrideMotionAnalysisFrameRate ||
            currentMotionAnalysisWidth != previousOverrideMotionAnalysisWidth ||
            currentMotionAnalysisHeight != previousOverrideMotionAnalysisHeight ||
            currentMotionCooldownSeconds != previousOverrideMotionCooldownSeconds ||
            currentPostMotionDurationSeconds != previousOverridePostMotionDurationSeconds;

        // Update tracked values
        previousOverrideEnableRecordingOnMotion = currentEnableRecordingOnMotion;
        previousOverrideShowBoundingBoxInGrid = currentShowBoundingBoxInGrid;
        previousOverrideShowBoundingBoxInFullScreen = currentShowBoundingBoxInFullScreen;
        previousOverrideBoundingBoxColor = currentBoundingBoxColor;
        previousOverrideBoundingBoxThickness = currentBoundingBoxThickness;
        previousOverrideBoundingBoxMinArea = currentBoundingBoxMinArea;
        previousOverrideMotionSensitivity = currentMotionSensitivity;
        previousOverrideMotionMinimumChangePercent = currentMotionMinimumChangePercent;
        previousOverrideMotionAnalysisFrameRate = currentMotionAnalysisFrameRate;
        previousOverrideMotionAnalysisWidth = currentMotionAnalysisWidth;
        previousOverrideMotionAnalysisHeight = currentMotionAnalysisHeight;
        previousOverrideMotionCooldownSeconds = currentMotionCooldownSeconds;
        previousOverridePostMotionDurationSeconds = currentPostMotionDurationSeconds;

        if (motionDetectionChanged)
        {
            // Always update overlay when EnableRecordingOnMotion override changes
            UpdateOverlayRecordingOnMotion();

            if (ConnectionState == ConnectionState.Connected)
            {
                // Motion detection settings changed - restart motion detection with new settings
                System.Diagnostics.Debug.WriteLine(
                    $"[MotionDetection] Override changed for '{Camera?.Display.DisplayName}' - restarting motion detection");

                StopMotionDetection();

                if (ShouldRunMotionDetection)
                {
                    // StartMotionDetection will call ApplyBoundingBoxSettings after starting
                    StartMotionDetection();
                }
                else
                {
                    // If not running motion detection, still apply bounding box settings to hide the overlay
                    ApplyBoundingBoxSettings();
                }
            }
        }

        // Display overrides are handled via bindings automatically
        // No action needed here for overlay settings
    }

    /// <summary>
    /// Recreates the player with current settings. Used when performance settings change.
    /// </summary>
    public void RecreatePlayer()
    {
        if (Camera is null)
        {
            return;
        }

        // Keep reference to old player
        var oldPlayer = Player;

        // Create new player with updated settings FIRST
        var newPlayer = CreatePlayer(Camera);
        newPlayer.PropertyChanged += OnPlayerPropertyChanged;

        // Swap to new player (without intermediate null state)
        Player = newPlayer;

        // Now cleanup old player
        if (oldPlayer is not null)
        {
            oldPlayer.PropertyChanged -= OnPlayerPropertyChanged;
            oldPlayer.Dispose();
        }

        UpdateConnectionState(ConnectionState.Connecting);

        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            PlayOnBackgroundThread);
    }

    private void OnCameraChangedInternal(CameraConfiguration? cameraConfig)
    {
        // During swap, the player is preserved - only update visual properties
        if (isSwapping)
        {
            if (cameraConfig is null)
            {
                CameraName = string.Empty;
                CameraDescription = string.Empty;
                return;
            }

            CameraName = cameraConfig.Display.DisplayName;
            CameraDescription = cameraConfig.Display.Description ?? string.Empty;
            UpdateOverlayPosition(cameraConfig.Display.OverlayPosition);
            return;
        }

        // Cleanup previous player
        if (Player is not null)
        {
            Player.PropertyChanged -= OnPlayerPropertyChanged;
            Player.Dispose();
            Player = null;
        }

        if (cameraConfig is null)
        {
            CameraName = string.Empty;
            CameraDescription = string.Empty;
            previousOverrideVideoQuality = null;
            previousOverrideHardwareAcceleration = null;
            previousOverrideEnableRecordingOnMotion = null;
            previousOverrideShowBoundingBoxInGrid = null;
            previousOverrideShowBoundingBoxInFullScreen = null;
            previousOverrideBoundingBoxColor = null;
            previousOverrideBoundingBoxThickness = null;
            previousOverrideBoundingBoxMinArea = null;
            previousOverrideMotionSensitivity = null;
            previousOverrideMotionMinimumChangePercent = null;
            previousOverrideMotionAnalysisFrameRate = null;
            previousOverrideMotionAnalysisWidth = null;
            previousOverrideMotionAnalysisHeight = null;
            previousOverrideMotionCooldownSeconds = null;
            previousOverridePostMotionDurationSeconds = null;
            return;
        }

        // Initialize tracked override values for change detection
        previousOverrideVideoQuality = cameraConfig.Overrides?.VideoQuality;
        previousOverrideHardwareAcceleration = cameraConfig.Overrides?.HardwareAcceleration;
        previousOverrideEnableRecordingOnMotion = cameraConfig.Overrides?.EnableRecordingOnMotion;
        previousOverrideShowBoundingBoxInGrid = cameraConfig.Overrides?.ShowBoundingBoxInGrid;
        previousOverrideShowBoundingBoxInFullScreen = cameraConfig.Overrides?.ShowBoundingBoxInFullScreen;
        previousOverrideBoundingBoxColor = cameraConfig.Overrides?.BoundingBoxColor;
        previousOverrideBoundingBoxThickness = cameraConfig.Overrides?.BoundingBoxThickness;
        previousOverrideBoundingBoxMinArea = cameraConfig.Overrides?.BoundingBoxMinArea;
        previousOverrideMotionSensitivity = cameraConfig.Overrides?.MotionSensitivity;
        previousOverrideMotionMinimumChangePercent = cameraConfig.Overrides?.MotionMinimumChangePercent;
        previousOverrideMotionAnalysisFrameRate = cameraConfig.Overrides?.MotionAnalysisFrameRate;
        previousOverrideMotionAnalysisWidth = cameraConfig.Overrides?.MotionAnalysisWidth;
        previousOverrideMotionAnalysisHeight = cameraConfig.Overrides?.MotionAnalysisHeight;
        previousOverrideMotionCooldownSeconds = cameraConfig.Overrides?.MotionCooldownSeconds;
        previousOverridePostMotionDurationSeconds = cameraConfig.Overrides?.PostMotionDurationSeconds;

        CameraName = cameraConfig.Display.DisplayName;
        CameraDescription = cameraConfig.Display.Description ?? string.Empty;

        Player = CreatePlayer(cameraConfig);
        Player.PropertyChanged += OnPlayerPropertyChanged;

        UpdateOverlayPosition(cameraConfig.Display.OverlayPosition);

        // Only auto-connect if enabled
        if (AutoConnectOnLoad)
        {
            // Show connecting state immediately
            UpdateConnectionState(ConnectionState.Connecting);

            // Defer player open to allow UI to render first
            // Player.Open() can block the UI thread, so we use BeginInvoke to let the render pass complete
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                PlayOnBackgroundThread);
        }
        else
        {
            // Stay disconnected until user manually connects
            UpdateConnectionState(ConnectionState.Disconnected);
        }
    }

    private void PlayOnBackgroundThread()
    {
        if (Camera is null || Player is null)
        {
            return;
        }

        // Capture references for background thread
        var capturedPlayer = Player;
        var capturedCamera = Camera;

        // Run blocking player operations on background thread to keep UI responsive
        _ = Task.Run(() =>
        {
            try
            {
                var uri = capturedCamera.BuildUri();
                capturedPlayer.Open(uri.ToString());
            }
            catch (Exception ex)
            {
                _ = Dispatcher.BeginInvoke(() =>
                {
                    UpdateConnectionState(ConnectionState.ConnectionFailed, ex.Message);
                });
            }
        });
    }

    private Player CreatePlayer(CameraConfiguration camera)
    {
        var config = new Config
        {
            Player =
            {
                AutoPlay = true,
                MaxLatency = camera.Stream.MaxLatencyMs * 10000L,
            },
            Video =
            {
                BackColor = Colors.Black,
                VideoAcceleration = HardwareAcceleration,
                MaxVerticalResolutionCustom = DropDownItemsFactory.GetMaxResolutionFromQuality(VideoQuality),
            },
            Audio =
            {
                Enabled = false,
            },
        };

        if (camera.Stream.UseLowLatencyMode)
        {
            config.Demuxer.BufferDuration = camera.Stream.BufferDurationMs * 10000L;
            config.Demuxer.FormatOpt["rtsp_transport"] = camera.Stream.RtspTransport;
            config.Demuxer.FormatOpt["fflags"] = "nobuffer";
        }

        return new Player(config);
    }

    private void OnPlayerPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Player.Status))
        {
            return;
        }

        // Capture status immediately - don't re-read inside BeginInvoke
        var capturedStatus = Player?.Status;
        var reconnecting = isReconnecting;

        _ = Dispatcher.BeginInvoke(() =>
        {
            // Use captured status to avoid race conditions with rapid state changes
            switch (capturedStatus)
            {
                case Status.Playing:
                case Status.Paused:
                    isReconnecting = false;
                    UpdateConnectionState(ConnectionState.Connected);
                    break;
                case Status.Opening:
                    UpdateConnectionState(ConnectionState.Connecting);
                    break;
                case Status.Stopped:
                case Status.Ended:
                    // Skip Disconnected state during reconnection (player stops briefly before reopening)
                    if (!reconnecting)
                    {
                        UpdateConnectionState(ConnectionState.Disconnected);
                    }

                    break;
                case Status.Failed:
                    isReconnecting = false;
                    UpdateConnectionState(ConnectionState.ConnectionFailed);
                    break;
            }
        });
    }

    private void CacheOverlayReference()
    {
        if (VideoPlayer.Overlay is null)
        {
            return;
        }

        // Try to find CameraOverlay in overlay window content
        if (VideoPlayer.Overlay.Content is CameraOverlay directOverlay)
        {
            cachedOverlay = directOverlay;
        }
        else if (VideoPlayer.Overlay.Content is DependencyObject content)
        {
            // Search both visual and logical trees
            cachedOverlay = content
                .FindChildren<CameraOverlay>()
                .FirstOrDefault();
        }

        // If still not found, try to find in overlay window itself
        cachedOverlay ??= VideoPlayer.Overlay
            .FindChildren<CameraOverlay>()
            .FirstOrDefault();

        // If found, sync current state
        if (cachedOverlay is not null)
        {
            cachedOverlay.Title = CameraName;
            cachedOverlay.Description = CameraDescription;
            cachedOverlay.ConnectionState = ConnectionState;
            ApplyOverlaySettings();
            UpdateOverlayRecordingOnMotion();
        }

        // Cache motion bounding box overlay
        CacheMotionOverlayReference();
    }

    private void CacheMotionOverlayReference()
    {
        if (VideoPlayer.Overlay is null)
        {
            return;
        }

        // Try to find MotionBoundingBoxOverlay in overlay window content
        if (VideoPlayer.Overlay.Content is MotionBoundingBoxOverlay directOverlay)
        {
            cachedMotionOverlay = directOverlay;
        }
        else if (VideoPlayer.Overlay.Content is DependencyObject content)
        {
            // Search both visual and logical trees
            cachedMotionOverlay = content
                .FindChildren<MotionBoundingBoxOverlay>()
                .FirstOrDefault();
        }

        // If still not found, try to find in overlay window itself
        cachedMotionOverlay ??= VideoPlayer.Overlay
            .FindChildren<MotionBoundingBoxOverlay>()
            .FirstOrDefault();

        // If found, apply current settings
        if (cachedMotionOverlay is not null)
        {
            ApplyBoundingBoxSettings();
        }
    }

    private void ApplyOverlaySettings()
    {
        var overlay = GetCameraOverlay();
        if (overlay is null)
        {
            return;
        }

        overlay.ShowTitle = ShowOverlayTitle;
        overlay.ShowDescription = ShowOverlayDescription;
        overlay.ShowConnectionStatus = ShowOverlayConnectionStatus;
        overlay.ShowTime = ShowOverlayTime;
        overlay.OverlayOpacity = OverlayOpacity;
    }

    private void SubscribeToMouseEvents()
    {
        if (VideoPlayer.Overlay is not null)
        {
            VideoPlayer.Overlay.PreviewMouseRightButtonUp -= OnMouseRightButtonUp;
            VideoPlayer.Overlay.PreviewMouseRightButtonUp += OnMouseRightButtonUp;
        }

        if (VideoPlayer.Surface is not null)
        {
            VideoPlayer.Surface.PreviewMouseRightButtonUp -= OnMouseRightButtonUp;
            VideoPlayer.Surface.PreviewMouseRightButtonUp += OnMouseRightButtonUp;
        }
    }

    private void UpdateConnectionState(
        ConnectionState newState,
        string? errorMessage = null)
    {
        if (ConnectionState == newState)
        {
            return;
        }

        var previous = previousState;
        previousState = ConnectionState;
        ConnectionState = newState;

        // Force update overlay directly
        var overlay = GetCameraOverlay();
        if (overlay is not null)
        {
            overlay.ConnectionState = newState;
        }

        if (Camera is not null)
        {
            ConnectionStateChanged?.Invoke(
                this,
                new CameraConnectionChangedEventArgs(Camera, previous, newState, errorMessage));
        }

        // Handle notifications and auto-reconnect
        HandleConnectionStateChange(previous, newState);
    }

    private void HandleConnectionStateChange(
        ConnectionState previousState,
        ConnectionState newState)
    {
        // Handle notifications
        if (newState == ConnectionState.Disconnected || newState == ConnectionState.ConnectionFailed)
        {
            if (previousState == ConnectionState.Connected && ShowNotificationOnDisconnect)
            {
                ShowNotification(
                    string.Format(CultureInfo.CurrentCulture, Translations.CameraDisconnected1, Camera?.Display.DisplayName ?? string.Empty));
            }

            // Stop motion detection when disconnected
            StopMotionDetection();

            // Try auto-reconnect on failure (but not on manual disconnect)
            if (newState == ConnectionState.ConnectionFailed)
            {
                TryAutoReconnect();
            }
        }
        else if (newState == ConnectionState.Connected)
        {
            // Reset reconnect attempts on successful connection
            currentReconnectAttempt = 0;

            if (previousState is ConnectionState.Disconnected or ConnectionState.ConnectionFailed or ConnectionState.Connecting
                && ShowNotificationOnReconnect)
            {
                ShowNotification(
                    string.Format(CultureInfo.CurrentCulture, Translations.CameraConnected1, Camera?.Display.DisplayName ?? string.Empty));
            }

            // Auto-start recording on connect if enabled
            if (EnableRecordingOnConnect &&
                recordingService is not null &&
                Player is not null &&
                Camera is not null &&
                RecordingState == RecordingState.Idle)
            {
                recordingService.StartRecording(Camera, Player);
            }

            // Auto-start motion detection if enabled (for motion-triggered recording or bounding box display)
            if (ShouldRunMotionDetection)
            {
                StartMotionDetection();
            }
        }
    }

    private void TryAutoReconnect()
    {
        if (!AutoReconnectOnFailure || Camera is null)
        {
            return;
        }

        if (currentReconnectAttempt >= MaxReconnectAttempts)
        {
            // Max attempts reached, stop trying
            return;
        }

        currentReconnectAttempt++;

        // Schedule auto-reconnect after delay
        autoReconnectTimer?.Stop();
        autoReconnectTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(ReconnectDelaySeconds),
        };

        autoReconnectTimer.Tick += (_, _) =>
        {
            autoReconnectTimer?.Stop();

            // Only reconnect if still in failed state
            if (ConnectionState == ConnectionState.ConnectionFailed && Camera is not null)
            {
                Reconnect();
            }
        };

        autoReconnectTimer.Start();
    }

    private void ShowNotification(string message)
    {
        if (PlayNotificationSound)
        {
            PlaySystemNotificationSound();
        }

        // Show Windows toast notification
        ShowToastNotification(message);
    }

    private static void PlaySystemNotificationSound()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            // Ignore sound playback errors
        }
    }

    private static void ShowToastNotification(string message)
    {
        // For now, we'll use a simple approach - the ConnectionStateChanged event
        // can be handled by the parent to show notifications in a toast/snackbar control
        // A full toast notification implementation would require additional infrastructure
        System.Diagnostics.Debug.WriteLine($"Notification: {message}");
    }

    private void UpdateOverlayConnectionState(ConnectionState state)
    {
        var overlay = GetCameraOverlay();
        overlay?.ConnectionState = state;
    }

    private void UpdateOverlayDescription(string description)
    {
        var overlay = GetCameraOverlay();
        overlay?.Description = description;
    }

    private void UpdateOverlayPosition(OverlayPosition position)
    {
        OverlayHorizontalAlignment = position switch
        {
            OverlayPosition.TopLeft or OverlayPosition.BottomLeft => HorizontalAlignment.Left,
            OverlayPosition.TopRight or OverlayPosition.BottomRight => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };

        OverlayVerticalAlignment = position switch
        {
            OverlayPosition.TopLeft or OverlayPosition.TopRight => VerticalAlignment.Top,
            OverlayPosition.BottomLeft or OverlayPosition.BottomRight => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top,
        };
    }

    private void UpdateOverlayTitle(string title)
    {
        var overlay = GetCameraOverlay();
        overlay?.Title = title;
    }

    private CameraOverlay? GetCameraOverlay()
    {
        // Return cached reference if available
        if (cachedOverlay is not null)
        {
            return cachedOverlay;
        }

        // Try to cache it now
        CacheOverlayReference();
        return cachedOverlay;
    }

    private void ReconnectPlayer()
    {
        if (Camera is null || Player is null)
        {
            return;
        }

        // Capture references for background thread
        var capturedPlayer = Player;
        var capturedCamera = Camera;

        // Start status check timer before background operation
        StartReconnectStatusCheck();

        // Run blocking player operations on background thread to keep UI responsive
        _ = Task.Run(() =>
        {
            try
            {
                // Stop the player first to ensure proper status transitions on reopen
                capturedPlayer.Stop();

                var uri = capturedCamera.BuildUri();
                capturedPlayer.Open(uri.ToString());
            }
            catch (Exception ex)
            {
                _ = Dispatcher.BeginInvoke(() =>
                {
                    isReconnecting = false;
                    UpdateConnectionState(ConnectionState.ConnectionFailed, ex.Message);
                });
            }
        });
    }

    private void StartReconnectStatusCheck()
    {
        // Stop any existing timer
        reconnectTimer?.Stop();

        var checkCount = 0;
        var maxChecks = ConnectionTimeoutSeconds * 2; // Check interval is 500ms, so multiply by 2

        reconnectTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };

        reconnectTimer.Tick += (_, _) =>
        {
            checkCount++;

            if (!isReconnecting)
            {
                // Already handled by Status property change
                reconnectTimer?.Stop();
                return;
            }

            if (Player?.Status == Status.Playing)
            {
                isReconnecting = false;
                UpdateConnectionState(ConnectionState.Connected);
                reconnectTimer?.Stop();
                return;
            }

            if (Player?.Status == Status.Failed || checkCount >= maxChecks)
            {
                isReconnecting = false;
                UpdateConnectionState(ConnectionState.ConnectionFailed, Translations.ConnectionTimedOut);
                reconnectTimer?.Stop();
            }
        };

        reconnectTimer.Start();
    }

    private bool CanExecuteCameraCommand()
        => Camera is not null;

    [RelayCommand(CanExecute = nameof(CanExecuteCameraCommand))]
    private void EditCamera()
    {
        if (Camera is not null)
        {
            EditCameraRequested?.Invoke(this, Camera);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCameraCommand))]
    private void DeleteCamera()
    {
        if (Camera is not null)
        {
            DeleteCameraRequested?.Invoke(this, Camera);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteWhenConnected))]
    private void FullScreen()
    {
        if (Camera is not null)
        {
            FullScreenRequested?.Invoke(this, new FullScreenRequestedEventArgs(Camera, this));
        }
    }

    private bool CanExecuteSwapLeft()
        => Camera is { CanSwapLeft: true };

    [RelayCommand(CanExecute = nameof(CanExecuteSwapLeft))]
    private void SwapLeft()
    {
        if (Camera is not null)
        {
            SwapLeftRequested?.Invoke(this, Camera);
        }
    }

    private bool CanExecuteSwapRight()
        => Camera is { CanSwapRight: true };

    [RelayCommand(CanExecute = nameof(CanExecuteSwapRight))]
    private void SwapRight()
    {
        if (Camera is not null)
        {
            SwapRightRequested?.Invoke(this, Camera);
        }
    }

    private bool CanExecuteWhenConnected()
        => Camera is not null &&
           ConnectionState == ConnectionState.Connected;

    [RelayCommand(CanExecute = nameof(CanExecuteWhenConnected))]
    private void Snapshot()
    {
        if (Player is null || Camera is null)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = Translations.SaveSnapshot,
            Filter = "PNG Image|*.png|JPEG Image|*.jpg",
            FileName = $"{Camera.Display.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}",
        };

        // Use configured snapshot path as initial directory if available
        if (!string.IsNullOrEmpty(SnapshotPath))
        {
            // Create the directory if it doesn't exist
            if (!Directory.Exists(SnapshotPath))
            {
                try
                {
                    Directory.CreateDirectory(SnapshotPath);
                }
                catch
                {
                    // If we can't create the directory, fall back to default behavior
                }
            }

            if (Directory.Exists(SnapshotPath))
            {
                dialog.InitialDirectory = SnapshotPath;
            }
        }

        if (dialog.ShowDialog() == true)
        {
            try
            {
                Player.TakeSnapshotToFile(dialog.FileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Snapshot failed: {ex.Message}");
            }
        }
    }

    [RelayCommand(nameof(Reconnect), CanExecute = nameof(CanExecuteCameraCommand))]
    private void ExecuteReconnect()
    {
        Reconnect();
    }

    private bool CanExecuteStartRecording()
        => Camera is not null &&
           ConnectionState == ConnectionState.Connected &&
           RecordingState == RecordingState.Idle &&
           recordingService is not null;

    [RelayCommand(CanExecute = nameof(CanExecuteStartRecording))]
    private void StartRecording()
    {
        if (Camera is null || Player is null || recordingService is null)
        {
            return;
        }

        recordingService.StartRecording(Camera, Player);
    }

    private bool CanExecuteStopRecording()
        => Camera is not null &&
           RecordingState != RecordingState.Idle &&
           recordingService is not null;

    [RelayCommand(CanExecute = nameof(CanExecuteStopRecording))]
    private void StopRecording()
    {
        if (Camera is null || recordingService is null)
        {
            return;
        }

        recordingService.StopRecording(Camera.Id);
    }

    private void OnServiceRecordingStateChanged(
        object? sender,
        RecordingStateChangedEventArgs e)
    {
        if (Camera is null || e.CameraId != Camera.Id)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            RecordingState = e.NewState;
            UpdateRecordingDurationTimer();
            UpdateOverlayRecordingState();

            RecordingStateChangedEvent?.Invoke(this, e);
            CommandManager.InvalidateRequerySuggested();
        });
    }

    private void OnMotionDetected(
        object? sender,
        MotionDetectedEventArgs e)
    {
        if (Camera is null || e.CameraId != Camera.Id)
        {
            return;
        }

        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] Motion event for '{Camera.Display.DisplayName}' - " +
            $"IsActive={e.IsMotionActive}, HasBoundingBox={e.BoundingBox.HasValue}, " +
            $"ChangePercentage={e.ChangePercentage:F2}%");

        Dispatcher.Invoke(() =>
        {
            // Update motion state based on IsMotionActive
            var wasMotionDetected = IsMotionDetected;
            IsMotionDetected = e.IsMotionActive;
            UpdateOverlayMotionIndicator();

            // Update bounding box overlay
            if (e.IsMotionActive && e.BoundingBox.HasValue)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MotionDetection] Updating bounding box: {e.BoundingBox.Value}, AnalysisRes={e.AnalysisWidth}x{e.AnalysisHeight}");
                UpdateMotionBoundingBox(e.BoundingBox, e.AnalysisWidth, e.AnalysisHeight);
            }
            else
            {
                UpdateMotionBoundingBox(boundingBox: null, e.AnalysisWidth, e.AnalysisHeight);
            }

            // Trigger motion recording if enabled and motion is active (use effective value for override)
            if (e.IsMotionActive && GetEffectiveEnableRecordingOnMotion() && recordingService is not null && Player is not null)
            {
                recordingService.TriggerMotionRecording(Camera, Player);
            }

            // If motion just stopped, schedule a delayed check to hide the bounding box
            if (wasMotionDetected && !e.IsMotionActive)
            {
                // Reset motion indicator after a short delay to avoid flickering
                var resetTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500),
                };
                resetTimer.Tick += (_, _) =>
                {
                    resetTimer.Stop();
                    if (motionDetectionService is null || !motionDetectionService.IsMotionDetected(Camera?.Id ?? Guid.Empty))
                    {
                        IsMotionDetected = false;
                        UpdateOverlayMotionIndicator();
                        UpdateMotionBoundingBox(boundingBox: null);
                    }
                };
                resetTimer.Start();
            }
        });
    }

    private static void OnRecordingStateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraTile tile && e.NewValue is RecordingState)
        {
            tile.UpdateOverlayRecordingState();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void UpdateRecordingDurationTimer()
    {
        if (RecordingState == RecordingState.Idle)
        {
            // Stop timer
            recordingDurationTimer?.Stop();
            recordingDurationTimer = null;
            RecordingDuration = TimeSpan.Zero;
        }
        else if (recordingDurationTimer is null)
        {
            // Start timer
            recordingDurationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            recordingDurationTimer.Tick += (_, _) => UpdateRecordingDuration();
            recordingDurationTimer.Start();
            UpdateRecordingDuration();
        }
    }

    private void UpdateRecordingDuration()
    {
        if (Camera is null || recordingService is null)
        {
            return;
        }

        var session = recordingService.GetSession(Camera.Id);
        if (session is not null)
        {
            RecordingDuration = session.Duration;

            // Update overlay with new duration
            var overlay = GetCameraOverlay();
            if (overlay is not null)
            {
                overlay.RecordingDuration = RecordingDuration;
            }
        }
    }

    private void UpdateOverlayRecordingState()
    {
        var overlay = GetCameraOverlay();
        if (overlay is not null)
        {
            overlay.IsRecording = RecordingState != RecordingState.Idle;
            overlay.RecordingDuration = RecordingDuration;
        }
    }

    private void UpdateOverlayMotionIndicator()
    {
        var overlay = GetCameraOverlay();
        if (overlay is not null)
        {
            overlay.IsMotionDetected = IsMotionDetected;
        }
    }

    private void UpdateOverlayRecordingOnMotion()
    {
        var overlay = GetCameraOverlay();
        if (overlay is not null)
        {
            overlay.EnableRecordingOnMotion = GetEffectiveEnableRecordingOnMotion();
        }
    }

    private void ApplyBoundingBoxSettings()
    {
        var motionOverlay = GetMotionBoundingBoxOverlay();
        if (motionOverlay is null)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[MotionDetection] ApplyBoundingBoxSettings for '{Camera?.Display.DisplayName ?? "null"}' - overlay not found!");
            return;
        }

        // Use effective values that consider per-camera overrides
        var effectiveShowInGrid = GetEffectiveShowBoundingBoxInGrid();
        var effectiveColor = GetEffectiveBoundingBoxColor();
        var effectiveThickness = GetEffectiveBoundingBoxThickness();
        var effectiveSmoothing = GetEffectiveBoundingBoxSmoothing();

        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] ApplyBoundingBoxSettings for '{Camera?.Display.DisplayName ?? "null"}' - " +
            $"ShowBoundingBoxInGrid={effectiveShowInGrid}, Color={effectiveColor}, Thickness={effectiveThickness}");

        motionOverlay.IsOverlayEnabled = effectiveShowInGrid;
        motionOverlay.BoxColor = effectiveColor;
        motionOverlay.BoxThickness = effectiveThickness;
        motionOverlay.SmoothingFactor = effectiveSmoothing;

        // Set analysis resolution from motion detection service if available
        if (motionDetectionService is not null && Camera is not null)
        {
            var (width, height) = motionDetectionService.GetAnalysisResolution(Camera.Id);
            motionOverlay.AnalysisWidth = width;
            motionOverlay.AnalysisHeight = height;
        }
    }

    private MotionBoundingBoxOverlay? GetMotionBoundingBoxOverlay()
    {
        // Return cached reference if available
        if (cachedMotionOverlay is not null)
        {
            return cachedMotionOverlay;
        }

        // Try to cache it now
        CacheMotionOverlayReference();
        return cachedMotionOverlay;
    }

    private void UpdateMotionBoundingBox(
        Rect? boundingBox,
        int analysisWidth = 320,
        int analysisHeight = 240)
    {
        var motionOverlay = GetMotionBoundingBoxOverlay();
        if (motionOverlay is null)
        {
            System.Diagnostics.Debug.WriteLine("[MotionDetection] UpdateMotionBoundingBox - overlay not found!");
            return;
        }

        // Update the overlay's analysis resolution to match the event's resolution
        // This ensures correct coordinate scaling
        motionOverlay.AnalysisWidth = analysisWidth;
        motionOverlay.AnalysisHeight = analysisHeight;

        // Get the video container size for coordinate mapping
        var containerSize = new Size(VideoPlayer.ActualWidth, VideoPlayer.ActualHeight);
        if ((containerSize.Width <= 0 || containerSize.Height <= 0) &&
            VideoPlayer.Overlay is not null)
        {
            // Try to get size from the overlay window
            containerSize = new Size(VideoPlayer.Overlay.ActualWidth, VideoPlayer.Overlay.ActualHeight);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] UpdateMotionBoundingBox - overlay.IsOverlayEnabled={motionOverlay.IsOverlayEnabled}, " +
            $"containerSize={containerSize.Width}x{containerSize.Height}, analysisRes={analysisWidth}x{analysisHeight}, boundingBox={boundingBox}");

        motionOverlay.UpdateBoundingBox(boundingBox, containerSize);
    }

    /// <summary>
    /// Starts motion detection for this camera if enabled (for recording or bounding box display).
    /// </summary>
    public void StartMotionDetection()
    {
        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] StartMotionDetection called for '{Camera?.Display.DisplayName ?? "null"}' - " +
            $"Camera={Camera is not null}, Player={Player is not null}, Service={motionDetectionService is not null}, " +
            $"EnableRecordingOnMotion={GetEffectiveEnableRecordingOnMotion()}, ShowBoundingBoxInGrid={GetEffectiveShowBoundingBoxInGrid()}, " +
            $"ShouldRun={ShouldRunMotionDetection}");

        if (Camera is null || Player is null || motionDetectionService is null || !ShouldRunMotionDetection)
        {
            System.Diagnostics.Debug.WriteLine("[MotionDetection] StartMotionDetection early return - preconditions not met");
            return;
        }

        // Build settings using effective values that consider per-camera overrides
        var (analysisWidth, analysisHeight) = GetEffectiveMotionAnalysisResolution();
        var settings = new MotionDetectionSettings
        {
            Sensitivity = GetEffectiveMotionSensitivity(),
            MinimumChangePercent = GetEffectiveMotionMinimumChangePercent(),
            AnalysisFrameRate = GetEffectiveMotionAnalysisFrameRate(),
            AnalysisWidth = analysisWidth,
            AnalysisHeight = analysisHeight,
            PostMotionDurationSeconds = GetEffectiveMotionPostDurationSeconds(),
            CooldownSeconds = GetEffectiveMotionCooldownSeconds(),
            BoundingBox = new BoundingBoxSettings
            {
                ShowInGrid = GetEffectiveShowBoundingBoxInGrid(),
                ShowInFullScreen = GetEffectiveShowBoundingBoxInFullScreen(),
                Color = GetEffectiveBoundingBoxColor(),
                Thickness = GetEffectiveBoundingBoxThickness(),
                MinArea = GetEffectiveBoundingBoxMinArea(),
                Padding = GetEffectiveBoundingBoxPadding(),
                Smoothing = GetEffectiveBoundingBoxSmoothing(),
            },
        };

        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] Starting detection for '{Camera.Display.DisplayName}' with " +
            $"ShowInGrid={settings.BoundingBox.ShowInGrid}, MinArea={settings.BoundingBox.MinArea}, " +
            $"Sensitivity={settings.Sensitivity}, Resolution={settings.AnalysisWidth}x{settings.AnalysisHeight}, " +
            $"OverrideMinArea={Camera.Overrides?.BoundingBoxMinArea}");
        motionDetectionService.StartDetection(Camera.Id, Player, settings);

        // Apply bounding box settings AFTER starting detection so the overlay gets the correct resolution
        ApplyBoundingBoxSettings();
    }

    /// <summary>
    /// Stops motion detection for this camera.
    /// </summary>
    public void StopMotionDetection()
    {
        if (Camera is null || motionDetectionService is null)
        {
            return;
        }

        motionDetectionService.StopDetection(Camera.Id);
    }

    private void OnDragCaptureLayerMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        dragStartPoint = e.GetPosition(this);

        // Handle double-click for fullscreen
        if (e.ClickCount == 2 && Camera is not null && ConnectionState == ConnectionState.Connected)
        {
            FullScreenRequested?.Invoke(this, new FullScreenRequestedEventArgs(Camera, this));
            e.Handled = true;
        }
    }

    private void OnDragCaptureLayerMouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || isDragging || Camera is null)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var diff = dragStartPoint - currentPosition;

        if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        isDragging = true;
        DragDrop.DoDragDrop(this, Camera, DragDropEffects.Move);
        isDragging = false;
    }

    private void OnDragCaptureLayerDragOver(
        object sender,
        DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(CameraConfiguration)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDragCaptureLayerDrop(
        object sender,
        DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(CameraConfiguration)))
        {
            return;
        }

        var sourceCamera = (CameraConfiguration)e.Data.GetData(typeof(CameraConfiguration))!;
        if (Camera is null || sourceCamera.Id == Camera.Id)
        {
            return;
        }

        CameraDropped?.Invoke(this, sourceCamera);
        e.Handled = true;
    }
}