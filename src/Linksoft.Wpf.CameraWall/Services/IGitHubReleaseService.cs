namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for checking GitHub releases.
/// </summary>
public interface IGitHubReleaseService
{
    /// <summary>
    /// Gets the latest version from GitHub releases.
    /// </summary>
    /// <returns>The latest version, or null if unable to retrieve.</returns>
    Task<Version?> GetLatestVersionAsync();

    /// <summary>
    /// Gets the download URL for the latest release.
    /// </summary>
    /// <returns>The download URL, or null if unable to retrieve.</returns>
    Task<Uri?> GetLatestReleaseUrlAsync();
}