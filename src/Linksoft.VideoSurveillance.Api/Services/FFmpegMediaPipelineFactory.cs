namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Factory that creates <see cref="FFmpegMediaPipeline"/> instances for the server edition.
/// </summary>
public sealed class FFmpegMediaPipelineFactory : IMediaPipelineFactory
{
    private readonly ILoggerFactory loggerFactory;

    public FFmpegMediaPipelineFactory(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IMediaPipeline Create(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var pipeline = new FFmpegMediaPipeline(
            loggerFactory.CreateLogger<FFmpegMediaPipeline>());

        pipeline.Open(camera.BuildUri(), camera.Stream);

        return pipeline;
    }
}