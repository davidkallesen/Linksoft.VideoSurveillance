namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Factory that creates <see cref="FFmpegMediaPipeline"/> instances for the server edition.
/// </summary>
public sealed class FFmpegMediaPipelineFactory : IMediaPipelineFactory
{
    private readonly ILoggerFactory loggerFactory;
    private readonly IApplicationSettingsService settingsService;

    public FFmpegMediaPipelineFactory(
        ILoggerFactory loggerFactory,
        IApplicationSettingsService settingsService)
    {
        this.loggerFactory = loggerFactory;
        this.settingsService = settingsService;
    }

    /// <inheritdoc />
    public IMediaPipeline Create(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var transcodeCodec = RecordingPolicyHelper.ResolveTranscodeCodec(
            camera,
            settingsService.Recording.TranscodeVideoCodec);

        var pipeline = new FFmpegMediaPipeline(
            loggerFactory.CreateLogger<FFmpegMediaPipeline>(),
            transcodeCodec);

        pipeline.Open(camera.BuildUri(), camera.Stream);

        return pipeline;
    }
}