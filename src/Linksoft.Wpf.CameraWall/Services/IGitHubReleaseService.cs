// ReSharper disable ArrangeTypeMemberModifiers
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
    /// Gets the download URL for the latest release page.
    /// </summary>
    /// <returns>The release page URL, or null if unable to retrieve.</returns>
    Task<Uri?> GetLatestReleaseUrlAsync();

    /// <summary>
    /// Gets the direct download URL for the latest MSI installer asset.
    /// Falls back to the release page URL if no MSI asset is found.
    /// </summary>
    /// <returns>The MSI download URL, or null if unable to retrieve.</returns>
    Task<Uri?> GetLatestMsiDownloadUrlAsync();
}