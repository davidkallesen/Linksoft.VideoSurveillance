namespace Linksoft.CameraWall.Wpf.Services;

/// <summary>
/// Service for automatically segmenting recordings at clock-aligned interval boundaries
/// (e.g., every 15 minutes at :00, :15, :30, :45).
/// </summary>
[Registration(Lifetime.Singleton)]
public partial class RecordingSegmentationService : IRecordingSegmentationService, IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly ILogger<RecordingSegmentationService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly IRecordingService recordingService;
    private readonly Lock lockObject = new();
    private DispatcherTimer? checkTimer;
    private int lastProcessedSlot = -1;
    private bool isRunning;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingSegmentationService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="recordingService">The recording service.</param>
    public RecordingSegmentationService(
        ILogger<RecordingSegmentationService> logger,
        IApplicationSettingsService settingsService,
        IRecordingService recordingService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
    }

    /// <inheritdoc/>
    public event EventHandler<RecordingSegmentedEventArgs>? RecordingSegmented;

    /// <inheritdoc/>
    public bool IsRunning
    {
        get
        {
            lock (lockObject)
            {
                return isRunning;
            }
        }
    }

    /// <inheritdoc/>
    public void Initialize()
    {
        var settings = settingsService.Recording;

        if (!settings.EnableHourlySegmentation)
        {
            LogSegmentationDisabled();
            return;
        }

        LogSegmentationInitializing(settings.MaxRecordingDurationMinutes);

        // Initialize last processed slot based on current time and interval
        var intervalMinutes = settings.MaxRecordingDurationMinutes;
        var now = DateTime.Now;
        lastProcessedSlot = ((now.Hour * 60) + now.Minute) / intervalMinutes;

        StartCheckTimer();

        lock (lockObject)
        {
            isRunning = true;
        }

        LogSegmentationStarted();
    }

    /// <inheritdoc/>
    public void StopService()
    {
        StopCheckTimer();

        lock (lockObject)
        {
            isRunning = false;
        }

        LogSegmentationStopped();
    }

    /// <summary>
    /// Disposes of the service resources.
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
            StopService();
        }

        disposed = true;
    }

    private void StartCheckTimer()
    {
        checkTimer = new DispatcherTimer
        {
            Interval = CheckInterval,
        };

        checkTimer.Tick += OnCheckTimerTick;
        checkTimer.Start();

        LogSegmentationTimerStarted(CheckInterval);
    }

    private void StopCheckTimer()
    {
        if (checkTimer is not null)
        {
            checkTimer.Stop();
            checkTimer.Tick -= OnCheckTimerTick;
            checkTimer = null;
            LogSegmentationTimerStopped();
        }
    }

    // Catches all exceptions; an unhandled exception in a DispatcherTimer.Tick
    // handler crashes the WPF dispatcher.
    private void OnCheckTimerTick(
        object? sender,
        EventArgs e)
    {
        try
        {
            PerformSegmentationCheck();
        }
        catch (Exception ex)
        {
            LogSegmentationTickFailed(ex);
        }
    }

    private void PerformSegmentationCheck()
    {
        var settings = settingsService.Recording;

        // Check if service is still enabled (settings may have changed)
        if (!settings.EnableHourlySegmentation)
        {
            return;
        }

        var now = DateTime.Now;
        var intervalMinutes = settings.MaxRecordingDurationMinutes;
        var currentSlot = ((now.Hour * 60) + now.Minute) / intervalMinutes;
        var isIntervalBoundary = currentSlot != lastProcessedSlot;
        var maxDuration = TimeSpan.FromMinutes(intervalMinutes);

        // Get all active sessions
        var activeSessions = recordingService.GetActiveSessions();

        if (activeSessions.Count == 0)
        {
            // Update slot tracking even if no recordings
            lastProcessedSlot = currentSlot;
            return;
        }

        foreach (var session in activeSessions)
        {
            var shouldSegment = false;
            var reason = SegmentationReason.IntervalBoundary;

            // Check for interval boundary (clock-aligned)
            if (isIntervalBoundary)
            {
                shouldSegment = true;
                reason = SegmentationReason.IntervalBoundary;
                LogIntervalBoundaryDetected(session.CameraId, currentSlot, intervalMinutes);
            }
            else if (session.Duration >= maxDuration)
            {
                shouldSegment = true;
                reason = SegmentationReason.MaxDurationReached;
                LogMaxDurationReached(session.CameraId, session.Duration);
            }

            if (shouldSegment)
            {
                var previousFilePath = session.CurrentFilePath;

                LogSegmentingRecording(session.CameraId, reason);

                var success = recordingService.SegmentRecording(session.CameraId);

                if (success)
                {
                    // Get the new session to find the new file path
                    var newSession = recordingService.GetSession(session.CameraId);
                    var newFilePath = newSession?.CurrentFilePath ?? string.Empty;

                    OnRecordingSegmented(session.CameraId, previousFilePath, newFilePath, reason);
                }
                else
                {
                    LogSegmentingFailed(session.CameraId);
                }
            }
        }

        // Update last processed slot
        lastProcessedSlot = currentSlot;
    }

    private void OnRecordingSegmented(
        Guid cameraId,
        string previousFilePath,
        string newFilePath,
        SegmentationReason reason)
    {
        RecordingSegmented?.Invoke(
            this,
            new RecordingSegmentedEventArgs(cameraId, previousFilePath, newFilePath, reason));
    }
}