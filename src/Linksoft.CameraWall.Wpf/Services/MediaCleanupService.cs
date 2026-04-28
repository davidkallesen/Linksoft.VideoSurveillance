namespace Linksoft.CameraWall.Wpf.Services;

/// <summary>
/// Service for automatically cleaning up old recordings and snapshots.
/// </summary>
[Registration]
public partial class MediaCleanupService : IMediaCleanupService, IDisposable
{
    private static readonly string[] RecordingExtensions = [".mp4", ".mkv", ".avi"];
    private static readonly string[] SnapshotExtensions = [".png", ".jpg", ".jpeg", ".bmp"];
    private static readonly TimeSpan PeriodicInterval = TimeSpan.FromHours(6);

    private readonly ILogger<MediaCleanupService> logger;
    private readonly IApplicationSettingsService settingsService;
    private readonly IRecordingService recordingService;
    private readonly Lock lockObject = new();

    // Threading.Timer (not DispatcherTimer) so cleanup ticks fire even when
    // the UI thread is busy rendering 10+ video tiles.
    private Timer? periodicTimer;
    private bool isRunning;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaCleanupService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="settingsService">The application settings service.</param>
    /// <param name="recordingService">The recording service, used to identify and skip files that are currently being written to.</param>
    public MediaCleanupService(
        ILogger<MediaCleanupService> logger,
        IApplicationSettingsService settingsService,
        IRecordingService recordingService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
    }

    /// <inheritdoc/>
    public event EventHandler<MediaCleanupCompletedEventArgs>? CleanupCompleted;

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
        var settings = settingsService.Recording.Cleanup;

        LogCleanupInitializing(settings.Schedule);

        switch (settings.Schedule)
        {
            case MediaCleanupSchedule.Disabled:
                LogCleanupDisabled();
                break;

            case MediaCleanupSchedule.OnStartup:
                _ = RunCleanupAsync();
                break;

            case MediaCleanupSchedule.OnStartupAndPeriodically:
                _ = RunCleanupAsync();
                StartPeriodicTimer();
                break;
        }
    }

    /// <inheritdoc/>
    public void StopService()
    {
        StopPeriodicTimer();
        LogCleanupStopped();
    }

    /// <inheritdoc/>
    public async Task<MediaCleanupResult> RunCleanupAsync()
    {
        lock (lockObject)
        {
            if (isRunning)
            {
                LogCleanupAlreadyInProgress();
                return new MediaCleanupResult();
            }

            isRunning = true;
        }

        try
        {
            var settings = settingsService.Recording.Cleanup;
            var result = new MediaCleanupResult();

            LogCleanupStarting(
                settings.RecordingRetentionDays,
                settings.IncludeSnapshots,
                settings.SnapshotRetentionDays);

            // Snapshot active recording paths so an in-progress recording is
            // never deleted by the cleanup pass (sharing-violation IOException
            // on Windows; orphaned undeletable file otherwise).
            var activePaths = GetActiveRecordingPaths();

            // Run the actual file-system work on the thread pool — keeps the
            // dispatcher responsive when scanning very large recording trees.
            await Task.Run(() =>
                RunCleanupCore(settings, activePaths, result)).ConfigureAwait(false);

            LogCleanupCompleted(
                result.RecordingsDeleted,
                result.SnapshotsDeleted,
                result.ThumbnailsDeleted,
                result.DirectoriesRemoved,
                FormatBytes(result.BytesFreed),
                result.ErrorCount);

            OnCleanupCompleted(result);
            return result;
        }
        finally
        {
            lock (lockObject)
            {
                isRunning = false;
            }
        }
    }

    private void RunCleanupCore(
        MediaCleanupSettings settings,
        HashSet<string> activePaths,
        MediaCleanupResult result)
    {
        var recordingPath = settingsService.Recording.RecordingPath;
        if (!string.IsNullOrEmpty(recordingPath))
        {
            var run = MediaCleanupRunner.CleanDirectory(
                recordingPath,
                RecordingExtensions,
                DateTime.Now.AddDays(-settings.RecordingRetentionDays),
                activePaths,
                deleteCompanionThumbnail: true);

            ApplyRecordingRunResult(run, result);

            var emptyDirs = MediaCleanupRunner.RemoveEmptyDirectoriesBelow(recordingPath);
            ApplyEmptyDirectoriesResult(emptyDirs, result);
        }

        if (!settings.IncludeSnapshots)
        {
            return;
        }

        var snapshotPath = settingsService.CameraDisplay.SnapshotPath;
        if (string.IsNullOrEmpty(snapshotPath))
        {
            return;
        }

        var snapshotRun = MediaCleanupRunner.CleanDirectory(
            snapshotPath,
            SnapshotExtensions,
            DateTime.Now.AddDays(-settings.SnapshotRetentionDays),
            activePaths,
            deleteCompanionThumbnail: false);

        ApplySnapshotRunResult(snapshotRun, result);

        var snapshotEmpty = MediaCleanupRunner.RemoveEmptyDirectoriesBelow(snapshotPath);
        ApplyEmptyDirectoriesResult(snapshotEmpty, result);
    }

    private void ApplyRecordingRunResult(
        MediaCleanupRunResult run,
        MediaCleanupResult result)
    {
        foreach (var file in run.DeletedFiles)
        {
            LogDeletedOldMediaFile(file);
        }

        foreach (var thumb in run.DeletedThumbnails)
        {
            LogDeletedThumbnail(thumb);
        }

        result.RecordingsDeleted += run.DeletedFiles.Count;
        result.ThumbnailsDeleted += run.DeletedThumbnails.Count;
        result.BytesFreed += run.BytesFreed;
        ApplyErrors(run.Errors, result);
    }

    private void ApplySnapshotRunResult(
        MediaCleanupRunResult run,
        MediaCleanupResult result)
    {
        foreach (var file in run.DeletedFiles)
        {
            LogDeletedOldMediaFile(file);
        }

        result.SnapshotsDeleted += run.DeletedFiles.Count;
        result.BytesFreed += run.BytesFreed;
        ApplyErrors(run.Errors, result);
    }

    private void ApplyEmptyDirectoriesResult(
        MediaCleanupDirectoryResult run,
        MediaCleanupResult result)
    {
        foreach (var dir in run.RemovedDirectories)
        {
            LogRemovedEmptyDirectory(dir);
        }

        result.DirectoriesRemoved += run.RemovedDirectories.Count;
        foreach (var error in run.Errors)
        {
            switch (error.Exception)
            {
                case UnauthorizedAccessException uae:
                    LogAccessDeniedRemovingDirectory(uae, error.Path);
                    break;
                default:
                    LogCouldNotRemoveDirectory(error.Exception, error.Path);
                    break;
            }
        }

        result.ErrorCount += run.Errors.Count;
    }

    private void ApplyErrors(
        IReadOnlyList<MediaCleanupRunError> errors,
        MediaCleanupResult result)
    {
        foreach (var error in errors)
        {
            switch (error.Exception)
            {
                case UnauthorizedAccessException uae:
                    LogAccessDeniedDeletingFile(uae, error.Path);
                    break;
                case IOException ioe:
                    LogFailedToDeleteFile(ioe, error.Path);
                    break;
                default:
                    LogFailedToDeleteFile(error.Exception, error.Path);
                    break;
            }
        }

        result.ErrorCount += errors.Count;
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

    private void StartPeriodicTimer()
    {
        periodicTimer = new Timer(OnPeriodicTimerTick, state: null, PeriodicInterval, PeriodicInterval);
        LogPeriodicTimerStarted(PeriodicInterval);
    }

    private void StopPeriodicTimer()
    {
        if (periodicTimer is not null)
        {
            periodicTimer.Dispose();
            periodicTimer = null;
            LogPeriodicTimerStopped();
        }
    }

    private void OnPeriodicTimerTick(object? state)
        => _ = RunCleanupSafelyAsync();

    // The Timer callback is on a thread-pool thread, but we still need to
    // catch all exceptions: an unobserved exception in fire-and-forget
    // continuations would terminate the process via the unhandled-exception
    // path.
    private async Task RunCleanupSafelyAsync()
    {
        try
        {
            await RunCleanupAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogPeriodicTickFailed(ex);
        }
    }

    // Builds an unordered, case-insensitive set of file paths currently being
    // written by the recording service. Used to skip in-flight files during
    // cleanup so we never call File.Delete on a handle the muxer still owns.
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

                    // Also protect the live thumbnail companion file
                    var thumbnail = Path.ChangeExtension(session.CurrentFilePath, ".png");
                    set.Add(Path.GetFullPath(thumbnail));
                }
            }
        }
        catch (Exception ex)
        {
            // If we can't enumerate sessions, fail safe by treating no files as active
            // — but at least don't take down the cleanup pass.
            LogActiveSessionLookupFailed(ex);
        }

        return set;
    }

    private void OnCleanupCompleted(MediaCleanupResult result)
    {
        CleanupCompleted?.Invoke(this, new MediaCleanupCompletedEventArgs(result));
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
}