namespace Linksoft.VideoSurveillance.Api.Domain.ApiHandlers.Cameras;

/// <summary>
/// Handler business logic for the DeleteCamera operation.
/// </summary>
public sealed class DeleteCameraHandler(
    ICameraStorageService storage) : IDeleteCameraHandler
{
    public Task<DeleteCameraResult> ExecuteAsync(
        DeleteCameraParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var deleted = storage.DeleteCamera(parameters.CameraId);
        if (!deleted)
        {
            return Task.FromResult(DeleteCameraResult.NotFound($"Camera {parameters.CameraId} not found."));
        }

        storage.Save();

        return Task.FromResult(DeleteCameraResult.NoContent());
    }
}