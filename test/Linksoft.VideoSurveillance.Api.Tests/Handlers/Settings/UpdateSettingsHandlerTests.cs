namespace Linksoft.VideoSurveillance.Api.Handlers.Settings;

public class UpdateSettingsHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_AppliesSettingsAndReturnsOk()
    {
        // Arrange
        var settingsService = Substitute.For<IApplicationSettingsService>();
        var general = new GeneralSettings();
        var recording = new RecordingSettings();
        var advanced = new AdvancedSettings();
        var display = new CameraDisplayAppSettings();
        settingsService.General.Returns(general);
        settingsService.Recording.Returns(recording);
        settingsService.Advanced.Returns(advanced);
        settingsService.CameraDisplay.Returns(display);
        var handler = new UpdateSettingsHandler(settingsService);

        var apiSettings = new AppSettings(
            ThemeBase: AppSettingsThemeBase.Light,
            Language: "1030",
            ConnectOnStartup: false,
            RecordingPath: @"D:\recordings",
            RecordingFormat: AppSettingsRecordingFormat.Mkv,
            SnapshotPath: @"D:\snapshots",
            EnableDebugLogging: true,
            LogPath: @"D:\logs");

        // Act
        var result = await handler.ExecuteAsync(
            new UpdateSettingsParameters(apiSettings),
            CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<Ok<AppSettings>>();
        settingsService.Received(1).SaveGeneral(general);
        settingsService.Received(1).SaveRecording(recording);
        settingsService.Received(1).SaveAdvanced(advanced);
        settingsService.Received(1).SaveCameraDisplay(display);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutSnapshotPath_DoesNotSaveDisplay()
    {
        // Arrange
        var settingsService = Substitute.For<IApplicationSettingsService>();
        settingsService.General.Returns(new GeneralSettings());
        settingsService.Recording.Returns(new RecordingSettings());
        settingsService.Advanced.Returns(new AdvancedSettings());
        settingsService.CameraDisplay.Returns(new CameraDisplayAppSettings());
        var handler = new UpdateSettingsHandler(settingsService);

        var apiSettings = new AppSettings(
            ThemeBase: AppSettingsThemeBase.Dark,
            Language: "1033",
            ConnectOnStartup: true,
            RecordingPath: @"C:\rec",
            RecordingFormat: AppSettingsRecordingFormat.Mp4,
            SnapshotPath: null!,
            EnableDebugLogging: false,
            LogPath: @"C:\logs");

        // Act
        await handler.ExecuteAsync(
            new UpdateSettingsParameters(apiSettings),
            CancellationToken.None);

        // Assert
        settingsService.Received(1).SaveGeneral(Arg.Any<GeneralSettings>());
        settingsService.Received(1).SaveRecording(Arg.Any<RecordingSettings>());
        settingsService.Received(1).SaveAdvanced(Arg.Any<AdvancedSettings>());
        settingsService.DidNotReceive().SaveCameraDisplay(Arg.Any<CameraDisplayAppSettings>());
    }
}