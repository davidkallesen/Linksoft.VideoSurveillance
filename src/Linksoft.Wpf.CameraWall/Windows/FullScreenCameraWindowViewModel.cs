// ReSharper disable GCSuppressFinalizeForTypeWithoutDestructor
namespace Linksoft.Wpf.CameraWall.Windows;

/// <summary>
/// ViewModel for the fullscreen camera window.
/// </summary>
public sealed partial class FullScreenCameraWindowViewModel : ViewModelDialogBase, IDisposable
{
    private readonly CameraConfiguration camera;
    private readonly IMotionDetectionService? motionDetectionService;
    private readonly bool ownsPlayer;
    private DispatcherTimer? overlayHideTimer;
    private bool disposed;

    [ObservableProperty]
    private Player? player;

    [ObservableProperty]
    private string cameraName = string.Empty;

    [ObservableProperty]
    private string cameraDescription = string.Empty;

    [ObservableProperty]
    private ConnectionState connectionState = ConnectionState.Disconnected;

    [ObservableProperty]
    private bool isOverlayVisible = true;

    [ObservableProperty]
    private bool showOverlayTitle = true;

    [ObservableProperty]
    private bool showOverlayDescription = true;

    [ObservableProperty]
    private bool showOverlayTime;

    [ObservableProperty]
    private bool showOverlayConnectionStatus = true;

    [ObservableProperty]
    private double overlayOpacity = 0.7;

    [ObservableProperty]
    private OverlayPosition overlayPosition = OverlayPosition.TopLeft;

    // Bounding box settings
    [ObservableProperty]
    private bool showBoundingBoxInFullScreen;

    [ObservableProperty]
    private string boundingBoxColor = "Red";

    [ObservableProperty]
    private int boundingBoxThickness = 2;

    [ObservableProperty]
    private double boundingBoxSmoothing = 0.3;

    [ObservableProperty]
    private Rect? currentBoundingBox;

    [ObservableProperty]
    private int analysisWidth = 320;

    [ObservableProperty]
    private int analysisHeight = 240;

    public FullScreenCameraWindowViewModel(
        CameraConfiguration camera,
        bool showOverlayTitle,
        bool showOverlayDescription,
        bool showOverlayTime,
        bool showOverlayConnectionStatus,
        double overlayOpacity,
        OverlayPosition overlayPosition,
        bool showBoundingBoxInFullScreen = false,
        string boundingBoxColor = "Red",
        int boundingBoxThickness = 2,
        double boundingBoxSmoothing = 0.3,
        IMotionDetectionService? motionDetectionService = null,
        Player? existingPlayer = null)
    {
        ArgumentNullException.ThrowIfNull(camera);

        this.camera = camera;
        this.motionDetectionService = motionDetectionService;
        CameraName = camera.Display.DisplayName;
        CameraDescription = camera.Display.Description ?? string.Empty;

        // Apply overlay settings (per-camera overrides merged with app defaults)
        ShowOverlayTitle = showOverlayTitle;
        ShowOverlayDescription = showOverlayDescription;
        ShowOverlayTime = showOverlayTime;
        ShowOverlayConnectionStatus = showOverlayConnectionStatus;
        OverlayOpacity = overlayOpacity;
        OverlayPosition = overlayPosition;

        // Apply bounding box settings
        ShowBoundingBoxInFullScreen = showBoundingBoxInFullScreen;
        BoundingBoxColor = boundingBoxColor;
        BoundingBoxThickness = boundingBoxThickness;
        BoundingBoxSmoothing = boundingBoxSmoothing;

        // Get analysis resolution from motion detection service
        if (motionDetectionService is not null)
        {
            var (width, height) = motionDetectionService.GetAnalysisResolution(camera.Id);
            AnalysisWidth = width;
            AnalysisHeight = height;
            motionDetectionService.MotionDetected += OnMotionDetected;
        }

        // Use existing player (borrowed from CameraTile) or create a new one
        if (existingPlayer is not null)
        {
            Player = existingPlayer;
            Player.PropertyChanged += OnPlayerPropertyChanged;

            // Enable audio for fullscreen playback
            existingPlayer.Config.Audio.Enabled = true;

            // Update connection state based on current player status
            ConnectionState = existingPlayer.Status switch
            {
                Status.Playing or Status.Paused => ConnectionState.Connected,
                Status.Opening => ConnectionState.Connecting,
                Status.Stopped or Status.Ended => ConnectionState.Disconnected,
                Status.Failed => ConnectionState.ConnectionFailed,
                _ => ConnectionState.Disconnected,
            };

            ownsPlayer = false;
        }
        else
        {
            InitializePlayer();
            ownsPlayer = true;
        }

        StartOverlayHideTimer();
    }

    /// <summary>
    /// Occurs when the window requests to be closed.
    /// </summary>
    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    /// <summary>
    /// Called when the mouse moves in the window.
    /// </summary>
    public void OnMouseMoved()
    {
        IsOverlayVisible = true;
        overlayHideTimer?.Stop();
        overlayHideTimer?.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    private void InitializePlayer()
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
                Enabled = true, // Enable audio for fullscreen
            },
        };

        Player = new Player(config);
        Player.PropertyChanged += OnPlayerPropertyChanged;

        // Defer stream opening to avoid blocking UI during window creation
        var uri = camera
            .BuildUri()
            .ToString();

        _ = Task.Run(() =>
        {
            try
            {
                Player?.Open(uri);
            }
            catch
            {
                // Status will be updated via PropertyChanged
            }
        });
    }

    private void OnPlayerPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Player.Status))
        {
            return;
        }

        var status = Player?.Status;
        ConnectionState = status switch
        {
            Status.Playing or Status.Paused => ConnectionState.Connected,
            Status.Opening => ConnectionState.Connecting,
            Status.Stopped or Status.Ended => ConnectionState.Disconnected,
            Status.Failed => ConnectionState.ConnectionFailed,
            _ => ConnectionState.Disconnected,
        };
    }

    private void OnMotionDetected(
        object? sender,
        MotionDetectedEventArgs e)
    {
        // Only handle events for this camera
        if (e.CameraId != camera.Id)
        {
            return;
        }

        // Update bounding box on UI thread
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (e.IsMotionActive && e.BoundingBox.HasValue)
            {
                CurrentBoundingBox = e.BoundingBox;
            }
            else
            {
                CurrentBoundingBox = null;
            }
        });
    }

    private void StartOverlayHideTimer()
    {
        overlayHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3),
        };

        overlayHideTimer.Tick += (_, _) =>
        {
            IsOverlayVisible = false;
            overlayHideTimer.Stop();
        };

        overlayHideTimer.Start();
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            overlayHideTimer?.Stop();
            overlayHideTimer = null;

            if (motionDetectionService is not null)
            {
                motionDetectionService.MotionDetected -= OnMotionDetected;
            }

            if (Player is not null)
            {
                Player.PropertyChanged -= OnPlayerPropertyChanged;

                // Only dispose the player if we own it (created it ourselves)
                // If it was borrowed from CameraTile, it will be returned and reused
                if (ownsPlayer)
                {
                    Player.Dispose();
                }

                Player = null;
            }
        }

        disposed = true;
    }
}