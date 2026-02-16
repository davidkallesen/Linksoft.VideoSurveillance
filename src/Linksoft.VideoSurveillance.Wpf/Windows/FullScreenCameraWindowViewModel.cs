// ReSharper disable GCSuppressFinalizeForTypeWithoutDestructor
namespace Linksoft.VideoSurveillance.Wpf.Windows;

/// <summary>
/// ViewModel for the fullscreen camera window.
/// Simplified version — no direct RTSP connection or local motion detection.
/// The player is borrowed from the CameraTile (not owned by this VM).
/// </summary>
public sealed partial class FullScreenCameraWindowViewModel : ViewModelBase, IDisposable
{
    private readonly SurveillanceHubService hubService;
    private readonly Guid cameraId;
    private DispatcherTimer? overlayHideTimer;
    private bool disposed;

    [ObservableProperty]
    private IVideoPlayer? player;

    [ObservableProperty]
    private string cameraName = string.Empty;

    [ObservableProperty]
    private string cameraDescription = string.Empty;

    [ObservableProperty]
    private string connectionState = "disconnected";

    [ObservableProperty]
    private bool isOverlayVisible = true;

    [ObservableProperty]
    private bool isRecording;

    [ObservableProperty]
    private bool isMotionDetected;

    [ObservableProperty]
    private IReadOnlyList<Rect> currentBoundingBoxes = [];

    [ObservableProperty]
    private int analysisWidth = 320;

    [ObservableProperty]
    private int analysisHeight = 240;

    public FullScreenCameraWindowViewModel(
        SurveillanceHubService hubService,
        Guid cameraId,
        IVideoPlayer? existingPlayer,
        string cameraName,
        string cameraDescription)
    {
        ArgumentNullException.ThrowIfNull(hubService);

        this.hubService = hubService;
        this.cameraId = cameraId;
        CameraName = cameraName;
        CameraDescription = cameraDescription;

        // Borrow existing player from tile (we do NOT own it)
        if (existingPlayer is not null)
        {
            Player = existingPlayer;
            Player.StateChanged += OnPlayerStateChanged;

            ConnectionState = existingPlayer.State switch
            {
                PlayerState.Playing => "connected",
                PlayerState.Opening => "connecting",
                PlayerState.Stopped => "disconnected",
                PlayerState.Error => "error",
                _ => "disconnected",
            };
        }

        hubService.OnConnectionStateChanged += OnHubConnectionStateChanged;
        hubService.OnRecordingStateChanged += OnHubRecordingStateChanged;
        hubService.OnMotionDetected += OnHubMotionDetected;

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

    private void OnPlayerStateChanged(
        object? sender,
        PlayerStateChangedEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            ConnectionState = e.NewState switch
            {
                PlayerState.Playing => "connected",
                PlayerState.Opening => "connecting",
                PlayerState.Stopped => "disconnected",
                PlayerState.Error => "error",
                _ => "disconnected",
            };
        });
    }

    private void OnHubConnectionStateChanged(
        SurveillanceHubService.ConnectionStateEvent e)
    {
        if (e.CameraId != cameraId)
        {
            return;
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
            ConnectionState = e.NewState.ToLowerInvariant();
        });
    }

    private void OnHubRecordingStateChanged(
        SurveillanceHubService.RecordingStateEvent e)
    {
        if (e.CameraId != cameraId)
        {
            return;
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
            IsRecording = string.Equals(e.NewState, "recording", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(e.NewState, "recordingMotion", StringComparison.OrdinalIgnoreCase);
        });
    }

    private void OnHubMotionDetected(
        SurveillanceHubService.MotionDetectedEvent e)
    {
        if (e.CameraId != cameraId)
        {
            return;
        }

        Application.Current?.Dispatcher.Invoke(() =>
        {
            IsMotionDetected = e.IsMotionActive;
            AnalysisWidth = e.AnalysisWidth;
            AnalysisHeight = e.AnalysisHeight;

            if (e.IsMotionActive && e.BoundingBoxes.Count > 0)
            {
                CurrentBoundingBoxes = e.BoundingBoxes
                    .Select(b => new Rect(b.X, b.Y, b.Width, b.Height))
                    .ToList();
            }
            else
            {
                CurrentBoundingBoxes = [];
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

            hubService.OnConnectionStateChanged -= OnHubConnectionStateChanged;
            hubService.OnRecordingStateChanged -= OnHubRecordingStateChanged;
            hubService.OnMotionDetected -= OnHubMotionDetected;

            if (Player is not null)
            {
                Player.StateChanged -= OnPlayerStateChanged;

                // Do NOT dispose the player — it is borrowed from the tile
                Player = null;
            }
        }

        disposed = true;
    }
}
