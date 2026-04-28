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

        // The wrapping pipeline owns the player and disposes it transitively.
        // Pre-pipeline construction failures (or pipeline.Open throwing) used
        // to leak the half-constructed player/pipeline because the caller's
        // `using var` never received the reference.
        var player = videoPlayerFactory.Create();
        VideoEngineMediaPipeline? pipeline = null;
        try
        {
            pipeline = new VideoEngineMediaPipeline(player, camera.Display.DisplayName);
            pipeline.Open(camera.BuildUri(), camera.Stream);
            return pipeline;
        }
        catch
        {
            if (pipeline is not null)
            {
                pipeline.Dispose();
            }
            else
            {
                (player as IDisposable)?.Dispose();
            }

            throw;
        }
    }
}