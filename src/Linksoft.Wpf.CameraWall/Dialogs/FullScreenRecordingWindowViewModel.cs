// ReSharper disable GCSuppressFinalizeForTypeWithoutDestructor
namespace Linksoft.Wpf.CameraWall.Dialogs;

/// <summary>
/// ViewModel for the fullscreen recording playback window.
/// </summary>
public sealed partial class FullScreenRecordingWindowViewModel : ViewModelDialogBase, IDisposable
{
    private static readonly double[] SpeedOptions = [1.0, 2.0, 4.0, 8.0, 16.0];

    private readonly string filePath;
    private readonly DateTime? recordingStartTime;
    private readonly PlaybackOverlaySettings overlaySettings;
    private DispatcherTimer? overlayHideTimer;
    private DispatcherTimer? positionUpdateTimer;
    private bool disposed;
    private bool isSeeking;
    private bool isUpdatingPositionFromPlayer;
    private int currentSpeedIndex;

    [ObservableProperty]
    private Player? player;

    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private bool isOverlayVisible = true;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private string positionText = "00:00";

    [ObservableProperty]
    private string durationText = "00:00";

    [ObservableProperty]
    private double seekPosition;

    [ObservableProperty]
    private double seekMaximum = 100;

    [ObservableProperty]
    private bool canSeek;

    [ObservableProperty]
    private double playbackSpeed = 1.0;

    [ObservableProperty]
    private string speedText = "1x";

    [ObservableProperty]
    private string recordingTimeText = string.Empty;

    [ObservableProperty]
    private bool showFilename = true;

    [ObservableProperty]
    private SolidColorBrush filenameColor = Brushes.White;

    [ObservableProperty]
    private bool showTimestamp = true;

    [ObservableProperty]
    private SolidColorBrush timestampColor = Brushes.White;

    /// <summary>
    /// Initializes a new instance of the <see cref="FullScreenRecordingWindowViewModel"/> class.
    /// </summary>
    /// <param name="filePath">The path to the recording file.</param>
    /// <param name="fileName">The display name of the recording file.</param>
    /// <param name="overlaySettings">The playback overlay settings.</param>
    public FullScreenRecordingWindowViewModel(
        string filePath,
        string fileName,
        PlaybackOverlaySettings? overlaySettings = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        this.filePath = filePath;
        this.overlaySettings = overlaySettings ?? new PlaybackOverlaySettings();
        FileName = fileName;
        recordingStartTime = ParseRecordingStartTime(fileName);

        // Apply overlay settings
        ShowFilename = this.overlaySettings.ShowFilename;
        FilenameColor = ParseColor(this.overlaySettings.FilenameColor, Brushes.White);
        ShowTimestamp = this.overlaySettings.ShowTimestamp;
        TimestampColor = ParseColor(this.overlaySettings.TimestampColor, new SolidColorBrush(Color.FromRgb(255, 215, 0)));

        UpdateRecordingTimeText(0);

        InitializePlayer();
        StartOverlayHideTimer();
        StartPositionUpdateTimer();
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

    /// <summary>
    /// Called when the seek slider drag starts.
    /// </summary>
    public void OnSeekStarted()
    {
        isSeeking = true;
    }

    /// <summary>
    /// Called when the seek slider drag ends.
    /// </summary>
    public void OnSeekCompleted()
    {
        if (isSeeking && Player is not null)
        {
            Player.CurTime = (long)SeekPosition;
            isSeeking = false;
        }
    }

    /// <summary>
    /// Called when the seek slider value changes.
    /// </summary>
    public void OnSeekValueChanged()
    {
        // Only seek if the change came from user interaction, not from the timer
        if (!isUpdatingPositionFromPlayer && Player is not null)
        {
            Player.CurTime = (long)SeekPosition;
            UpdateRecordingTimeText((long)SeekPosition);
        }
    }

    private void UpdateRecordingTimeText(long positionTicks)
    {
        if (recordingStartTime.HasValue)
        {
            var currentTime = recordingStartTime.Value.AddTicks(positionTicks);
            RecordingTimeText = currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        else
        {
            // Fallback to playback position if we couldn't parse the start time
            RecordingTimeText = FormatDuration(positionTicks);
        }
    }

    private static DateTime? ParseRecordingStartTime(string fileName)
    {
        // Filename format: {CameraName}_{yyyyMMdd_HHmmss}.ext
        // Example: FrontDoor_20240126_143215.mp4
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Find the timestamp pattern at the end: _yyyyMMdd_HHmmss
        var lastUnderscore = nameWithoutExtension.LastIndexOf('_');
        if (lastUnderscore < 9)
        {
            return null;
        }

        // Get the timestamp part (should be like "20240126_143215")
        var secondLastUnderscore = nameWithoutExtension.LastIndexOf('_', lastUnderscore - 1);
        if (secondLastUnderscore < 0)
        {
            return null;
        }

        var timestampPart = nameWithoutExtension[(secondLastUnderscore + 1)..];

        if (DateTime.TryParseExact(
            timestampPart,
            "yyyyMMdd_HHmmss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var result))
        {
            return result;
        }

        return null;
    }

    private static SolidColorBrush ParseColor(
        string colorName,
        SolidColorBrush defaultBrush)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            return new SolidColorBrush(color);
        }
        catch
        {
            return defaultBrush;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (Player is null)
        {
            return;
        }

        if (Player.Status == Status.Playing)
        {
            Player.Pause();
        }
        else
        {
            Player.Play();
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    [RelayCommand]
    private void CycleSpeed()
    {
        currentSpeedIndex = (currentSpeedIndex + 1) % SpeedOptions.Length;
        PlaybackSpeed = SpeedOptions[currentSpeedIndex];
        SpeedText = $"{PlaybackSpeed:G}x";

        if (Player is not null)
        {
            Player.Speed = PlaybackSpeed;
        }
    }

    private void InitializePlayer()
    {
        // Reset speed to 1x
        currentSpeedIndex = 0;
        PlaybackSpeed = 1.0;
        SpeedText = "1x";

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
                Enabled = true,
            },
        };

        Player = new Player(config);
        Player.PropertyChanged += OnPlayerPropertyChanged;

        // Defer file opening to avoid blocking UI during window creation
        _ = Task.Run(() =>
        {
            try
            {
                Player?.Open(filePath);
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
        switch (e.PropertyName)
        {
            case nameof(Player.Status):
                IsPlaying = Player?.Status == Status.Playing;
                break;

            case nameof(Player.Duration):
                var duration = Player?.Duration ?? 0;
                DurationText = FormatDuration(duration);
                SeekMaximum = duration > 0 ? duration : 100;
                CanSeek = duration > 0;
                break;
        }
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

    private void StartPositionUpdateTimer()
    {
        positionUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };

        positionUpdateTimer.Tick += (_, _) =>
        {
            if (Player is not null)
            {
                var position = Player.CurTime;

                // Always update the recording time text
                UpdateRecordingTimeText(position);

                if (!isSeeking)
                {
                    // Set flag to prevent OnSeekValueChanged from triggering a seek
                    isUpdatingPositionFromPlayer = true;
                    try
                    {
                        SeekPosition = position;
                        PositionText = FormatDuration(position);
                    }
                    finally
                    {
                        isUpdatingPositionFromPlayer = false;
                    }
                }
            }
        };

        positionUpdateTimer.Start();
    }

    private static string FormatDuration(long ticks)
    {
        var timeSpan = TimeSpan.FromTicks(ticks);
        return timeSpan.Hours > 0
            ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
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

            positionUpdateTimer?.Stop();
            positionUpdateTimer = null;

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