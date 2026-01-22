#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace Linksoft.Wpf.CameraWall.UserControls;

/// <summary>
/// Overlay control displaying camera information on a camera tile.
/// </summary>
public partial class CameraOverlay
{
    private DispatcherTimer? timeTimer;
    private Storyboard? recordingBlinkAnimation;

    [DependencyProperty]
    private string title = string.Empty;

    [DependencyProperty(PropertyChangedCallback = nameof(OnDescriptionChanged))]
    private string description = string.Empty;

    [DependencyProperty(
        DefaultValue = ConnectionState.Disconnected,
        PropertyChangedCallback = nameof(OnConnectionStateChanged))]
    private ConnectionState connectionState;

    [DependencyProperty]
    private bool hasDescription;

    [DependencyProperty]
    private Brush statusColor = Brushes.Gray;

    [DependencyProperty]
    private string statusText = string.Empty;

    [DependencyProperty(DefaultValue = true)]
    private bool showTitle;

    [DependencyProperty(DefaultValue = true)]
    private bool showDescription;

    [DependencyProperty(DefaultValue = true)]
    private bool showConnectionStatus;

    [DependencyProperty(DefaultValue = false, PropertyChangedCallback = nameof(OnShowTimeChanged))]
    private bool showTime;

    [DependencyProperty]
    private string currentTime = string.Empty;

    [DependencyProperty(DefaultValue = 0.6)]
    private double overlayOpacity;

    [DependencyProperty(DefaultValue = false, PropertyChangedCallback = nameof(OnIsRecordingChanged))]
    private bool isRecording;

    [DependencyProperty(PropertyChangedCallback = nameof(OnRecordingDurationChanged))]
    private TimeSpan recordingDuration;

    [DependencyProperty]
    private string recordingDurationText = string.Empty;

    [DependencyProperty(DefaultValue = false)]
    private bool isMotionDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraOverlay"/> class.
    /// </summary>
    public CameraOverlay()
    {
        InitializeComponent();
        StatusText = Translations.Disconnected;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(
        object sender,
        RoutedEventArgs e)
        => StopTimeTimer();

    private static void OnDescriptionChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraOverlay overlay)
        {
            overlay.HasDescription = !string.IsNullOrEmpty(e.NewValue as string);
        }
    }

    private static void OnConnectionStateChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraOverlay overlay && e.NewValue is ConnectionState state)
        {
            overlay.UpdateConnectionVisuals(state);
        }
    }

    private void UpdateConnectionVisuals(ConnectionState state)
    {
        var (color, text) = state switch
        {
            ConnectionState.Connected => (Brushes.LimeGreen, Translations.Connected),
            ConnectionState.Connecting => (Brushes.Yellow, Translations.Connecting),
            ConnectionState.Reconnecting => (Brushes.Orange, Translations.Reconnecting),
            ConnectionState.ConnectionFailed => (Brushes.Red, Translations.Error),
            ConnectionState.Disconnected => (Brushes.Gray, Translations.Disconnected),
            _ => (Brushes.Gray, Translations.Unknown),
        };

        // Set dependency properties
        StatusColor = color;
        StatusText = text;

        // Also directly set UI elements in case bindings don't work in overlay window
        StatusIndicator.Fill = color;
        StatusTextBlock.Text = text;
    }

    private static void OnShowTimeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraOverlay overlay && e.NewValue is bool showTime)
        {
            if (showTime)
            {
                overlay.StartTimeTimer();
            }
            else
            {
                overlay.StopTimeTimer();
            }
        }
    }

    private void StartTimeTimer()
    {
        if (timeTimer is not null)
        {
            return;
        }

        // Update time immediately
        UpdateCurrentTime();

        timeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        timeTimer.Tick += (_, _) => UpdateCurrentTime();
        timeTimer.Start();
    }

    private void StopTimeTimer()
    {
        timeTimer?.Stop();
        timeTimer = null;
    }

    private void UpdateCurrentTime()
    {
        CurrentTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
    }

    private static void OnIsRecordingChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraOverlay overlay && e.NewValue is bool isRecording)
        {
            overlay.UpdateRecordingAnimation(isRecording);
        }
    }

    private static void OnRecordingDurationChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraOverlay overlay && e.NewValue is TimeSpan duration)
        {
            overlay.RecordingDurationText = duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }
    }

    private void UpdateRecordingAnimation(bool isRecording)
    {
        if (isRecording)
        {
            // Start the blink animation
            if (recordingBlinkAnimation is null && TryFindResource("RecordingBlinkAnimation") is Storyboard storyboard)
            {
                recordingBlinkAnimation = storyboard;
            }

            if (recordingBlinkAnimation is not null)
            {
                recordingBlinkAnimation.Begin(RecordingDot, isControllable: true);
            }
        }
        else
        {
            // Stop the blink animation
            recordingBlinkAnimation?.Stop(RecordingDot);
        }
    }
}