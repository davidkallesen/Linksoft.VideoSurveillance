namespace Linksoft.VideoSurveillance.Services;

/// <summary>
/// Abstraction over a media playback/capture pipeline.
/// Both WPF and Server implement via Linksoft.VideoEngine (in-process FFmpeg).
/// </summary>
public interface IMediaPipeline : IDisposable
{
    /// <summary>
    /// Opens a stream from the specified URI.
    /// </summary>
    void Open(
        Uri streamUri,
        StreamSettings settings);

    /// <summary>
    /// Closes the current stream.
    /// </summary>
    void Close();

    /// <summary>
    /// Starts recording the stream to a file.
    /// </summary>
    void StartRecording(string outputFilePath);

    /// <summary>
    /// Stops the current recording.
    /// </summary>
    void StopRecording();

    /// <summary>
    /// Atomically transitions the active recording to a new output file.
    /// Implementations should perform the close+open under a single lock so
    /// that packets arriving mid-switch land in either the previous or the
    /// new segment — never the close/open gap.
    /// Default implementation falls back to <see cref="StopRecording"/>
    /// followed by <see cref="StartRecording"/>; override for atomicity.
    /// </summary>
    void SwitchRecording(string newOutputFilePath)
    {
        StopRecording();
        StartRecording(newOutputFilePath);
    }

    /// <summary>
    /// Gets a value indicating whether recording is currently active.
    /// </summary>
    bool IsRecordingActive { get; }

    /// <summary>
    /// Captures a single frame from the stream as PNG image bytes.
    /// </summary>
    Task<byte[]?> CaptureFrameAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the clockwise rotation applied to live display, snapshots, and
    /// (via container metadata) any active or future recording.
    /// </summary>
    void SetRotation(CameraRotation rotation);

    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    double CurrentFps { get; }

    /// <summary>
    /// Gets the total number of frames decoded.
    /// </summary>
    long FramesDecoded { get; }

    /// <summary>
    /// Gets the UTC timestamp of the last packet successfully read from the
    /// underlying demuxer, or <see cref="DateTime.MinValue"/> if no packet has
    /// arrived yet. Used by the server-side stream-stale watchdog to detect
    /// wedged pipelines (socket open but no packets) without waiting for the
    /// VideoEngine's consecutive-read-errors threshold.
    /// </summary>
    DateTime LastPacketUtc { get; }

    /// <summary>
    /// Occurs when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}