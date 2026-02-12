namespace Linksoft.VideoSurveillance.Api.Domain.ApiHandlers.Settings;

/// <summary>
/// Handler business logic for the UpdateSettings operation.
/// </summary>
public sealed class UpdateSettingsHandler(
    IApplicationSettingsService settingsService) : IUpdateSettingsHandler
{
    public Task<UpdateSettingsResult> ExecuteAsync(
        UpdateSettingsParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var general = settingsService.General;
        var recording = settingsService.Recording;
        var advanced = settingsService.Advanced;

        parameters.Request.ApplyToCore(general, recording, advanced);

        settingsService.SaveGeneral(general);
        settingsService.SaveRecording(recording);
        settingsService.SaveAdvanced(advanced);

        if (!string.IsNullOrEmpty(parameters.Request.SnapshotPath))
        {
            var display = settingsService.CameraDisplay;
            display.SnapshotPath = parameters.Request.SnapshotPath;
            settingsService.SaveCameraDisplay(display);
        }

        var updated = SettingsMappingExtensions.ToApiModel(
            settingsService.General,
            settingsService.Recording,
            settingsService.Advanced,
            settingsService.CameraDisplay.SnapshotPath);

        return Task.FromResult(UpdateSettingsResult.Ok(updated));
    }
}