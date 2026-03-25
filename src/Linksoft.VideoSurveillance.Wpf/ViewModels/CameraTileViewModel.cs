namespace Linksoft.VideoSurveillance.Wpf.ViewModels;

/// <summary>
/// Per-camera view model for a single tile in the live view grid.
/// Manages HLS streaming via the API server and hub events.
/// </summary>
public sealed partial class CameraTileViewModel : ViewModelBase, IDisposable
{
    private readonly SurveillanceHubService hubService;
    private readonly string apiBaseAddress;
    private bool disposed;

    [ObservableProperty]
    private Guid cameraId;

    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string connectionState = "disconnected";

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

    [ObservableProperty]
    private IVideoPlayer? player;

    [ObservableProperty]
    private bool isStreaming;

    public CameraTileViewModel(
        IVideoPlayerFactory videoPlayerFactory,
        SurveillanceHubService hubService,
        string apiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(videoPlayerFactory);
        ArgumentNullException.ThrowIfNull(hubService);

        this.hubService = hubService;
        this.apiBaseAddress = apiBaseAddress;

        Player = videoPlayerFactory.Create();
        Player.StateChanged += OnPlayerStateChanged;

        hubService.OnStreamStarted += OnStreamStarted;
        hubService.OnConnectionStateChanged += OnHubConnectionStateChanged;
        hubService.OnRecordingStateChanged += OnHubRecordingStateChanged;
        hubService.OnMotionDetected += OnHubMotionDetected;
    }

    public Task StartStreamAsync()
        => hubService.StartStreamAsync(CameraId);

    public Task StopStreamAsync()
    {
        Player?.Close();

        _ = Application.Current?.Dispatcher.InvokeAsync(() => IsStreaming = false);

        return hubService.StopStreamAsync(CameraId);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        hubService.OnStreamStarted -= OnStreamStarted;
        hubService.OnConnectionStateChanged -= OnHubConnectionStateChanged;
        hubService.OnRecordingStateChanged -= OnHubRecordingStateChanged;
        hubService.OnMotionDetected -= OnHubMotionDetected;

        if (Player is not null)
        {
            Player.StateChanged -= OnPlayerStateChanged;
            Player.Dispose();
            Player = null;
        }
    }

    private void OnStreamStarted(SurveillanceHubService.StreamStartedEvent e)
    {
        if (e.CameraId != CameraId)
        {
            return;
        }

        var fullUrl = $"{apiBaseAddress.TrimEnd('/')}{e.PlaylistUrl}";
        var uri = new Uri(fullUrl);
        var options = new StreamOptions
        {
            UseLowLatencyMode = true,
            HardwareAcceleration = true,
        };

        _ = Task.Run(() =>
        {
            try
            {
                Player?.Open(uri, options);
            }
            catch
            {
                // State will be updated via StateChanged
            }
        });

        _ = Application.Current?.Dispatcher.InvokeAsync(() => IsStreaming = true);
    }

    private void OnPlayerStateChanged(
        object? sender,
        PlayerStateChangedEventArgs e)
    {
        _ = Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            ConnectionState = e.NewState switch
            {
                PlayerState.Playing => "connected",
                PlayerState.Opening => "connecting",
                PlayerState.Stopped => "disconnected",
                PlayerState.Error => "error",
                _ => "disconnected",
            };

            if (e.NewState is PlayerState.Stopped or PlayerState.Error)
            {
                IsStreaming = false;
            }
        });
    }

    private void OnHubConnectionStateChanged(
        SurveillanceHubService.ConnectionStateEvent e)
    {
        if (e.CameraId != CameraId)
        {
            return;
        }

        _ = Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            ConnectionState = e.NewState.ToLowerInvariant();
        });
    }

    private void OnHubRecordingStateChanged(
        SurveillanceHubService.RecordingStateEvent e)
    {
        if (e.CameraId != CameraId)
        {
            return;
        }

        _ = Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            IsRecording = string.Equals(e.NewState, "recording", StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(e.NewState, "recordingMotion", StringComparison.OrdinalIgnoreCase);
        });
    }

    private void OnHubMotionDetected(
        SurveillanceHubService.MotionDetectedEvent e)
    {
        if (e.CameraId != CameraId)
        {
            return;
        }

        _ = Application.Current?.Dispatcher.InvokeAsync(() =>
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
}