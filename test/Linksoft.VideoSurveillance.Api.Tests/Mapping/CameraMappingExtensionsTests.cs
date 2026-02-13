namespace Linksoft.VideoSurveillance.Api.Mapping;

public class CameraMappingExtensionsTests
{
    [Fact]
    public void ToApiModel_MapsAllFieldsCorrectly()
    {
        // Arrange
        var core = new CameraConfiguration();
        core.Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        core.Display.DisplayName = "Front Door";
        core.Display.Description = "Main entrance camera";
        core.Connection.IpAddress = "192.168.1.10";
        core.Connection.Port = 554;
        core.Connection.Protocol = CoreCameraProtocol.Rtsp;
        core.Connection.Path = "stream1";
        core.Authentication.UserName = "admin";

        // Act
        var api = core.ToApiModel();

        // Assert
        api.Id.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        api.DisplayName.Should().Be("Front Door");
        api.Description.Should().Be("Main entrance camera");
        api.IpAddress.Should().Be("192.168.1.10");
        api.Port.Should().Be(554);
        api.Protocol.Should().Be(ApiCameraProtocol.Rtsp);
        api.Path.Should().Be("stream1");
        api.Username.Should().Be("admin");
        api.ConnectionState.Should().BeNull();
        api.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void ToApiModel_WithConnectionStateAndRecording_MapsOptionalFields()
    {
        // Arrange
        var core = new CameraConfiguration();
        core.Display.DisplayName = "Test";
        core.Connection.IpAddress = "10.0.0.1";

        // Act
        var api = core.ToApiModel(
            connectionState: ConnectionState.Connected,
            isRecording: true);

        // Assert
        api.ConnectionState.Should().Be(CameraConnectionState.Connected);
        api.IsRecording.Should().BeTrue();
    }

    [Fact]
    public void ToCoreModel_MapsCreateRequestToConfiguration()
    {
        // Arrange
        var request = new CreateCameraRequest(
            DisplayName: "New Camera",
            Description: "Test description",
            IpAddress: "10.0.0.5",
            Protocol: ApiCameraProtocol.Http,
            Path: "/mjpeg",
            Username: "user",
            Password: "secret",
            OverlayPosition: null,
            StreamUseLowLatencyMode: true,
            StreamMaxLatencyMs: 500,
            StreamRtspTransport: null,
            StreamBufferDurationMs: 0,
            Port: 8080);

        // Act
        var core = request.ToCoreModel();

        // Assert
        core.Display.DisplayName.Should().Be("New Camera");
        core.Display.Description.Should().Be("Test description");
        core.Connection.IpAddress.Should().Be("10.0.0.5");
        core.Connection.Port.Should().Be(8080);
        core.Connection.Protocol.Should().Be(CoreCameraProtocol.Http);
        core.Connection.Path.Should().Be("/mjpeg");
        core.Authentication.UserName.Should().Be("user");
        core.Authentication.Password.Should().Be("secret");
        core.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ToCoreModel_NullProtocol_DefaultsToRtsp()
    {
        // Arrange
        var request = new CreateCameraRequest(
            DisplayName: "Cam",
            Description: null!,
            IpAddress: "10.0.0.1",
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

        // Act
        var core = request.ToCoreModel();

        // Assert
        core.Connection.Protocol.Should().Be(CoreCameraProtocol.Rtsp);
    }

    [Fact]
    public void ApplyUpdate_OnlyChangesProvidedFields()
    {
        // Arrange
        var core = new CameraConfiguration();
        core.Display.DisplayName = "Original";
        core.Display.Description = "Original desc";
        core.Connection.IpAddress = "192.168.1.1";
        core.Connection.Port = 554;

        var request = new UpdateCameraRequest(
            DisplayName: "Updated",
            Description: null!,
            IpAddress: null!,
            Port: 0,
            Protocol: null,
            Path: null!,
            Username: null!,
            Password: null!,
            OverlayPosition: null,
            StreamUseLowLatencyMode: true,
            StreamMaxLatencyMs: 500,
            StreamRtspTransport: null,
            StreamBufferDurationMs: 0);

        // Act
        core.ApplyUpdate(request);

        // Assert
        core.Display.DisplayName.Should().Be("Updated");
        core.Display.Description.Should().Be("Original desc");
        core.Connection.IpAddress.Should().Be("192.168.1.1");
        core.Connection.Port.Should().Be(554);
    }

    [Theory]
    [InlineData(CoreCameraProtocol.Rtsp, ApiCameraProtocol.Rtsp)]
    [InlineData(CoreCameraProtocol.Http, ApiCameraProtocol.Http)]
    [InlineData(CoreCameraProtocol.Https, ApiCameraProtocol.Https)]
    public void ToApiProtocol_MapsCorrectly(
        CoreCameraProtocol core,
        ApiCameraProtocol expected)
    {
        // Act & Assert
        core.ToApiProtocol().Should().Be(expected);
    }

    [Theory]
    [InlineData(ApiCameraProtocol.Rtsp, CoreCameraProtocol.Rtsp)]
    [InlineData(ApiCameraProtocol.Http, CoreCameraProtocol.Http)]
    [InlineData(ApiCameraProtocol.Https, CoreCameraProtocol.Https)]
    public void ToCoreProtocol_MapsCorrectly(
        ApiCameraProtocol api,
        CoreCameraProtocol expected)
    {
        // Act & Assert
        api.ToCoreProtocol().Should().Be(expected);
    }

    [Theory]
    [InlineData(ConnectionState.Disconnected, CameraConnectionState.Disconnected)]
    [InlineData(ConnectionState.Connecting, CameraConnectionState.Connecting)]
    [InlineData(ConnectionState.Connected, CameraConnectionState.Connected)]
    [InlineData(ConnectionState.Reconnecting, CameraConnectionState.Reconnecting)]
    [InlineData(ConnectionState.Error, CameraConnectionState.Error)]
    public void ToApiConnectionState_MapsCorrectly(
        ConnectionState core,
        CameraConnectionState expected)
    {
        // Act & Assert
        core.ToApiConnectionState().Should().Be(expected);
    }

    [Theory]
    [InlineData(RecordingState.Idle, RecordingStatusState.Idle)]
    [InlineData(RecordingState.Recording, RecordingStatusState.Recording)]
    [InlineData(RecordingState.RecordingMotion, RecordingStatusState.RecordingMotion)]
    [InlineData(RecordingState.RecordingPostMotion, RecordingStatusState.RecordingPostMotion)]
    public void ToApiRecordingState_MapsCorrectly(
        RecordingState core,
        RecordingStatusState expected)
    {
        // Act & Assert
        core.ToApiRecordingState().Should().Be(expected);
    }
}