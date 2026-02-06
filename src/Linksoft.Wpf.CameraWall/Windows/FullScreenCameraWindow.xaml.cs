namespace Linksoft.Wpf.CameraWall.Windows;

/// <summary>
/// Fullscreen window for displaying a single camera stream.
/// </summary>
public partial class FullScreenCameraWindow : IDisposable
{
    private const int WmKeyDown = 0x0100;
    private const int WmRightButtonUp = 0x0205;
    private const int VkEscape = 0x1B;

    private readonly FullScreenCameraWindowViewModel viewModel;
    private DispatcherTimer? timeUpdateTimer;
    private Point lastMousePosition;
    private MotionBoundingBoxOverlay? cachedMotionOverlay;
    private bool disposed;

    public FullScreenCameraWindow(FullScreenCameraWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        this.viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CloseRequested += OnCloseRequested;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += OnWindowClosed;

        // Use InputManager to capture mouse input before FlyleafHost intercepts it
        InputManager.Current.PreProcessInput += OnPreProcessInput;

        // Use ComponentDispatcher to capture keyboard at Win32 level (FlyleafHost uses HwndHost)
        ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;

        // Start time update timer for overlay
        StartTimeUpdateTimer();
    }

    private void StartTimeUpdateTimer()
    {
        // Update time display initially
        UpdateTimeDisplay();

        // Set overlay background with configured opacity and position
        ApplyOverlayBackground();
        ApplyOverlayPosition();

        // Create timer to update time every second
        timeUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        timeUpdateTimer.Tick += (_, _) => UpdateTimeDisplay();
        timeUpdateTimer.Start();
    }

    private void UpdateTimeDisplay()
    {
        TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
    }

    private void ApplyOverlayBackground()
    {
        // Apply overlay opacity to the background color
        var opacity = viewModel.OverlayOpacity;
        var color = Color.FromArgb((byte)(opacity * 255), 0, 0, 0);
        OverlayBorder.Background = new SolidColorBrush(color);
    }

    private void ApplyOverlayPosition()
    {
        var position = viewModel.OverlayPosition;

        OverlayBorder.HorizontalAlignment = position switch
        {
            OverlayPosition.TopLeft or OverlayPosition.BottomLeft => HorizontalAlignment.Left,
            OverlayPosition.TopRight or OverlayPosition.BottomRight => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };

        OverlayBorder.VerticalAlignment = position switch
        {
            OverlayPosition.TopLeft or OverlayPosition.TopRight => VerticalAlignment.Top,
            OverlayPosition.BottomLeft or OverlayPosition.BottomRight => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top,
        };
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FullScreenCameraWindowViewModel.CurrentBoundingBoxes))
        {
            UpdateMotionBoundingBoxes();
        }
    }

    private void UpdateMotionBoundingBoxes()
    {
        var motionOverlay = GetMotionBoundingBoxOverlay();
        if (motionOverlay is null)
        {
            return;
        }

        // Set analysis resolution
        motionOverlay.AnalysisWidth = viewModel.AnalysisWidth;
        motionOverlay.AnalysisHeight = viewModel.AnalysisHeight;

        // Set the video stream dimensions for letterbox-aware coordinate mapping
        if (viewModel.Player?.Video is not null && viewModel.Player.Video.Width > 0 && viewModel.Player.Video.Height > 0)
        {
            motionOverlay.VideoWidth = viewModel.Player.Video.Width;
            motionOverlay.VideoHeight = viewModel.Player.Video.Height;
        }

        // Get the video container size for coordinate mapping
        var containerSize = new Size(VideoPlayer.ActualWidth, VideoPlayer.ActualHeight);
        if ((containerSize.Width <= 0 || containerSize.Height <= 0) &&
            VideoPlayer.Overlay is not null)
        {
            // Try to get size from the overlay window
            containerSize = new Size(VideoPlayer.Overlay.ActualWidth, VideoPlayer.Overlay.ActualHeight);
        }

        motionOverlay.UpdateBoundingBoxes(viewModel.CurrentBoundingBoxes, containerSize);
    }

    private MotionBoundingBoxOverlay? GetMotionBoundingBoxOverlay()
    {
        // Return cached reference if available
        if (cachedMotionOverlay is not null)
        {
            return cachedMotionOverlay;
        }

        // Try to find MotionBoundingBoxOverlay in overlay window content
        if (VideoPlayer.Overlay is null)
        {
            return null;
        }

        if (VideoPlayer.Overlay.Content is MotionBoundingBoxOverlay directOverlay)
        {
            cachedMotionOverlay = directOverlay;
        }
        else if (VideoPlayer.Overlay.Content is DependencyObject content)
        {
            // Search visual tree
            cachedMotionOverlay = content
                .FindChildren<MotionBoundingBoxOverlay>()
                .FirstOrDefault();
        }

        // If still not found, try to find in overlay window itself
        cachedMotionOverlay ??= VideoPlayer.Overlay
            .FindChildren<MotionBoundingBoxOverlay>()
            .FirstOrDefault();

        return cachedMotionOverlay;
    }

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
            timeUpdateTimer?.Stop();
            timeUpdateTimer = null;
            ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Closed -= OnWindowClosed;
            viewModel.Dispose();
        }

        disposed = true;
    }

    private void OnCloseRequested(
        object? sender,
        DialogClosedEventArgs e)
        => Close();

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
        => Dispose();

    private void OnPreProcessInput(
        object sender,
        PreProcessInputEventArgs e)
    {
        // Skip if disposed or not active
        if (disposed || !IsActive)
        {
            return;
        }

        try
        {
            switch (e.StagingItem.Input)
            {
                case MouseButtonEventArgs mouseButtonArgs:
                    HandleMouseButtonInput(mouseButtonArgs);
                    break;
                case MouseEventArgs mouseArgs:
                    HandleMouseInput(mouseArgs);
                    break;
            }
        }
        catch
        {
            // Silently ignore any errors to avoid interfering with other windows
        }
    }

    private void HandleMouseInput(MouseEventArgs e)
    {
        var currentPosition = e.GetPosition(this);

        if (currentPosition == lastMousePosition)
        {
            return;
        }

        lastMousePosition = currentPosition;
        viewModel.OnMouseMoved();
    }

    private void HandleMouseButtonInput(MouseButtonEventArgs e)
    {
        // Show context menu on right-click release
        if (e is { ChangedButton: MouseButton.Right, ButtonState: MouseButtonState.Released })
        {
            ShowContextMenu();
        }
    }

    private void OnThreadFilterMessage(
        ref MSG msg,
        ref bool handled)
    {
        // Skip if disposed, already handled, or not active
        if (disposed || handled || !IsActive)
        {
            return;
        }

        try
        {
            switch (msg.message)
            {
                // Handle ESC key at Win32 message level
                case WmKeyDown when (int)msg.wParam == VkEscape:
                    viewModel.CloseCommand.Execute(parameter: null);
                    handled = true;
                    break;

                // Handle right-click to show context menu
                case WmRightButtonUp:
                    ShowContextMenu();
                    handled = true;
                    break;
            }
        }
        catch
        {
            // Silently ignore any errors to avoid interfering with other windows
        }
    }

    private void ShowContextMenu()
    {
        // Create context menu dynamically to avoid binding conflicts
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(new MenuItem
        {
            Header = Translations.Close,
            Command = viewModel.CloseCommand,
        });
        contextMenu.IsOpen = true;
    }
}