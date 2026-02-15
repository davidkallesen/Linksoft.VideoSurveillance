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
    /// Gets a value indicating whether recording is currently active.
    /// </summary>
    bool IsRecordingActive { get; }

    /// <summary>
    /// Captures a single frame from the stream as PNG image bytes.
    /// </summary>
    Task<byte[]?> CaptureFrameAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    double CurrentFps { get; }

    /// <summary>
    /// Gets the total number of frames decoded.
    /// </summary>
    long FramesDecoded { get; }

    /// <summary>
    /// Occurs when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
}