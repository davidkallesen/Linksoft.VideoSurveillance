// ReSharper disable GCSuppressFinalizeForTypeWithoutDestructor
namespace Linksoft.Wpf.CameraWall.Windows;

/// <summary>
/// ViewModel for the fullscreen camera window.
/// </summary>
public sealed partial class FullScreenCameraWindowViewModel : ViewModelDialogBase, IDisposable
{
    private readonly CameraConfiguration camera;
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

    public FullScreenCameraWindowViewModel(
        CameraConfiguration camera,
        bool showOverlayTitle,
        bool showOverlayDescription,
        bool showOverlayTime,
        bool showOverlayConnectionStatus,
        double overlayOpacity,
        OverlayPosition overlayPosition)
    {
        ArgumentNullException.ThrowIfNull(camera);

        this.camera = camera;
        CameraName = camera.Display.DisplayName;
        CameraDescription = camera.Display.Description ?? string.Empty;

        // Apply overlay settings (per-camera overrides merged with app defaults)
        ShowOverlayTitle = showOverlayTitle;
        ShowOverlayDescription = showOverlayDescription;
        ShowOverlayTime = showOverlayTime;
        ShowOverlayConnectionStatus = showOverlayConnectionStatus;
        OverlayOpacity = overlayOpacity;
        OverlayPosition = overlayPosition;

        InitializePlayer();
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

            if (Player is not null)
            {
                Player.PropertyChanged -= OnPlayerPropertyChanged;
                Player.Dispose();
                Player = null;
            }
        }

        disposed = true;
    }
}