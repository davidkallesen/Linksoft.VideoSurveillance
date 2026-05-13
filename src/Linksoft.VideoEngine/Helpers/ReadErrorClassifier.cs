namespace Linksoft.VideoEngine.Helpers;

/// <summary>
/// Pure classification of read-loop error bursts. Lives outside
/// <see cref="VideoPlayer"/> so the heuristic can be unit-tested without
/// the FFmpeg-bound demuxer/decoder dependency.
/// </summary>
public static class ReadErrorClassifier
{
    /// <summary>
    /// Decide how to label a burst of consecutive <c>av_read_frame</c>
    /// failures that exceeded the read-loop's tolerance threshold.
    /// </summary>
    /// <param name="inputFormat">The input format the player opened
    /// with. Only <see cref="InputFormatKind.Dshow"/> currently maps to
    /// the device-busy heuristic — other local-device kinds
    /// (V4l2, AVFoundation) reserved for the deferred platform phases.</param>
    /// <param name="anyFramesReceived"><see langword="true"/> if the
    /// read loop produced at least one successful packet before the
    /// error burst. A camera that streamed then stopped is treated as
    /// <see cref="StreamFailureReason.Unknown"/> (likely a hardware
    /// disconnect / driver hiccup, not exclusive lock).</param>
    /// <param name="lastErrorCode">Most recent FFmpeg return code from
    /// <c>av_read_frame</c>. We classify on this and not the full
    /// history because FFmpeg's dshow demuxer consistently emits the
    /// same code while the device is locked.</param>
    public static StreamFailureReason Classify(
        InputFormatKind inputFormat,
        bool anyFramesReceived,
        int lastErrorCode)
    {
        if (inputFormat != InputFormatKind.Dshow)
        {
            return StreamFailureReason.Unknown;
        }

        if (anyFramesReceived)
        {
            return StreamFailureReason.Unknown;
        }

        // AVERROR(EAGAIN) on a dshow source that never delivered a
        // frame is the canonical "device locked by another consumer"
        // pattern. EAGAIN under FFmpeg's POSIX-style errno conversion
        // is -11 on Windows builds; pulling the constant from the
        // global static-using keeps the build portable if the binding
        // ever changes.
        if (lastErrorCode == AVERROR_EAGAIN)
        {
            return StreamFailureReason.DeviceBusy;
        }

        return StreamFailureReason.Unknown;
    }
}