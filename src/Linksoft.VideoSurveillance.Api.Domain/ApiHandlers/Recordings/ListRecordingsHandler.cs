namespace Linksoft.VideoSurveillance.Api.Domain.ApiHandlers.Recordings;

/// <summary>
/// Handler business logic for the ListRecordings operation.
/// </summary>
public sealed class ListRecordingsHandler(
    IRecordingService recordingService,
    IApplicationSettingsService settingsService,
    ICameraStorageService cameraStorageService) : IListRecordingsHandler
{
    private static readonly string[] VideoExtensions = [".mp4", ".mkv"];

    public Task<ListRecordingsResult> ExecuteAsync(
        ListRecordingsParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var cameras = cameraStorageService.GetAllCameras();
        var cameraNames = cameras.ToDictionary(c => c.Id, c => c.Display.DisplayName);

        // Active recording sessions
        var sessions = recordingService.GetActiveSessions();
        var activeRecordings = sessions
            .Where(s => parameters.CameraId is null || s.CameraId == parameters.CameraId)
            .Select(s => new Recording(
                Id: s.CameraId,
                CameraId: s.CameraId,
                CameraName: cameraNames.GetValueOrDefault(s.CameraId, string.Empty),
                FilePath: s.CurrentFilePath,
                StartedAt: new DateTimeOffset(s.StartTime, TimeSpan.Zero),
                Duration: s.Duration.ToString("c"),
                FileSizeBytes: 0,
                HasThumbnail: false))
            .ToList();

        // Historical recordings from filesystem
        var recordingPath = settingsService.Recording.RecordingPath;
        if (Directory.Exists(recordingPath))
        {
            var activeFilePaths = new HashSet<string>(
                sessions.Select(s => s.CurrentFilePath),
                StringComparer.OrdinalIgnoreCase);

            var files = Directory.EnumerateFiles(recordingPath, "*.*", SearchOption.AllDirectories)
                .Where(f => VideoExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .Where(f => !activeFilePaths.Contains(f));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var relativePath = Path.GetRelativePath(recordingPath, file).Replace('\\', '/');

                // Try to match camera by folder name or filename pattern
                var cameraId = Guid.Empty;
                var cameraName = string.Empty;
                foreach (var camera in cameras)
                {
                    if (file.Contains(camera.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        cameraId = camera.Id;
                        cameraName = camera.Display.DisplayName;
                        break;
                    }
                }

                if (parameters.CameraId is not null && cameraId != parameters.CameraId)
                {
                    continue;
                }

                activeRecordings.Add(new Recording(
                    Id: Guid.NewGuid(),
                    CameraId: cameraId,
                    CameraName: cameraName,
                    FilePath: relativePath,
                    StartedAt: new DateTimeOffset(fileInfo.CreationTimeUtc, TimeSpan.Zero),
                    Duration: string.Empty,
                    FileSizeBytes: fileInfo.Length,
                    HasThumbnail: false));
            }
        }

        var sorted = activeRecordings
            .OrderByDescending(r => r.StartedAt)
            .ToList();

        return Task.FromResult(ListRecordingsResult.Ok(sorted));
    }
}