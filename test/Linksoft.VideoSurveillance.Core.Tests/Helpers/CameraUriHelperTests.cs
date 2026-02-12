namespace Linksoft.VideoSurveillance.Helpers;

public class CameraUriHelperTests
{
    [Fact]
    public void BuildUri_WithCameraConfiguration_Returns_Valid_Uri()
    {
        // Arrange
        var camera = new CameraConfiguration();
        camera.Connection.IpAddress = "192.168.1.10";
        camera.Connection.Port = 554;
        camera.Connection.Protocol = CameraProtocol.Rtsp;
        camera.Connection.Path = "stream1";

        // Act
        var uri = CameraUriHelper.BuildUri(camera);

        // Assert
        uri.ToString().Should().Be("rtsp://192.168.1.10:554/stream1");
    }

    [Fact]
    public void BuildUri_WithCameraConfiguration_Null_Throws()
    {
        // Act
        var act = () => CameraUriHelper.BuildUri((CameraConfiguration)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildUri_FromComponents_Returns_Valid_Rtsp_Uri()
    {
        // Act
        var uri = CameraUriHelper.BuildUri(
            CameraProtocol.Rtsp,
            "192.168.1.10",
            554,
            "stream1");

        // Assert
        uri.ToString().Should().Be("rtsp://192.168.1.10:554/stream1");
    }

    [Fact]
    public void BuildUri_FromComponents_Http_Returns_Valid_Uri()
    {
        // Act
        var uri = CameraUriHelper.BuildUri(
            CameraProtocol.Http,
            "10.0.0.1",
            80);

        // Assert
        uri.Scheme.Should().Be("http");
        uri.Host.Should().Be("10.0.0.1");
        uri.Port.Should().Be(80);
    }

    [Fact]
    public void BuildUri_FromComponents_WithCredentials_Includes_UserInfo()
    {
        // Act
        var uri = CameraUriHelper.BuildUri(
            CameraProtocol.Rtsp,
            "192.168.1.10",
            554,
            "stream1",
            "admin",
            "password");

        // Assert
        uri.ToString().Should().Contain("admin:password@");
        uri.ToString().Should().StartWith("rtsp://");
    }

    [Fact]
    public void BuildUri_FromComponents_WithoutPath_Omits_Path()
    {
        // Act
        var uri = CameraUriHelper.BuildUri(
            CameraProtocol.Rtsp,
            "192.168.1.10",
            554);

        // Assert
        uri.Host.Should().Be("192.168.1.10");
        uri.Port.Should().Be(554);
    }

    [Fact]
    public void BuildUri_FromComponents_WithLeadingSlashPath_Normalizes()
    {
        // Act
        var uri = CameraUriHelper.BuildUri(
            CameraProtocol.Rtsp,
            "192.168.1.10",
            554,
            "/stream1");

        // Assert
        uri.AbsolutePath.Should().Be("/stream1");
    }

    [Theory]
    [InlineData(CameraProtocol.Rtsp, 554)]
    [InlineData(CameraProtocol.Http, 80)]
    [InlineData(CameraProtocol.Https, 443)]
    public void GetDefaultPort_Returns_Expected_Port(
        CameraProtocol protocol,
        int expectedPort)
    {
        // Act
        var port = CameraUriHelper.GetDefaultPort(protocol);

        // Assert
        port.Should().Be(expectedPort);
    }

    [Fact]
    public void GetDefaultPort_UnknownProtocol_Returns_554()
    {
        // Arrange
        var unknown = (CameraProtocol)999;

        // Act
        var port = CameraUriHelper.GetDefaultPort(unknown);

        // Assert
        port.Should().Be(554);
    }
}