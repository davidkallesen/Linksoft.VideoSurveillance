namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Tracks an active recording session for a camera.
/// </summary>
public class RecordingSession
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingSession"/> class.
    /// </summary>
    /// <param name="cameraId">The ID of the camera being recorded.</param>
    /// <param name="filePath">The file path of the recording.</param>
    /// <param name="isManualRecording">True if this is a manual recording, false if triggered by motion.</param>
    public RecordingSession(
        Guid cameraId,
        string filePath,
        bool isManualRecording)
    {
        CameraId = cameraId;
        CurrentFilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        IsManualRecording = isManualRecording;
        State = isManualRecording ? RecordingState.Recording : RecordingState.RecordingMotion;
        StartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the camera being recorded.
    /// </summary>
    public Guid CameraId { get; }

    /// <summary>
    /// Gets or sets the current recording state.
    /// </summary>
    public RecordingState State { get; set; }

    /// <summary>
    /// Gets the file path of the current recording.
    /// </summary>
    public string CurrentFilePath { get; }

    /// <summary>
    /// Gets the time when the recording started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets a value indicating whether this is a manual recording (vs motion-triggered).
    /// </summary>
    public bool IsManualRecording { get; }

    /// <summary>
    /// Gets or sets the last time motion was detected (for motion-triggered recordings).
    /// </summary>
    public DateTime? LastMotionTime { get; set; }

    /// <summary>
    /// Gets the current recording duration.
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - StartTime;
}