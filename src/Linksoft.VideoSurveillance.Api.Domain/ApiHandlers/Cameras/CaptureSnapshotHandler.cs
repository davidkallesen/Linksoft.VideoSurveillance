namespace Linksoft.VideoSurveillance.Api.Domain.ApiHandlers.Cameras;

/// <summary>
/// Handler business logic for the CaptureSnapshot operation.
/// Creates a transient media pipeline to capture a single frame from the camera stream.
/// </summary>
public sealed class CaptureSnapshotHandler(
    ICameraStorageService storage,
    IMediaPipelineFactory pipelineFactory,
    IApplicationSettingsService settingsService) : ICaptureSnapshotHandler
{
    public async Task<CaptureSnapshotResult> ExecuteAsync(
        CaptureSnapshotParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var camera = storage.GetCameraById(parameters.CameraId);
        if (camera is null)
        {
            return CaptureSnapshotResult.NotFound($"Camera {parameters.CameraId} not found.");
        }

        using var pipeline = pipelineFactory.Create(camera);
        var frame = await pipeline.CaptureFrameAsync(cancellationToken);
        if (frame is null)
        {
            return CaptureSnapshotResult.NotFound("Failed to capture frame from camera stream.");
        }

        var snapshotPath = settingsService.CameraDisplay.SnapshotPath;
        Directory.CreateDirectory(snapshotPath);

        var safeName = string.IsNullOrWhiteSpace(camera.Display.DisplayName)
            ? camera.Id.ToString("N")[..8]
            : string.Join("_", camera.Display.DisplayName.Split(Path.GetInvalidFileNameChars()));
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var fileName = $"{safeName}_{timestamp}.png";
        var filePath = Path.Combine(snapshotPath, fileName);

        await File.WriteAllBytesAsync(filePath, frame, cancellationToken);

        return CaptureSnapshotResult.Ok(frame, "image/png", fileName);
    }
}