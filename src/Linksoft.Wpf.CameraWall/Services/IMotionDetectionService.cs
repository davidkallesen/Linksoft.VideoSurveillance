namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for detecting motion in camera video streams.
/// </summary>
public interface IMotionDetectionService
{
    /// <summary>
    /// Occurs when motion is detected on a camera.
    /// </summary>
    event EventHandler<MotionDetectedEventArgs>? MotionDetected;

    /// <summary>
    /// Starts motion detection for a camera.
    /// </summary>
    /// <param name="cameraId">The camera ID.</param>
    /// <param name="player">The FlyleafLib player instance.</param>
    /// <param name="settings">Optional motion detection settings. If null, uses defaults.</param>
    void StartDetection(
        Guid cameraId,
        FlyleafLib.MediaPlayer.Player player,
        MotionDetectionSettings? settings = null);

    /// <summary>
    /// Stops motion detection for a camera.
    /// </summary>
    /// <param name="cameraId">The camera ID.</param>
    void StopDetection(Guid cameraId);

    /// <summary>
    /// Checks if motion detection is active for a camera.
    /// </summary>
    /// <param name="cameraId">The camera ID.</param>
    /// <returns>True if detection is active; otherwise, false.</returns>
    bool IsDetectionActive(Guid cameraId);

    /// <summary>
    /// Checks if motion is currently detected for a camera.
    /// </summary>
    /// <param name="cameraId">The camera ID.</param>
    /// <returns>True if motion is detected; otherwise, false.</returns>
    bool IsMotionDetected(Guid cameraId);
}