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

    [DependencyProperty(DefaultValue = false)]
    private bool enableRecordingOnMotion;

    [DependencyProperty(DefaultValue = false)]
    private bool enableRecordingOnConnect;

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
    private DispatcherTimer? reconnectTimer;
    private DispatcherTimer? autoReconnectTimer;
    private int currentReconnectAttempt;

    // Track previous performance override values to detect changes that require reconnection
    private string? previousOverrideVideoQuality;
    private bool? previousOverrideHardwareAcceleration;

    /// <summary>
    /// Occurs when a full screen request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? FullScreenRequested;

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
            return;
        }

        // Initialize tracked override values for change detection
        previousOverrideVideoQuality = cameraConfig.Overrides?.VideoQuality;
        previousOverrideHardwareAcceleration = cameraConfig.Overrides?.HardwareAcceleration;

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
            FullScreenRequested?.Invoke(this, Camera);
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
        if (!string.IsNullOrEmpty(SnapshotPath) && Directory.Exists(SnapshotPath))
        {
            dialog.InitialDirectory = SnapshotPath;
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

        Dispatcher.Invoke(() =>
        {
            IsMotionDetected = true;
            UpdateOverlayMotionIndicator();

            // Trigger motion recording if enabled
            if (EnableRecordingOnMotion && recordingService is not null && Player is not null)
            {
                recordingService.TriggerMotionRecording(Camera, Player);
            }

            // Reset motion indicator after a short delay
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
                }
            };
            resetTimer.Start();
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

    /// <summary>
    /// Starts motion detection for this camera if enabled.
    /// </summary>
    public void StartMotionDetection()
    {
        if (Camera is null || Player is null || motionDetectionService is null || !EnableRecordingOnMotion)
        {
            return;
        }

        var settings = new MotionDetectionSettings();

        // Apply overrides if present
        if (Camera.Overrides?.MotionSensitivity.HasValue == true)
        {
            settings.Sensitivity = Camera.Overrides.MotionSensitivity.Value;
        }

        if (Camera.Overrides?.PostMotionDurationSeconds.HasValue == true)
        {
            settings.PostMotionDurationSeconds = Camera.Overrides.PostMotionDurationSeconds.Value;
        }

        motionDetectionService.StartDetection(Camera.Id, Player, settings);
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