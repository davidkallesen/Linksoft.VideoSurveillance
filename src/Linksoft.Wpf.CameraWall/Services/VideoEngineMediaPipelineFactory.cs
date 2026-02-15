namespace Linksoft.Wpf.CameraWall.Services;

using CoreCameraConfiguration = Linksoft.VideoSurveillance.Models.CameraConfiguration;
using IMediaPipelineFactory = Linksoft.VideoSurveillance.Services.IMediaPipelineFactory;

/// <summary>
/// Factory that creates <see cref="VideoEngineMediaPipeline"/> instances
/// using <see cref="IVideoPlayerFactory"/>.
/// </summary>
[Registration(Lifetime.Singleton)]
public class VideoEngineMediaPipelineFactory : IMediaPipelineFactory
{
    private readonly IVideoPlayerFactory videoPlayerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoEngineMediaPipelineFactory"/> class.
    /// </summary>
    /// <param name="videoPlayerFactory">The video player factory.</param>
    public VideoEngineMediaPipelineFactory(
        IVideoPlayerFactory videoPlayerFactory)
    {
        this.videoPlayerFactory = videoPlayerFactory ?? throw new ArgumentNullException(nameof(videoPlayerFactory));
    }

    /// <inheritdoc />
    public IMediaPipeline Create(CoreCameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var player = videoPlayerFactory.Create();
        return new VideoEngineMediaPipeline(player);
    }
}