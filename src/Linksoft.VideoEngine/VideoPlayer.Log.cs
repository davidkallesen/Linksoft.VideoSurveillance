namespace Linksoft.VideoEngine;

public sealed partial class VideoPlayer
{
    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Recording started: {Path}")]
    private partial void LogRecordingStarted(string path);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Recording stopped")]
    private partial void LogRecordingStopped();

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Opening stream: {Uri}")]
    private partial void LogOpeningStream(string uri);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Stream demuxer opened successfully")]
    private partial void LogStreamDemuxerOpened();

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Decoder opened: HwAccel={HwAccel}")]
    private partial void LogDecoderOpened(bool hwAccel);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "Stream opened: {StreamInfo} from {Uri}")]
    private partial void LogStreamOpened(VideoStreamInfo streamInfo, string uri);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "Demux loop failed")]
    private partial void LogDemuxLoopFailed(Exception ex);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "End of stream reached")]
    private partial void LogEndOfStreamReached();

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "Exceeded {Max} consecutive read errors")]
    private partial void LogExceededConsecutiveReadErrors(int max);
}