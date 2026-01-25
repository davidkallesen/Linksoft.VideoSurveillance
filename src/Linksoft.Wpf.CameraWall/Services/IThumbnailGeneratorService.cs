namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for generating recording thumbnails.
/// Captures frames during recording and creates a 2x2 grid thumbnail.
/// </summary>
public interface IThumbnailGeneratorService
{
    /// <summary>
    /// Starts capturing frames for thumbnail generation.
    /// Captures 4 frames at 1-second intervals (0s, 1s, 2s, 3s).
    /// </summary>
    /// <param name="cameraId">The camera identifier.</param>
    /// <param name="player">The FlyleafLib player to capture frames from.</param>
    /// <param name="videoFilePath">The video file path (thumbnail will use same base name with .png extension).</param>
    void StartCapture(
        Guid cameraId,
        Player player,
        string videoFilePath);

    /// <summary>
    /// Stops capturing frames and generates the thumbnail if any frames were captured.
    /// Missing frames will be filled with black.
    /// </summary>
    /// <param name="cameraId">The camera identifier.</param>
    void StopCapture(Guid cameraId);

    /// <summary>
    /// Gets whether capture is currently active for a camera.
    /// </summary>
    /// <param name="cameraId">The camera identifier.</param>
    /// <returns>True if capture is active; otherwise, false.</returns>
    bool IsCaptureActive(Guid cameraId);

    /// <summary>
    /// Stops all active captures and generates thumbnails.
    /// </summary>
    void StopAllCaptures();
}