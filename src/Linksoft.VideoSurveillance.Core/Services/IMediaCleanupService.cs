namespace Linksoft.VideoSurveillance.Services;

/// <summary>
/// Service for automatically cleaning up old recordings and snapshots.
/// </summary>
public interface IMediaCleanupService
{
    event EventHandler<MediaCleanupCompletedEventArgs>? CleanupCompleted;

    bool IsRunning { get; }

    Task<MediaCleanupResult> RunCleanupAsync();
}