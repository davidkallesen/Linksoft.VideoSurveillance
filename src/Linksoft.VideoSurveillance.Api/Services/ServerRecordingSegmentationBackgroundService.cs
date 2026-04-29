namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side periodic segmentation. Mirrors the WPF
/// <c>RecordingSegmentationService</c> but runs as an
/// <see cref="IHostedService"/> driven by <see cref="System.Threading.Timer"/>
/// and uses the shared <see cref="RecordingSlotCalculator"/> for boundary
/// detection that is immune to NTP rollback, midnight, and DST.
/// </summary>
public sealed partial class ServerRecordingSegmentationBackgroundService : IHostedService, IAsyncDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly ILogger<ServerRecordingSegmentationBackgroundService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly IRecordingService recordingService;

    private Timer? checkTimer;
    private (DateOnly Date, int Slot) lastProcessed = (DateOnly.MinValue, -1);
    private int tickInProgress;
    private bool disposed;

    public ServerRecordingSegmentationBackgroundService(
        ILogger<ServerRecordingSegmentationBackgroundService> logger,
        IApplicationSettingsService settingsService,
        IRecordingService recordingService)
    {
        this.logger = logger;
        this.settingsService = settingsService;
        this.recordingService = recordingService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = settingsService.Recording;
        if (!settings.EnableHourlySegmentation)
        {
            LogSegmentationDisabled();
            return Task.CompletedTask;
        }

        // Initialize the high-water mark to the current slot so we don't
        // segment a recording that just started during a service restart
        // boundary alignment.
        lastProcessed = RecordingSlotCalculator.ComputeSlot(
            DateTime.Now,
            settings.MaxRecordingDurationMinutes);

        checkTimer = new Timer(OnCheckTimerTick, state: null, CheckInterval, CheckInterval);
        LogSegmentationStarted(settings.MaxRecordingDurationMinutes);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (checkTimer is not null)
        {
            await checkTimer.DisposeAsync().ConfigureAwait(false);
            checkTimer = null;
        }

        LogSegmentationStopped();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private void OnCheckTimerTick(object? state)
    {
        // System.Threading.Timer can overlap ticks if a previous tick takes
        // longer than the interval (e.g. SwitchRecording stalls during an
        // RTSP reconnect). Without this guard, two threads would compute
        // identical currentSlot values and both call SegmentRecording on
        // the same camera — the recording service's TryGetValue/mutate is
        // not atomic, so the result would be undefined.
        if (Interlocked.CompareExchange(ref tickInProgress, 1, 0) != 0)
        {
            return;
        }

        try
        {
            PerformSegmentationCheck();
        }
        catch (Exception ex)
        {
            LogSegmentationTickFailed(ex);
        }
        finally
        {
            Interlocked.Exchange(ref tickInProgress, 0);
        }
    }

    private void PerformSegmentationCheck()
    {
        var settings = settingsService.Recording;

        if (!settings.EnableHourlySegmentation)
        {
            return;
        }

        var now = DateTime.Now;
        var intervalMinutes = settings.MaxRecordingDurationMinutes;
        var currentSlot = RecordingSlotCalculator.ComputeSlot(now, intervalMinutes);
        var isIntervalBoundary = RecordingSlotCalculator.IsNewBoundary(currentSlot, lastProcessed);
        var maxDuration = TimeSpan.FromMinutes(intervalMinutes);

        var activeSessions = recordingService.GetActiveSessions();
        if (activeSessions.Count == 0)
        {
            if (isIntervalBoundary)
            {
                lastProcessed = currentSlot;
            }

            return;
        }

        foreach (var session in activeSessions)
        {
            var shouldSegment = false;

            if (isIntervalBoundary)
            {
                shouldSegment = true;
                LogIntervalBoundaryDetected(session.CameraId, currentSlot.Slot, intervalMinutes);
            }
            else if (session.Duration >= maxDuration)
            {
                shouldSegment = true;
                LogMaxDurationReached(session.CameraId, session.Duration);
            }

            if (!shouldSegment)
            {
                continue;
            }

            var success = recordingService.SegmentRecording(session.CameraId);
            if (!success)
            {
                LogSegmentingFailed(session.CameraId);
            }
        }

        // Only advance the high-water mark forward; backward clock jumps
        // (NTP rollback, DST fall-back) leave it untouched so the same
        // wall-clock slot is never segmented twice.
        if (isIntervalBoundary)
        {
            lastProcessed = currentSlot;
        }
    }
}