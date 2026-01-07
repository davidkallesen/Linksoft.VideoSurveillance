namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for checking GitHub releases for the Linksoft.CameraWall repository.
/// </summary>
[Registration(Lifetime.Singleton)]
public sealed class GitHubReleaseService : IGitHubReleaseService, IDisposable
{
    private const string GitHubApiUrl = "https://api.github.com/repos/davidkallesen/Linksoft.CameraWall/releases/latest";
    private const string UserAgent = "Linksoft-CameraWall";

    private readonly HttpClient httpClient;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubReleaseService"/> class.
    /// </summary>
    public GitHubReleaseService()
    {
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
    }

    /// <inheritdoc />
    public async Task<Version?> GetLatestVersionAsync()
    {
        try
        {
            var response = await httpClient
                .GetStringAsync(new Uri(GitHubApiUrl))
                .ConfigureAwait(false);

            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            if (root.TryGetProperty("tag_name", out var tagName))
            {
                var versionString = tagName.GetString();
                if (!string.IsNullOrEmpty(versionString))
                {
                    // Remove leading 'v' if present (e.g., "v1.0.0" -> "1.0.0")
                    if (versionString.StartsWith('v'))
                    {
                        versionString = versionString[1..];
                    }

                    if (Version.TryParse(versionString, out var version))
                    {
                        return version;
                    }
                }
            }
        }
        catch
        {
            // Silently fail - update check is not critical
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<Uri?> GetLatestReleaseUrlAsync()
    {
        try
        {
            var response = await httpClient
                .GetStringAsync(new Uri(GitHubApiUrl))
                .ConfigureAwait(false);

            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;

            if (root.TryGetProperty("html_url", out var htmlUrl))
            {
                var urlString = htmlUrl.GetString();
                if (!string.IsNullOrEmpty(urlString))
                {
                    return new Uri(urlString);
                }
            }
        }
        catch
        {
            // Silently fail - update check is not critical
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        httpClient.Dispose();
        disposed = true;
    }
}