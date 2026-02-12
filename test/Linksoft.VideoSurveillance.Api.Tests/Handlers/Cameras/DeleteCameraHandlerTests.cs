namespace Linksoft.VideoSurveillance.Api.Handlers.Cameras;

public class DeleteCameraHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_CameraExists_DeletesAndReturnsNoContent()
    {
        // Arrange
        var storage = Substitute.For<ICameraStorageService>();
        var cameraId = Guid.NewGuid();
        storage.DeleteCamera(cameraId).Returns(true);
        var handler = new DeleteCameraHandler(storage);

        // Act
        var result = await handler.ExecuteAsync(
            new DeleteCameraParameters(cameraId),
            CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NoContent>();
        storage.Received(1).DeleteCamera(cameraId);
        storage.Received(1).Save();
    }

    [Fact]
    public async Task ExecuteAsync_CameraNotFound_ReturnsNotFound()
    {
        // Arrange
        var storage = Substitute.For<ICameraStorageService>();
        var cameraId = Guid.NewGuid();
        storage.DeleteCamera(cameraId).Returns(false);
        var handler = new DeleteCameraHandler(storage);

        // Act
        var result = await handler.ExecuteAsync(
            new DeleteCameraParameters(cameraId),
            CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFound<string>>();
        storage.DidNotReceive().Save();
    }
}