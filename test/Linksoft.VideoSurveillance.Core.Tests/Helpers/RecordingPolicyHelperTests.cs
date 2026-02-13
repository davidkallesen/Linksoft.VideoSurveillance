namespace Linksoft.VideoSurveillance.Helpers;

public class RecordingPolicyHelperTests
{
    [Fact]
    public void ShouldRecordOnConnect_Override_True_AppDefault_False_Returns_True()
    {
        // Arrange
        var camera = new CameraConfiguration();
        camera.Overrides.Recording.EnableRecordingOnConnect = true;

        // Act
        var result = RecordingPolicyHelper.ShouldRecordOnConnect(camera, appDefault: false);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRecordOnConnect_Override_False_AppDefault_True_Returns_False()
    {
        // Arrange
        var camera = new CameraConfiguration();
        camera.Overrides.Recording.EnableRecordingOnConnect = false;

        // Act
        var result = RecordingPolicyHelper.ShouldRecordOnConnect(camera, appDefault: true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRecordOnConnect_Override_Null_AppDefault_True_Returns_True()
    {
        // Arrange
        var camera = new CameraConfiguration();

        // Act
        var result = RecordingPolicyHelper.ShouldRecordOnConnect(camera, appDefault: true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldRecordOnConnect_Override_Null_AppDefault_False_Returns_False()
    {
        // Arrange
        var camera = new CameraConfiguration();

        // Act
        var result = RecordingPolicyHelper.ShouldRecordOnConnect(camera, appDefault: false);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldRecordOnConnect_Null_Camera_Throws()
    {
        // Act
        var act = () => RecordingPolicyHelper.ShouldRecordOnConnect(null!, appDefault: true);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}