namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Factory that creates <see cref="VideoEngineMediaPipeline"/> instances for the server edition.
/// Each pipeline wraps an <see cref="IVideoPlayer"/> and is opened immediately upon creation.
/// </summary>
public sealed class VideoEngineMediaPipelineFactory : IMediaPipelineFactory
{
    private readonly IVideoPlayerFactory videoPlayerFactory;

    public VideoEngineMediaPipelineFactory(
        IVideoPlayerFactory videoPlayerFactory)
    {
        this.videoPlayerFactory = videoPlayerFactory ?? throw new ArgumentNullException(nameof(videoPlayerFactory));
    }

    /// <inheritdoc />
    public IMediaPipeline Create(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var player = videoPlayerFactory.Create();
        var pipeline = new VideoEngineMediaPipeline(player);

        pipeline.Open(camera.BuildUri(), camera.Stream);

        return pipeline;
    }
}