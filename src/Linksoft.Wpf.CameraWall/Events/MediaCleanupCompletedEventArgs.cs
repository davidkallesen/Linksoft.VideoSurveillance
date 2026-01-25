namespace Linksoft.Wpf.CameraWall.Events;

/// <summary>
/// Event arguments for media cleanup completion.
/// </summary>
/// <param name="result">The cleanup result.</param>
public class MediaCleanupCompletedEventArgs(MediaCleanupResult result) : EventArgs
{
    /// <summary>
    /// Gets the cleanup result.
    /// </summary>
    public MediaCleanupResult Result { get; } = result ?? throw new ArgumentNullException(nameof(result));
}