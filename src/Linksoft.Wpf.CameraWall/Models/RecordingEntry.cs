namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents a recorded video file entry.
/// </summary>
public class RecordingEntry
{
    /// <summary>
    /// Gets the full file path of the recording.
    /// </summary>
    required public string FilePath { get; init; }

    /// <summary>
    /// Gets the name of the camera that made the recording.
    /// </summary>
    required public string CameraName { get; init; }

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the date and time when the recording was made.
    /// </summary>
    public DateTime RecordingTime { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets the formatted file size string.
    /// </summary>
    public string FormattedFileSize => FormatFileSize(FileSizeBytes);

    /// <summary>
    /// Gets the formatted recording time string.
    /// </summary>
    public string FormattedRecordingTime
        => RecordingTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets the thumbnail file path (same as video path but with .png extension).
    /// </summary>
    public string ThumbnailPath => Path.ChangeExtension(FilePath, ".png");

    /// <summary>
    /// Gets a value indicating whether a thumbnail file exists for this recording.
    /// </summary>
    public bool HasThumbnail => File.Exists(ThumbnailPath);

    private static string FormatFileSize(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        return bytes switch
        {
            >= gb => $"{bytes / (double)gb:F2} GB",
            >= mb => $"{bytes / (double)mb:F2} MB",
            >= kb => $"{bytes / (double)kb:F2} KB",
            _ => $"{bytes} B",
        };
    }
}