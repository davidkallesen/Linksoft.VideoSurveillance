namespace Linksoft.VideoSurveillance.Helpers;

/// <summary>
/// Shared helper for resolving recording policy decisions that consider per-camera overrides.
/// </summary>
public static class RecordingPolicyHelper
{
    /// <summary>
    /// Determines whether a camera should record on connect, considering per-camera overrides.
    /// </summary>
    /// <param name="camera">The camera configuration (may have per-camera overrides).</param>
    /// <param name="appDefault">The application-level default for EnableRecordingOnConnect.</param>
    /// <returns><c>true</c> if recording should start when the camera connects.</returns>
    public static bool ShouldRecordOnConnect(
        CameraConfiguration camera,
        bool appDefault)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return camera.Overrides?.Recording.EnableRecordingOnConnect ?? appDefault;
    }
}