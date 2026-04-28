namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side periodic media cleanup. Mirrors the WPF
/// <c>MediaCleanupService</c> but runs as an <see cref="IHostedService"/>
/// driven by <see cref="System.Threading.Timer"/> so it has no UI / dispatcher
/// dependency. Delegates the file-system work to the shared
/// <see cref="MediaCleanupRunner"/>.
/// </summary>
public sealed partial class ServerMediaCleanupBackgroundService : IHostedService, IAsyncDisposable
{
    private static readonly string[] RecordingExtensions = [".mp4", ".mkv", ".avi"];
    private static readonly string[] SnapshotExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

    // Server runs cleanup more frequently than WPF (which runs on user-driven
    // schedule). 4 hours is a safe default for autonomous service operation.
    private static readonly TimeSpan PeriodicInterval = TimeSpan.FromHours(4);

    private readonly ILogger<ServerMediaCleanupBackgroundService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly IRecordingService recordingService;
    private readonly Lock runLock = new();
    private Timer? timer;
    private bool isRunning;
    private bool disposed;

    public ServerMediaCleanupBackgroundService(
        ILogger<ServerMediaCleanupBackgroundService> logger,
        IApplicationSettingsService settingsService,
        IRecordingService recordingService)
    {
        this.logger = logger;
        this.settingsService = settingsService;
        this.recordingService = recordingService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var schedule = settingsService.Recording.Cleanup.Schedule;
        LogServiceStarting(schedule);

        switch (schedule)
        {
            case MediaCleanupSchedule.Disabled:
                LogDisabled();
                break;

            case MediaCleanupSchedule.OnStartup:
                _ = Task.Run(RunCleanupSafelyAsync, cancellationToken);
                break;

            case MediaCleanupSchedule.OnStartupAndPeriodically:
                _ = Task.Run(RunCleanupSafelyAsync, cancellationToken);
                timer = new Timer(OnPeriodicTick, null, PeriodicInterval, PeriodicInterval);
                break;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (timer is not null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
            timer = null;
        }

        LogServiceStopped();
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

    private void OnPeriodicTick(object? state)
        => _ = RunCleanupSafelyAsync();

    private async Task RunCleanupSafelyAsync()
    {
        try
        {
            await RunCleanupAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogCleanupTickFailed(ex);
        }
    }

    private async Task RunCleanupAsync()
    {
        lock (runLock)
        {
            if (isRunning)
            {
                LogAlreadyInProgress();
                return;
            }

            isRunning = true;
        }

        try
        {
            var settings = settingsService.Recording.Cleanup;

            LogCleanupStarting(
                settings.RecordingRetentionDays,
                settings.IncludeSnapshots,
                settings.SnapshotRetentionDays);

            var activePaths = GetActiveRecordingPaths();

            // File-system work runs on the thread pool — never block the
            // hosted-service start-up sequence with synchronous IO.
            var summary = await Task.Run(() => RunCore(settings, activePaths)).ConfigureAwait(false);

            LogCleanupCompleted(
                summary.RecordingsDeleted,
                summary.SnapshotsDeleted,
                summary.ThumbnailsDeleted,
                summary.DirectoriesRemoved,
                FormatBytes(summary.BytesFreed),
                summary.ErrorCount);
        }
        finally
        {
            lock (runLock)
            {
                isRunning = false;
            }
        }
    }

    private CleanupSummary RunCore(
        MediaCleanupSettings settings,
        HashSet<string> activePaths)
    {
        var summary = new CleanupSummary();

        var recordingPath = settingsService.Recording.RecordingPath;
        if (!string.IsNullOrEmpty(recordingPath))
        {
            var run = MediaCleanupRunner.CleanDirectory(
                recordingPath,
                RecordingExtensions,
                DateTime.Now.AddDays(-settings.RecordingRetentionDays),
                activePaths,
                deleteCompanionThumbnail: true);

            foreach (var f in run.DeletedFiles)
            {
                LogDeletedFile(f);
            }

            foreach (var t in run.DeletedThumbnails)
            {
                LogDeletedThumbnail(t);
            }

            summary.RecordingsDeleted += run.DeletedFiles.Count;
            summary.ThumbnailsDeleted += run.DeletedThumbnails.Count;
            summary.BytesFreed += run.BytesFreed;
            ApplyErrors(run.Errors, summary);

            var emptyDirs = MediaCleanupRunner.RemoveEmptyDirectoriesBelow(recordingPath);
            ApplyEmptyDirectoriesResult(emptyDirs, summary);
        }

        if (!settings.IncludeSnapshots)
        {
            return summary;
        }

        var snapshotPath = settingsService.CameraDisplay.SnapshotPath;
        if (string.IsNullOrEmpty(snapshotPath))
        {
            return summary;
        }

        var snapshotRun = MediaCleanupRunner.CleanDirectory(
            snapshotPath,
            SnapshotExtensions,
            DateTime.Now.AddDays(-settings.SnapshotRetentionDays),
            activePaths,
            deleteCompanionThumbnail: false);

        foreach (var f in snapshotRun.DeletedFiles)
        {
            LogDeletedFile(f);
        }

        summary.SnapshotsDeleted += snapshotRun.DeletedFiles.Count;
        summary.BytesFreed += snapshotRun.BytesFreed;
        ApplyErrors(snapshotRun.Errors, summary);

        var snapshotEmpty = MediaCleanupRunner.RemoveEmptyDirectoriesBelow(snapshotPath);
        ApplyEmptyDirectoriesResult(snapshotEmpty, summary);

        return summary;
    }

    private void ApplyEmptyDirectoriesResult(
        MediaCleanupDirectoryResult run,
        CleanupSummary summary)
    {
        foreach (var dir in run.RemovedDirectories)
        {
            LogRemovedEmptyDirectory(dir);
        }

        summary.DirectoriesRemoved += run.RemovedDirectories.Count;

        foreach (var error in run.Errors)
        {
            LogDirectoryError(error.Exception, error.Path);
        }

        summary.ErrorCount += run.Errors.Count;
    }

    private void ApplyErrors(
        IReadOnlyList<MediaCleanupRunError> errors,
        CleanupSummary summary)
    {
        foreach (var error in errors)
        {
            LogFileError(error.Exception, error.Path);
        }

        summary.ErrorCount += errors.Count;
    }

    private HashSet<string> GetActiveRecordingPaths()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var session in recordingService.GetActiveSessions())
            {
                if (!string.IsNullOrEmpty(session.CurrentFilePath))
                {
                    set.Add(Path.GetFullPath(session.CurrentFilePath));
                }
            }
        }
        catch (Exception ex)
        {
            LogActiveSessionLookupFailed(ex);
        }

        return set;
    }

    private static string FormatBytes(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F2} GB",
            >= MB => $"{bytes / (double)MB:F2} MB",
            >= KB => $"{bytes / (double)KB:F2} KB",
            _ => $"{bytes} bytes",
        };
    }

    private sealed class CleanupSummary
    {
        public int RecordingsDeleted { get; set; }

        public int SnapshotsDeleted { get; set; }

        public int ThumbnailsDeleted { get; set; }

        public int DirectoriesRemoved { get; set; }

        public long BytesFreed { get; set; }

        public int ErrorCount { get; set; }
    }
}