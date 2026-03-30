namespace Linksoft.VideoSurveillance.Api.Services;

public sealed partial class StreamingService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "HLS stream stopped for camera {CameraId} (no viewers)")]
    private partial void LogHlsStreamStopped(Guid cameraId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting FFmpeg for camera {CameraId}: ffmpeg {Args}")]
    private partial void LogStartingFfmpeg(Guid cameraId, string args);

    [LoggerMessage(Level = LogLevel.Information, Message = "[FFmpeg {CameraId}] {Line}")]
    private partial void LogFfmpegOutput(Guid cameraId, string line);

    [LoggerMessage(Level = LogLevel.Information, Message = "HLS stream started for camera {CameraId} -> {PlaylistPath}")]
    private partial void LogHlsStreamStarted(Guid cameraId, string playlistPath);
}