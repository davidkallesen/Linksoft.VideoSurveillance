namespace Linksoft.VideoSurveillance.Api.Mapping;

public class SettingsMappingExtensionsTests
{
    [Fact]
    public void ToApiModel_CombinesAllSettingsSources()
    {
        // Arrange
        var general = new GeneralSettings
        {
            ThemeBase = "Dark",
            Language = "1033",
            ConnectCamerasOnStartup = true,
        };
        var recording = new RecordingSettings
        {
            RecordingPath = @"C:\recordings",
            RecordingFormat = "mp4",
        };
        var advanced = new AdvancedSettings
        {
            EnableDebugLogging = true,
            LogPath = @"C:\logs",
        };

        // Act
        var api = SettingsMappingExtensions.ToApiModel(general, recording, advanced, @"C:\snapshots");

        // Assert
        api.ThemeBase.Should().Be(AppSettingsThemeBase.Dark);
        api.Language.Should().Be("1033");
        api.ConnectOnStartup.Should().BeTrue();
        api.RecordingPath.Should().Be(@"C:\recordings");
        api.RecordingFormat.Should().Be(AppSettingsRecordingFormat.Mp4);
        api.SnapshotPath.Should().Be(@"C:\snapshots");
        api.EnableDebugLogging.Should().BeTrue();
        api.LogPath.Should().Be(@"C:\logs");
    }

    [Fact]
    public void ToApiModel_LightTheme_MapsCorrectly()
    {
        // Arrange
        var general = new GeneralSettings { ThemeBase = "Light" };

        // Act
        var api = SettingsMappingExtensions.ToApiModel(
            general,
            new RecordingSettings(),
            new AdvancedSettings(),
            string.Empty);

        // Assert
        api.ThemeBase.Should().Be(AppSettingsThemeBase.Light);
    }

    [Theory]
    [InlineData("mp4", AppSettingsRecordingFormat.Mp4)]
    [InlineData("mkv", AppSettingsRecordingFormat.Mkv)]
    [InlineData("avi", AppSettingsRecordingFormat.Avi)]
    [InlineData("MKV", AppSettingsRecordingFormat.Mkv)]
    public void ToApiModel_RecordingFormat_ParsesCorrectly(
        string format,
        AppSettingsRecordingFormat expected)
    {
        // Arrange
        var recording = new RecordingSettings { RecordingFormat = format };

        // Act
        var api = SettingsMappingExtensions.ToApiModel(
            new GeneralSettings(),
            recording,
            new AdvancedSettings(),
            string.Empty);

        // Assert
        api.RecordingFormat.Should().Be(expected);
    }

    [Fact]
    public void ApplyToCore_AppliesAllFields()
    {
        // Arrange
        var general = new GeneralSettings();
        var recording = new RecordingSettings();
        var advanced = new AdvancedSettings();

        var api = new AppSettings(
            ThemeBase: AppSettingsThemeBase.Light,
            Language: "1030",
            ConnectOnStartup: false,
            RecordingPath: @"D:\rec",
            RecordingFormat: AppSettingsRecordingFormat.Mkv,
            SnapshotPath: null!,
            EnableDebugLogging: true,
            LogPath: @"D:\logs");

        // Act
        api.ApplyToCore(general, recording, advanced);

        // Assert
        general.ThemeBase.Should().Be("Light");
        general.Language.Should().Be("1030");
        general.ConnectCamerasOnStartup.Should().BeFalse();
        recording.RecordingPath.Should().Be(@"D:\rec");
        recording.RecordingFormat.Should().Be("mkv");
        advanced.EnableDebugLogging.Should().BeTrue();
        advanced.LogPath.Should().Be(@"D:\logs");
    }

    [Fact]
    public void ApplyToCore_NullTheme_DoesNotChangeTheme()
    {
        // Arrange
        var general = new GeneralSettings { ThemeBase = "Dark" };

        var api = new AppSettings(
            ThemeBase: null,
            Language: null!,
            ConnectOnStartup: true,
            RecordingPath: null!,
            RecordingFormat: null,
            SnapshotPath: null!,
            EnableDebugLogging: false,
            LogPath: null!);

        // Act
        api.ApplyToCore(general, new RecordingSettings(), new AdvancedSettings());

        // Assert
        general.ThemeBase.Should().Be("Dark");
    }
}