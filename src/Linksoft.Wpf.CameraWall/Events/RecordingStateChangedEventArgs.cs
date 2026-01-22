namespace Linksoft.Wpf.CameraWall.Events;

/// <summary>
/// Event arguments for recording state changes.
/// </summary>
public class RecordingStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="cameraId">The ID of the camera whose recording state changed.</param>
    /// <param name="oldState">The previous recording state.</param>
    /// <param name="newState">The new recording state.</param>
    /// <param name="filePath">The file path of the recording, if applicable.</param>
    public RecordingStateChangedEventArgs(
        Guid cameraId,
        RecordingState oldState,
        RecordingState newState,
        string? filePath = null)
    {
        CameraId = cameraId;
        OldState = oldState;
        NewState = newState;
        FilePath = filePath;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the camera whose recording state changed.
    /// </summary>
    public Guid CameraId { get; }

    /// <summary>
    /// Gets the previous recording state.
    /// </summary>
    public RecordingState OldState { get; }

    /// <summary>
    /// Gets the new recording state.
    /// </summary>
    public RecordingState NewState { get; }

    /// <summary>
    /// Gets the file path of the recording, if applicable.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the timestamp when the state changed.
    /// </summary>
    public DateTime Timestamp { get; }
}