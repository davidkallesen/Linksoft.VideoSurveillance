namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for automatically cleaning up old recordings and snapshots.
/// </summary>
public interface IMediaCleanupService
{
    /// <summary>
    /// Occurs when a cleanup operation completes.
    /// </summary>
    event EventHandler<MediaCleanupCompletedEventArgs>? CleanupCompleted;

    /// <summary>
    /// Gets a value indicating whether a cleanup operation is currently in progress.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Initializes the cleanup service based on configured settings.
    /// If cleanup schedule is OnStartup or OnStartupAndPeriodically, runs initial cleanup.
    /// If schedule is OnStartupAndPeriodically, starts periodic timer.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Stops the cleanup service and any running timers.
    /// </summary>
    void StopService();

    /// <summary>
    /// Runs cleanup immediately, regardless of the configured schedule.
    /// </summary>
    /// <returns>The cleanup result.</returns>
    Task<MediaCleanupResult> RunCleanupAsync();
}