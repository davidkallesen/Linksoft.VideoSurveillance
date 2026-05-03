namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Periodic liveness beacon for long-running deployments. Each tick of the
/// <see cref="BackgroundServiceBase{T}"/> loop emits a single INFO line with
/// uptime, active recording sessions, stuck-session count, process metrics
/// (working set, handles, threads, GC counts) and free disk space on the
/// recording drive. Without this, a 4-day soak log can go nearly silent
/// while a slow leak or wedged subsystem brews unnoticed; the beacon gives
/// a steady cadence that an operator (or the soak-report script) can scan
/// for drift over time.
/// </summary>
public sealed partial class ServerHeartbeatService : BackgroundServiceBase<ServerHeartbeatService>
{
    private readonly ServerRecordingService recordingService;
    private readonly IApplicationSettingsService settingsService;
    private readonly DateTime processStartUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    public ServerHeartbeatService(
        ILogger<ServerHeartbeatService> logger,
        ServerHeartbeatServiceOptions backgroundServiceOptions,
        ServerRecordingService recordingService,
        IApplicationSettingsService settingsService)
        : base(logger, backgroundServiceOptions)
    {
        this.recordingService = recordingService;
        this.settingsService = settingsService;
    }

    /// <inheritdoc />
    public override Task DoWorkAsync(CancellationToken stoppingToken)
    {
        EmitHeartbeat();
        return Task.CompletedTask;
    }

    private void EmitHeartbeat()
    {
        var diagnostics = recordingService.GetDiagnostics();
        var stuckCount = diagnostics.Count(d => !d.IsPipelineActive);
        var cameraNames = diagnostics.Count == 0
            ? "(none)"
            : string.Join(", ", diagnostics.Select(d => d.CameraName));

        var uptime = DateTime.UtcNow - processStartUtc;

        // Process.Refresh() is required — cached values stale until refreshed.
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var workingSetMb = process.WorkingSet64 / (1024.0 * 1024.0);
        var handleCount = process.HandleCount;
        var threadCount = process.Threads.Count;

        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);

        var (driveLabel, freeGb) = TryGetRecordingDriveFreeGb();

        LogHeartbeat(
            (long)uptime.TotalSeconds,
            diagnostics.Count,
            stuckCount,
            cameraNames,
            workingSetMb,
            handleCount,
            threadCount,
            gen0,
            gen1,
            gen2,
            driveLabel,
            freeGb);
    }

    private (string Drive, double FreeGb) TryGetRecordingDriveFreeGb()
    {
        var recordingPath = settingsService.Recording.RecordingPath;
        if (string.IsNullOrEmpty(recordingPath))
        {
            return ("(unset)", -1);
        }

        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(recordingPath));
            if (string.IsNullOrEmpty(root))
            {
                return ("(unrooted)", -1);
            }

            var drive = new DriveInfo(root);
            return (root, drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Soak heartbeats must not crash because of a transient drive
            // hiccup — surface a sentinel and keep going.
            return ("(error)", -1);
        }
    }
}