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

    [DependencyProperty(DefaultValue = 10)]
    private int reconnectDelaySeconds;

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

    // Recording and notification services
    private IRecordingService? recordingService;
    private IMotionDetectionService? motionDetectionService;
    private ITimelapseService? timelapseService;
    private IToastNotificationService? toastNotificationService;
    private DispatcherTimer? recordingDurationTimer;
    private FlyleafLibMediaPipeline? mediaPipeline;

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
    private int reconnectCheckCount;

    // Stream health monitoring
    private DispatcherTimer? streamHealthTimer;
    private long lastFramesDisplayed;
    private int staleStreamCheckCount;

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
        => Camera?.Overrides?.Recording.EnableRecordingOnMotion ?? EnableRecordingOnMotion;

    /// <summary>
    /// Gets the effective ShowBoundingBoxInGrid value, considering camera override.
    /// </summary>
    private bool GetEffectiveShowBoundingBoxInGrid()
        => Camera?.Overrides?.MotionDetection.BoundingBox.ShowInGrid ?? ShowBoundingBoxInGrid;

    /// <summary>
    /// Gets the effective ShowBoundingBoxInFullScreen value, considering camera override.
    /// </summary>
    private bool GetEffectiveShowBoundingBoxInFullScreen()
        => Camera?.Overrides?.MotionDetection.BoundingBox.ShowInFullScreen ?? false;

    /// <summary>
    /// Gets the effective BoundingBoxColor value, considering camera override.
    /// </summary>
    private string GetEffectiveBoundingBoxColor()
        => Camera?.Overrides?.MotionDetection.BoundingBox.Color ?? BoundingBoxColor;

    /// <summary>
    /// Gets the effective BoundingBoxThickness value, considering camera override.
    /// </summary>
    private int GetEffectiveBoundingBoxThickness()
        => Camera?.Overrides?.MotionDetection.BoundingBox.Thickness ?? BoundingBoxThickness;

    /// <summary>
    /// Gets the effective BoundingBoxSmoothing value, considering camera override.
    /// </summary>
    private double GetEffectiveBoundingBoxSmoothing()
        => Camera?.Overrides?.MotionDetection.BoundingBox.Smoothing ?? BoundingBoxSmoothing;

    /// <summary>
    /// Gets the effective BoundingBoxMinArea value, considering camera override.
    /// </summary>
    private int GetEffectiveBoundingBoxMinArea()
        => Camera?.Overrides?.MotionDetection.BoundingBox.MinArea ?? MotionBoundingBoxMinArea;

    /// <summary>
    /// Gets the effective BoundingBoxPadding value, considering camera override.
    /// </summary>
    private int GetEffectiveBoundingBoxPadding()
        => Camera?.Overrides?.MotionDetection.BoundingBox.Padding ?? MotionBoundingBoxPadding;

    /// <summary>
    /// Gets the effective MotionSensitivity value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionSensitivity()
        => Camera?.Overrides?.MotionDetection.Sensitivity ?? MotionSensitivity;

    /// <summary>
    /// Gets the effective MotionMinimumChangePercent value, considering camera override.
    /// </summary>
    private double GetEffectiveMotionMinimumChangePercent()
        => Camera?.Overrides?.MotionDetection.MinimumChangePercent ?? MotionMinimumChangePercent;

    /// <summary>
    /// Gets the effective MotionAnalysisFrameRate value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionAnalysisFrameRate()
        => Camera?.Overrides?.MotionDetection.AnalysisFrameRate ?? MotionAnalysisFrameRate;

    /// <summary>
    /// Gets the effective MotionPostDurationSeconds value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionPostDurationSeconds()
        => Camera?.Overrides?.MotionDetection.PostMotionDurationSeconds ?? MotionPostDurationSeconds;

    /// <summary>
    /// Gets the effective MotionCooldownSeconds value, considering camera override.
    /// </summary>
    private int GetEffectiveMotionCooldownSeconds()
        => Camera?.Overrides?.MotionDetection.CooldownSeconds ?? MotionCooldownSeconds;

    /// <summary>
    /// Gets the effective AutoReconnectOnFailure value, considering camera override.
    /// </summary>
    private bool GetEffectiveAutoReconnectOnFailure()
        => Camera?.Overrides?.Connection.AutoReconnectOnFailure ?? AutoReconnectOnFailure;

    /// <summary>
    /// Gets the effective ReconnectDelaySeconds value, considering camera override.
    /// </summary>
    private int GetEffectiveReconnectDelaySeconds()
        => Camera?.Overrides?.Connection.ReconnectDelaySeconds ?? ReconnectDelaySeconds;

    /// <summary>
    /// Gets the effective motion analysis resolution, considering camera override.
    /// Returns width and height from override if set, otherwise uses service default.
    /// </summary>
    private (int Width, int Height) GetEffectiveMotionAnalysisResolution()
    {
        var overrideWidth = Camera?.Overrides?.MotionDetection.AnalysisWidth;
        var overrideHeight = Camera?.Overrides?.MotionDetection.AnalysisHeight;

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
    /// Initializes the recording, motion detection, timelapse, and toast notification services.
    /// </summary>
    /// <param name="recordingService">The recording service.</param>
    /// <param name="motionDetectionService">The motion detection service.</param>
    /// <param name="timelapseService">The timelapse service.</param>
    /// <param name="toastNotificationService">The toast notification service.</param>
    public void InitializeServices(
        IRecordingService? recordingService,
        IMotionDetectionService? motionDetectionService,
        ITimelapseService? timelapseService = null,
        IToastNotificationService? toastNotificationService = null)
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
        this.timelapseService = timelapseService;
        this.toastNotificationService = toastNotificationService;

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

        // Start timelapse if camera is already connected and enabled
        if (ConnectionState == ConnectionState.Connected)
        {
            StartTimelapse();
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
    /// Swaps the Player instance with another CameraTile to keep streams alive
    /// during a position swap. Both tiles should already have their Camera DPs
    /// updated (via collection swap with isSwapping=true). After this call, each
    /// tile's Player plays the stream matching its new Camera.
    /// </summary>
    /// <param name="other">The other tile to swap players with.</param>
    public void SwapPlayerWith(CameraTile other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var myPlayer = Player;
        var otherPlayer = other.Player;
        var myPipeline = mediaPipeline;
        var otherPipeline = other.mediaPipeline;

        // Detach PropertyChanged handlers from current players
        if (myPlayer is not null)
        {
            myPlayer.PropertyChanged -= OnPlayerPropertyChanged;
        }

        if (otherPlayer is not null)
        {
            otherPlayer.PropertyChanged -= other.OnPlayerPropertyChanged;
        }

        // Stop stream health monitoring on both (will be restarted after swap)
        StopStreamHealthCheck();
        other.StopStreamHealthCheck();

        // Detach both players from FlyleafHost to avoid a Player being
        // attached to two hosts simultaneously
        Player = null;
        other.Player = null;

        // Reattach swapped players
        Player = otherPlayer;
        other.Player = myPlayer;

        // Swap media pipelines
        mediaPipeline = otherPipeline;
        other.mediaPipeline = myPipeline;

        // Reattach PropertyChanged handlers to the new players
        if (Player is not null)
        {
            Player.PropertyChanged += OnPlayerPropertyChanged;
        }

        if (other.Player is not null)
        {
            other.Player.PropertyChanged += other.OnPlayerPropertyChanged;
        }

        // Swap recording state (tracked per camera by IRecordingService)
        (RecordingState, other.RecordingState) = (other.RecordingState, RecordingState);
        (RecordingDuration, other.RecordingDuration) = (other.RecordingDuration, RecordingDuration);

        // Stop recording duration timers — their Tick closures reference the original tile,
        // so we must recreate them rather than swapping references
        recordingDurationTimer?.Stop();
        recordingDurationTimer = null;
        other.recordingDurationTimer?.Stop();
        other.recordingDurationTimer = null;
        UpdateRecordingDurationTimer();
        other.UpdateRecordingDurationTimer();

        // Swap motion detection state
        (IsMotionDetected, other.IsMotionDetected) = (other.IsMotionDetected, IsMotionDetected);

        // Restart stream health monitoring with new players
        StartStreamHealthCheck();
        other.StartStreamHealthCheck();

        // Update overlays to reflect swapped state
        UpdateOverlayRecordingState();
        other.UpdateOverlayRecordingState();
        UpdateOverlayMotionIndicator();
        other.UpdateOverlayMotionIndicator();
        UpdateOverlayRecordingOnMotion();
        other.UpdateOverlayRecordingOnMotion();
        ApplyBoundingBoxSettings();
        other.ApplyBoundingBoxSettings();
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

            if (reconnectTimer is not null)
            {
                reconnectTimer.Stop();
                reconnectTimer.Tick -= OnReconnectTimerTick;
                reconnectTimer = null;
            }

            autoReconnectTimer?.Stop();
            autoReconnectTimer = null;

            // Stop stream health monitoring
            StopStreamHealthCheck();

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

            mediaPipeline?.Dispose();
            mediaPipeline = null;

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

        // Skip if value didn't actually change (safety for binding re-evaluation during layout switches)
        if (Equals(e.OldValue, e.NewValue))
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

    private void InitializeOverrideTracking(CameraConfiguration cameraConfig)
    {
        previousOverrideVideoQuality = cameraConfig.Overrides?.Performance.VideoQuality;
        previousOverrideHardwareAcceleration = cameraConfig.Overrides?.Performance.HardwareAcceleration;
        previousOverrideEnableRecordingOnMotion = cameraConfig.Overrides?.Recording.EnableRecordingOnMotion;
        previousOverrideShowBoundingBoxInGrid = cameraConfig.Overrides?.MotionDetection.BoundingBox.ShowInGrid;
        previousOverrideShowBoundingBoxInFullScreen = cameraConfig.Overrides?.MotionDetection.BoundingBox.ShowInFullScreen;
        previousOverrideBoundingBoxColor = cameraConfig.Overrides?.MotionDetection.BoundingBox.Color;
        previousOverrideBoundingBoxThickness = cameraConfig.Overrides?.MotionDetection.BoundingBox.Thickness;
        previousOverrideBoundingBoxMinArea = cameraConfig.Overrides?.MotionDetection.BoundingBox.MinArea;
        previousOverrideMotionSensitivity = cameraConfig.Overrides?.MotionDetection.Sensitivity;
        previousOverrideMotionMinimumChangePercent = cameraConfig.Overrides?.MotionDetection.MinimumChangePercent;
        previousOverrideMotionAnalysisFrameRate = cameraConfig.Overrides?.MotionDetection.AnalysisFrameRate;
        previousOverrideMotionAnalysisWidth = cameraConfig.Overrides?.MotionDetection.AnalysisWidth;
        previousOverrideMotionAnalysisHeight = cameraConfig.Overrides?.MotionDetection.AnalysisHeight;
        previousOverrideMotionCooldownSeconds = cameraConfig.Overrides?.MotionDetection.CooldownSeconds;
        previousOverridePostMotionDurationSeconds = cameraConfig.Overrides?.MotionDetection.PostMotionDurationSeconds;
    }

    private void HandleOverridesChanged()
    {
        var currentVideoQuality = Camera?.Overrides?.Performance.VideoQuality;
        var currentHardwareAcceleration = Camera?.Overrides?.Performance.HardwareAcceleration;

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
        var currentEnableRecordingOnMotion = Camera?.Overrides?.Recording.EnableRecordingOnMotion;
        var currentShowBoundingBoxInGrid = Camera?.Overrides?.MotionDetection.BoundingBox.ShowInGrid;
        var currentShowBoundingBoxInFullScreen = Camera?.Overrides?.MotionDetection.BoundingBox.ShowInFullScreen;
        var currentBoundingBoxColor = Camera?.Overrides?.MotionDetection.BoundingBox.Color;
        var currentBoundingBoxThickness = Camera?.Overrides?.MotionDetection.BoundingBox.Thickness;
        var currentBoundingBoxMinArea = Camera?.Overrides?.MotionDetection.BoundingBox.MinArea;
        var currentMotionSensitivity = Camera?.Overrides?.MotionDetection.Sensitivity;
        var currentMotionMinimumChangePercent = Camera?.Overrides?.MotionDetection.MinimumChangePercent;
        var currentMotionAnalysisFrameRate = Camera?.Overrides?.MotionDetection.AnalysisFrameRate;
        var currentMotionAnalysisWidth = Camera?.Overrides?.MotionDetection.AnalysisWidth;
        var currentMotionAnalysisHeight = Camera?.Overrides?.MotionDetection.AnalysisHeight;
        var currentMotionCooldownSeconds = Camera?.Overrides?.MotionDetection.CooldownSeconds;
        var currentPostMotionDurationSeconds = Camera?.Overrides?.MotionDetection.PostMotionDurationSeconds;

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

        // Keep reference to old player and pipeline
        var oldPlayer = Player;
        var oldPipeline = mediaPipeline;

        // Create new player with updated settings FIRST
        var newPlayer = CreatePlayer(Camera);
        newPlayer.PropertyChanged += OnPlayerPropertyChanged;

        // Swap to new player (without intermediate null state)
        Player = newPlayer;
        mediaPipeline = new FlyleafLibMediaPipeline(newPlayer);

        // Now cleanup old player and pipeline
        oldPipeline?.Dispose();

        if (oldPlayer is not null)
        {
            oldPlayer.PropertyChanged -= OnPlayerPropertyChanged;
            oldPlayer.Dispose();
        }

        // Use the reconnect flow for the new player (includes timeout handling)
        isReconnecting = true;
        UpdateConnectionState(ConnectionState.Connecting);

        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            ReconnectPlayer);
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
            InitializeOverrideTracking(cameraConfig);
            return;
        }

        // Cleanup previous player and pipeline
        mediaPipeline?.Dispose();
        mediaPipeline = null;

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
        InitializeOverrideTracking(cameraConfig);

        CameraName = cameraConfig.Display.DisplayName;
        CameraDescription = cameraConfig.Display.Description ?? string.Empty;

        Player = CreatePlayer(cameraConfig);
        Player.PropertyChanged += OnPlayerPropertyChanged;
        mediaPipeline = new FlyleafLibMediaPipeline(Player);

        UpdateOverlayPosition(cameraConfig.Display.OverlayPosition);

        // Only auto-connect if enabled
        if (AutoConnectOnLoad)
        {
            // Use the same reconnect flow for initial connection
            // This ensures consistent timeout and retry behavior
            Reconnect();
        }
        else
        {
            // Stay disconnected until user manually connects
            UpdateConnectionState(ConnectionState.Disconnected);
        }
    }

    private Player CreatePlayer(CameraConfiguration camera)
    {
        var config = new Config
        {
            Player =
            {
                AutoPlay = true,
                MaxLatency = camera.Stream.MaxLatencyMs * 10000L,
                Stats = true, // Enable stats for stream health monitoring (FPSCurrent, FramesDisplayed, etc.)
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
                    // Skip Disconnected state during reconnection
                    // (player stops briefly before reopening)
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

            // Stop stream health monitoring
            StopStreamHealthCheck();

            // Stop motion detection when disconnected
            StopMotionDetection();

            // Stop timelapse capture when disconnected
            StopTimelapse();

            // Try auto-reconnect on failure (but not on manual disconnect)
            if (newState == ConnectionState.ConnectionFailed)
            {
                TryAutoReconnect();
            }
        }
        else if (newState == ConnectionState.Connected)
        {
            if (previousState is ConnectionState.Disconnected or ConnectionState.ConnectionFailed or ConnectionState.Connecting
                && ShowNotificationOnReconnect)
            {
                ShowNotification(
                    string.Format(CultureInfo.CurrentCulture, Translations.CameraConnected1, Camera?.Display.DisplayName ?? string.Empty));
            }

            // Start stream health monitoring to detect if stream goes offline
            StartStreamHealthCheck();

            // Auto-start recording on connect if enabled
            if (EnableRecordingOnConnect &&
                recordingService is not null &&
                mediaPipeline is not null &&
                Camera is not null &&
                RecordingState == RecordingState.Idle)
            {
                recordingService.StartRecording(Camera, mediaPipeline);
            }

            // Auto-start motion detection if enabled (for motion-triggered recording or bounding box display)
            if (ShouldRunMotionDetection)
            {
                StartMotionDetection();
            }

            // Auto-start timelapse capture if enabled
            StartTimelapse();
        }
    }

    private void TryAutoReconnect()
    {
        // Use effective value to respect per-camera override
        if (!GetEffectiveAutoReconnectOnFailure() || Camera is null)
        {
            isReconnecting = false; // Clear flag since we won't reconnect
            return;
        }

        // When AutoReconnectOnFailure is true, retry forever with configured delay
        var delaySeconds = GetEffectiveReconnectDelaySeconds();

        // Schedule auto-reconnect after delay (use effective value for per-camera override)
        autoReconnectTimer?.Stop();
        autoReconnectTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(delaySeconds),
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

    private void StartStreamHealthCheck()
    {
        StopStreamHealthCheck();

        if (Player is null)
        {
            return;
        }

        // Initialize tracking - use -1 to skip first check
        // This gives the stream time to establish
        lastFramesDisplayed = -1;
        staleStreamCheckCount = 0;

        // Check stream health every 5 seconds
        streamHealthTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5),
        };
        streamHealthTimer.Tick += OnStreamHealthTimerTick;
        streamHealthTimer.Start();
    }

    private void StopStreamHealthCheck()
    {
        if (streamHealthTimer is not null)
        {
            streamHealthTimer.Stop();
            streamHealthTimer.Tick -= OnStreamHealthTimerTick;
            streamHealthTimer = null;
        }

        staleStreamCheckCount = 0;
        lastFramesDisplayed = -1;
    }

    private void OnStreamHealthTimerTick(
        object? sender,
        EventArgs e)
    {
        // Don't check during reconnection
        if (Player?.Video is null || ConnectionState != ConnectionState.Connected || isReconnecting)
        {
            StopStreamHealthCheck();
            return;
        }

        var playerStatus = Player.Status;
        var currentFps = Player.Video.FPSCurrent;
        var currentFrames = (long)Player.Video.FramesDisplayed;

        // Only check if player is in a state where we expect video
        // Both Playing and Paused can have a frozen stream
        if (playerStatus != Status.Playing && playerStatus != Status.Paused)
        {
            return;
        }

        // First check - skip to allow stream to establish
        if (lastFramesDisplayed < 0)
        {
            lastFramesDisplayed = currentFrames;
            return;
        }

        // Detect stale stream using two indicators:
        // 1. FPS is very low (< 1) - indicates no/few frames being decoded
        // 2. FramesDisplayed not advancing - DWM not presenting new frames
        // Both must be true to avoid false positives
        var fpsIsLow = currentFps < 1.0;
        var framesNotAdvancing = currentFrames == lastFramesDisplayed;

        // Stream is stale if FPS is low AND frames aren't advancing
        var isStale = fpsIsLow && framesNotAdvancing;

        if (isStale)
        {
            staleStreamCheckCount++;

            // If stale for 3 consecutive checks (15 seconds), trigger reconnect
            if (staleStreamCheckCount >= 3)
            {
                StopStreamHealthCheck();

                // Set reconnecting flag to prevent OnPlayerPropertyChanged from
                // changing state when we stop the player. Keep it true until
                // the auto-reconnect timer fires to ignore all FlyleafLib status changes.
                isReconnecting = true;

                // Stop the player to prevent FlyleafLib from auto-recovering
                try
                {
                    Player?.Stop();
                }
                catch
                {
                    // Ignore stop errors
                }

                // Update state to ConnectionFailed (keep isReconnecting = true)
                // The flag will be managed by TryAutoReconnect/Reconnect
                UpdateConnectionState(ConnectionState.ConnectionFailed, "Stream stopped responding");
            }
        }
        else
        {
            // Stream is healthy - reset counter
            staleStreamCheckCount = 0;
        }

        lastFramesDisplayed = currentFrames;
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

    private void ShowToastNotification(string message)
    {
        if (toastNotificationService is null)
        {
            return;
        }

        toastNotificationService.ShowInformation(
            Camera?.Display.DisplayName ?? Translations.ApplicationTitle,
            message,
            useDesktop: true,
            expirationTime: TimeSpan.FromSeconds(5));
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

        // Capture references
        var capturedPlayer = Player;
        var capturedCamera = Camera;

        // Stop the player on the UI thread (FlyleafLib requires this)
        try
        {
            capturedPlayer.Stop();
        }
        catch
        {
            // Ignore Stop() errors - we'll try to Open() anyway
        }

        // Start status check timer before background operation
        StartReconnectStatusCheck();

        // Run Open on background thread to keep UI responsive
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

        // Reset check count (use instance field to avoid closure issues)
        reconnectCheckCount = 0;

        // Create timer on UI thread
        reconnectTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };

        reconnectTimer.Tick += OnReconnectTimerTick;
        reconnectTimer.Start();
    }

    private void OnReconnectTimerTick(
        object? sender,
        EventArgs e)
    {
        reconnectCheckCount++;

        // Calculate max checks (at least 10 seconds)
        var maxChecks = Math.Max(ConnectionTimeoutSeconds * 2, 20);

        if (!isReconnecting)
        {
            // Already handled by Status property change
            reconnectTimer?.Stop();
            return;
        }

        if (Player?.Status == Status.Playing)
        {
            isReconnecting = false;
            reconnectTimer?.Stop();
            UpdateConnectionState(ConnectionState.Connected);
            return;
        }

        if (Player?.Status == Status.Failed || reconnectCheckCount >= maxChecks)
        {
            isReconnecting = false;
            reconnectTimer?.Stop();
            UpdateConnectionState(ConnectionState.ConnectionFailed, Translations.ConnectionTimedOut);
        }
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
        if (Camera is null || mediaPipeline is null || recordingService is null)
        {
            return;
        }

        recordingService.StartRecording(Camera, mediaPipeline);
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
            $"IsActive={e.IsMotionActive}, BoundingBoxCount={e.BoundingBoxes.Count}, " +
            $"ChangePercentage={e.ChangePercentage:F2}%");

        Dispatcher.Invoke(() =>
        {
            // Update motion state based on IsMotionActive
            var wasMotionDetected = IsMotionDetected;
            IsMotionDetected = e.IsMotionActive;
            UpdateOverlayMotionIndicator();

            // Update bounding box overlay
            if (e.IsMotionActive && e.HasBoundingBoxes)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MotionDetection] Updating {e.BoundingBoxes.Count} bounding boxes, AnalysisRes={e.AnalysisWidth}x{e.AnalysisHeight}");
                UpdateMotionBoundingBoxes(e.BoundingBoxes, e.AnalysisWidth, e.AnalysisHeight);
            }
            else
            {
                UpdateMotionBoundingBoxes(boundingBoxes: null, e.AnalysisWidth, e.AnalysisHeight);
            }

            // Trigger motion recording if enabled and motion is active (use effective value for override)
            if (e.IsMotionActive && GetEffectiveEnableRecordingOnMotion() && recordingService is not null && mediaPipeline is not null)
            {
                recordingService.TriggerMotionRecording(Camera, mediaPipeline);
            }

            // If motion just stopped, schedule a delayed check to hide the bounding box
            if (wasMotionDetected && !e.IsMotionActive)
            {
                // Reset motion indicator after the configured post-motion duration
                var postMotionSeconds = GetEffectiveMotionPostDurationSeconds();
                var resetTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(postMotionSeconds),
                };
                resetTimer.Tick += (_, _) =>
                {
                    resetTimer.Stop();
                    if (motionDetectionService is null || !motionDetectionService.IsMotionDetected(Camera?.Id ?? Guid.Empty))
                    {
                        IsMotionDetected = false;
                        UpdateOverlayMotionIndicator();
                        UpdateMotionBoundingBoxes(boundingBoxes: null);
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

    private void UpdateMotionBoundingBoxes(
        IReadOnlyList<Rect>? boundingBoxes,
        int analysisWidth = 320,
        int analysisHeight = 240)
    {
        var motionOverlay = GetMotionBoundingBoxOverlay();
        if (motionOverlay is null)
        {
            System.Diagnostics.Debug.WriteLine("[MotionDetection] UpdateMotionBoundingBoxes - overlay not found!");
            return;
        }

        // Update the overlay's analysis resolution to match the event's resolution
        // This ensures correct coordinate scaling
        motionOverlay.AnalysisWidth = analysisWidth;
        motionOverlay.AnalysisHeight = analysisHeight;

        // Set the video stream dimensions for letterbox-aware coordinate mapping
        if (Player?.Video is not null && Player.Video.Width > 0 && Player.Video.Height > 0)
        {
            motionOverlay.VideoWidth = Player.Video.Width;
            motionOverlay.VideoHeight = Player.Video.Height;
        }

        // Get the video container size for coordinate mapping
        var containerSize = new Size(VideoPlayer.ActualWidth, VideoPlayer.ActualHeight);
        if ((containerSize.Width <= 0 || containerSize.Height <= 0) &&
            VideoPlayer.Overlay is not null)
        {
            // Try to get size from the overlay window
            containerSize = new Size(VideoPlayer.Overlay.ActualWidth, VideoPlayer.Overlay.ActualHeight);
        }

        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] UpdateMotionBoundingBoxes - overlay.IsOverlayEnabled={motionOverlay.IsOverlayEnabled}, " +
            $"containerSize={containerSize.Width}x{containerSize.Height}, analysisRes={analysisWidth}x{analysisHeight}, boxCount={boundingBoxes?.Count ?? 0}");

        motionOverlay.UpdateBoundingBoxes(boundingBoxes, containerSize);
    }

    /// <summary>
    /// Starts motion detection for this camera if enabled (for recording or bounding box display).
    /// </summary>
    public void StartMotionDetection()
    {
        System.Diagnostics.Debug.WriteLine(
            $"[MotionDetection] StartMotionDetection called for '{Camera?.Display.DisplayName ?? "null"}' - " +
            $"Camera={Camera is not null}, Pipeline={mediaPipeline is not null}, Service={motionDetectionService is not null}, " +
            $"EnableRecordingOnMotion={GetEffectiveEnableRecordingOnMotion()}, ShowBoundingBoxInGrid={GetEffectiveShowBoundingBoxInGrid()}, " +
            $"ShouldRun={ShouldRunMotionDetection}");

        if (Camera is null || mediaPipeline is null || motionDetectionService is null || !ShouldRunMotionDetection)
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
            $"OverrideMinArea={Camera.Overrides?.MotionDetection.BoundingBox.MinArea}");
        motionDetectionService.StartDetection(Camera.Id, mediaPipeline, settings);

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

    /// <summary>
    /// Starts timelapse capture for this camera if enabled.
    /// </summary>
    public void StartTimelapse()
    {
        if (Camera is null || mediaPipeline is null || timelapseService is null)
        {
            return;
        }

        if (!timelapseService.GetEffectiveEnabled(Camera))
        {
            return;
        }

        timelapseService.StartCapture(Camera, mediaPipeline);
    }

    /// <summary>
    /// Stops timelapse capture for this camera.
    /// </summary>
    public void StopTimelapse()
    {
        if (Camera is null || timelapseService is null)
        {
            return;
        }

        timelapseService.StopCapture(Camera.Id);
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