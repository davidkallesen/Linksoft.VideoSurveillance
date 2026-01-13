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
    private string? snapshotDirectory;

    private bool disposed;
    private bool isReconnecting;
    private bool isSwapping;
    private bool isDragging;
    private Point dragStartPoint;
    private ConnectionState previousState = ConnectionState.Disconnected;
    private CameraOverlay? cachedOverlay;
    private DispatcherTimer? reconnectTimer;

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
    /// Initializes a new instance of the <see cref="CameraTile"/> class.
    /// </summary>
    public CameraTile()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        VideoPlayer.OverlayCreated += OnOverlayCreated;
        VideoPlayer.SurfaceCreated += OnSurfaceCreated;
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
            reconnectTimer?.Stop();
            reconnectTimer = null;

            Loaded -= OnLoaded;
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
                case nameof(CameraConfiguration.DisplayName):
                    CameraName = Camera?.DisplayName ?? string.Empty;
                    break;
                case nameof(CameraConfiguration.Description):
                    CameraDescription = Camera?.Description ?? string.Empty;
                    break;
            }
        });
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

            CameraName = cameraConfig.DisplayName;
            CameraDescription = cameraConfig.Description ?? string.Empty;
            UpdateOverlayPosition(cameraConfig.OverlayPosition);
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
            return;
        }

        CameraName = cameraConfig.DisplayName;
        CameraDescription = cameraConfig.Description ?? string.Empty;

        // Create new player
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

        Player = new Player(config);
        Player.PropertyChanged += OnPlayerPropertyChanged;

        UpdateOverlayPosition(cameraConfig.OverlayPosition);

        // Start streaming
        Play();
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
        const int maxChecks = 30; // Check for up to 15 seconds (30 * 500ms)

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

    [RelayCommand(CanExecute = nameof(CanExecuteCameraCommand))]
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

    private bool CanExecuteSnapshot()
        => Camera is not null && ConnectionState == ConnectionState.Connected;

    [RelayCommand(CanExecute = nameof(CanExecuteSnapshot))]
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
            FileName = $"{Camera.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}",
        };

        // Use configured snapshot directory as initial directory if available
        if (!string.IsNullOrEmpty(SnapshotDirectory) && Directory.Exists(SnapshotDirectory))
        {
            dialog.InitialDirectory = SnapshotDirectory;
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