namespace Linksoft.VideoSurveillance.Api.Handlers.Cameras;

public class CaptureSnapshotHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_CameraNotFound_ReturnsNotFound()
    {
        // Arrange
        var storage = Substitute.For<ICameraStorageService>();
        var pipelineFactory = Substitute.For<IMediaPipelineFactory>();
        var settingsService = Substitute.For<IApplicationSettingsService>();
        var cameraId = Guid.NewGuid();
        storage.GetCameraById(cameraId).Returns((CameraConfiguration?)null);
        var handler = new CaptureSnapshotHandler(storage, pipelineFactory, settingsService);

        // Act
        var result = await handler.ExecuteAsync(
            new CaptureSnapshotParameters(cameraId),
            CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFound<string>>();
    }

    [Fact]
    public async Task ExecuteAsync_CaptureReturnsNull_ReturnsNotFound()
    {
        // Arrange
        var storage = Substitute.For<ICameraStorageService>();
        var pipelineFactory = Substitute.For<IMediaPipelineFactory>();
        var settingsService = Substitute.For<IApplicationSettingsService>();
        var pipeline = Substitute.For<IMediaPipeline>();
        var cameraId = Guid.NewGuid();
        var camera = new CameraConfiguration { Id = cameraId };
        storage.GetCameraById(cameraId).Returns(camera);
        pipelineFactory.Create(camera).Returns(pipeline);
        pipeline.CaptureFrameAsync(Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        var handler = new CaptureSnapshotHandler(storage, pipelineFactory, settingsService);

        // Act
        var result = await handler.ExecuteAsync(
            new CaptureSnapshotParameters(cameraId),
            CancellationToken.None);

        // Assert
        pipelineFactory.Received(1).Create(camera);
        result.Result.Should().BeOfType<NotFound<string>>();
    }
}