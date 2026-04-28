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
    private DispatcherTimer? periodicTimer;
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

            // Clean recordings
            var recordingPath = settingsService.Recording.RecordingPath;
            if (!string.IsNullOrEmpty(recordingPath) && Directory.Exists(recordingPath))
            {
                await CleanDirectoryAsync(
                    recordingPath,
                    RecordingExtensions,
                    settings.RecordingRetentionDays,
                    result,
                    activePaths,
                    isRecording: true).ConfigureAwait(false);
            }

            // Clean snapshots if enabled
            if (settings.IncludeSnapshots)
            {
                var snapshotPath = settingsService.CameraDisplay.SnapshotPath;
                if (!string.IsNullOrEmpty(snapshotPath) && Directory.Exists(snapshotPath))
                {
                    await CleanDirectoryAsync(
                        snapshotPath,
                        SnapshotExtensions,
                        settings.SnapshotRetentionDays,
                        result,
                        activePaths,
                        isRecording: false).ConfigureAwait(false);
                }
            }

            // Clean up empty directories
            await CleanEmptyDirectoriesAsync(recordingPath, result).ConfigureAwait(false);
            if (settings.IncludeSnapshots)
            {
                var snapshotPath = settingsService.CameraDisplay.SnapshotPath;
                if (!string.IsNullOrEmpty(snapshotPath) && Directory.Exists(snapshotPath))
                {
                    await CleanEmptyDirectoriesAsync(snapshotPath, result).ConfigureAwait(false);
                }
            }

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
        periodicTimer = new DispatcherTimer
        {
            Interval = PeriodicInterval,
        };

        periodicTimer.Tick += OnPeriodicTimerTick;
        periodicTimer.Start();

        LogPeriodicTimerStarted(PeriodicInterval);
    }

    private void StopPeriodicTimer()
    {
        if (periodicTimer is not null)
        {
            periodicTimer.Stop();
            periodicTimer.Tick -= OnPeriodicTimerTick;
            periodicTimer = null;
            LogPeriodicTimerStopped();
        }
    }

    // Async-void event handler must catch all exceptions; an unobserved
    // exception here would crash the dispatcher.
    private async void OnPeriodicTimerTick(
        object? sender,
        EventArgs e)
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

    private Task CleanDirectoryAsync(
        string path,
        string[] extensions,
        int retentionDays,
        MediaCleanupResult result,
        HashSet<string> activePaths,
        bool isRecording)
    {
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);

        return Task.Run(() =>
        {
            try
            {
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

                foreach (var file in files)
                {
                    try
                    {
                        // Skip files currently being written by an active recording
                        if (activePaths.Contains(Path.GetFullPath(file)))
                        {
                            continue;
                        }

                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            var fileSize = fileInfo.Length;
                            fileInfo.Delete();

                            if (isRecording)
                            {
                                result.RecordingsDeleted++;

                                // Also delete associated thumbnail if exists
                                var thumbnailPath = Path.ChangeExtension(file, ".png");
                                if (File.Exists(thumbnailPath))
                                {
                                    var thumbInfo = new FileInfo(thumbnailPath);
                                    var thumbSize = thumbInfo.Length;
                                    thumbInfo.Delete();
                                    result.ThumbnailsDeleted++;
                                    result.BytesFreed += thumbSize;
                                    LogDeletedThumbnail(thumbnailPath);
                                }
                            }
                            else
                            {
                                result.SnapshotsDeleted++;
                            }

                            result.BytesFreed += fileSize;
                            LogDeletedOldMediaFile(file);
                        }
                    }
                    catch (IOException ex)
                    {
                        result.ErrorCount++;
                        LogFailedToDeleteFile(ex, file);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        result.ErrorCount++;
                        LogAccessDeniedDeletingFile(ex, file);
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Directory no longer exists, nothing to clean
            }
            catch (UnauthorizedAccessException ex)
            {
                // Recursive enumeration hit a folder we can't read; abort this batch
                result.ErrorCount++;
                LogEnumerationFailed(ex, path);
            }
            catch (IOException ex)
            {
                // Network drive dropped, path-too-long, etc. Don't crash the dispatcher.
                result.ErrorCount++;
                LogEnumerationFailed(ex, path);
            }
        });
    }

    private Task CleanEmptyDirectoriesAsync(
        string rootPath,
        MediaCleanupResult result)
    {
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
        {
            return Task.CompletedTask;
        }

        return Task.Run(() =>
        {
            try
            {
                // Get all directories, sorted by depth (deepest first)
                var directories = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                    .OrderByDescending(d => d.Length)
                    .ToList();

                foreach (var dir in directories)
                {
                    try
                    {
                        // Don't delete if it's the root path
                        if (string.Equals(dir, rootPath, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Check if directory is empty
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir);
                            result.DirectoriesRemoved++;
                            LogRemovedEmptyDirectory(dir);
                        }
                    }
                    catch (IOException ex)
                    {
                        LogCouldNotRemoveDirectory(ex, dir);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogAccessDeniedRemovingDirectory(ex, dir);
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Root directory no longer exists
            }
        });
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