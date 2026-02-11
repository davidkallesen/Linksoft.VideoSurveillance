namespace Linksoft.Wpf.CameraWall.Services;

using CoreCameraConfiguration = Linksoft.VideoSurveillance.Models.CameraConfiguration;
using IMediaPipeline = Linksoft.VideoSurveillance.Services.IMediaPipeline;
using IMediaPipelineFactory = Linksoft.VideoSurveillance.Services.IMediaPipelineFactory;

/// <summary>
/// Factory that creates <see cref="FlyleafLibMediaPipeline"/> instances
/// by configuring FlyleafLib <see cref="Player"/> from camera settings.
/// </summary>
[Registration(Lifetime.Singleton)]
public class FlyleafLibMediaPipelineFactory : IMediaPipelineFactory
{
    /// <inheritdoc />
    public IMediaPipeline Create(CoreCameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var config = new Config
        {
            Player =
            {
                AutoPlay = true,
                MaxLatency = camera.Stream.MaxLatencyMs * 10000L,
                Stats = true,
            },
            Video =
            {
                BackColor = System.Windows.Media.Colors.Black,
                VideoAcceleration = true,
            },
            Audio =
            {
                Enabled = false,
            },
        };

        if (camera.Stream.UseLowLatencyMode)
        {
            config.Demuxer.BufferDuration = camera.Stream.BufferDurationMs * 10000L;
            config.Demuxer.FormatOpt["rtsp_transport"] = camera.Stream.RtspTransport;
            config.Demuxer.FormatOpt["fflags"] = "nobuffer";
        }

        var player = new Player(config);
        return new FlyleafLibMediaPipeline(player);
    }
}