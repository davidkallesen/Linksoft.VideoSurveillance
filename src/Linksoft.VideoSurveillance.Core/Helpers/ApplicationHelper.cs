namespace Linksoft.VideoSurveillance.Helpers;

/// <summary>
/// Provides the application version from the assembly informational version attribute.
/// </summary>
public static class ApplicationHelper
{
    /// <summary>
    /// Gets the semantic version string from <see cref="AssemblyInformationalVersionAttribute"/>,
    /// stripping any +metadata suffix (e.g., "1.0.4+abcdef" becomes "1.0.4").
    /// Falls back to <see cref="AssemblyName.Version"/> if the informational version is unavailable.
    /// </summary>
    /// <returns>The version string (e.g., "1.0.4").</returns>
    public static string GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (infoVersion is not null)
        {
            // Strip +commithash metadata (e.g., "1.0.4+abcdef" -> "1.0.4")
            var plusIndex = infoVersion.IndexOf('+', StringComparison.Ordinal);
            if (plusIndex > 0)
            {
                infoVersion = infoVersion[..plusIndex];
            }

            return infoVersion;
        }

        // Fallback to AssemblyVersion
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "1.0.0";
    }
}