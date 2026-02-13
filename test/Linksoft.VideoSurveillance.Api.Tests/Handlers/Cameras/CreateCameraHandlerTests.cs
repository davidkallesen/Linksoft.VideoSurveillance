namespace Linksoft.VideoSurveillance.Api.Handlers.Cameras;

public class CreateCameraHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ValidRequest_ReturnsCreatedWithMappedCamera()
    {
        // Arrange
        var storage = Substitute.For<ICameraStorageService>();
        var handler = new CreateCameraHandler(storage);
        var request = new CreateCameraRequest(
            DisplayName: "Test Camera",
            Description: "A test camera",
            IpAddress: "10.0.0.1",
            Protocol: ApiCameraProtocol.Rtsp,
            Path: "stream1",
            Username: "admin",
            Password: "pass",
            OverlayPosition: null,
            StreamUseLowLatencyMode: true,
            StreamMaxLatencyMs: 500,
            StreamRtspTransport: null,
            StreamBufferDurationMs: 0,
            Port: 554);
        var parameters = new CreateCameraParameters(request);

        // Act
        var result = await handler.ExecuteAsync(parameters, CancellationToken.None);

        // Assert
        var created = result.Result.Should().BeOfType<Created<Camera>>().Subject;
        created.Value.Should().NotBeNull();
        created.Value!.DisplayName.Should().Be("Test Camera");
        created.Value.IpAddress.Should().Be("10.0.0.1");
        created.Value.Port.Should().Be(554);
        created.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_CallsStorageAddAndSave()
    {
        // Arrange
        var storage = Substitute.For<ICameraStorageService>();
        var handler = new CreateCameraHandler(storage);
        var request = new CreateCameraRequest(
            DisplayName: "New Cam",
            Description: null!,
            IpAddress: "192.168.1.50",
            Protocol: null,
            Path: null!,
            Username: null!,
            Password: null!,
            OverlayPosition: null,
            StreamUseLowLatencyMode: true,
            StreamMaxLatencyMs: 500,
            StreamRtspTransport: null,
            StreamBufferDurationMs: 0,
            Port: 554);
        var parameters = new CreateCameraParameters(request);

        // Act
        await handler.ExecuteAsync(parameters, CancellationToken.None);

        // Assert
        storage.Received(1).AddOrUpdateCamera(Arg.Is<CameraConfiguration>(c =>
            c.Display.DisplayName == "New Cam" &&
            c.Connection.IpAddress == "192.168.1.50"));
        storage.Received(1).Save();
    }
}