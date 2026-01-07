namespace Linksoft.Wpf.CameraWall.Helpers;

/// <summary>
/// Helper class for building camera stream URIs.
/// </summary>
public static class CameraUriHelper
{
    /// <summary>
    /// Builds a URI for the specified camera configuration.
    /// </summary>
    /// <param name="camera">The camera configuration.</param>
    /// <returns>The constructed URI.</returns>
    public static Uri BuildUri(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return camera.BuildUri();
    }

    /// <summary>
    /// Builds a URI from individual components.
    /// </summary>
    /// <param name="protocol">The streaming protocol.</param>
    /// <param name="ipAddress">The IP address of the camera.</param>
    /// <param name="port">The port number.</param>
    /// <param name="path">The stream path (optional).</param>
    /// <param name="userName">The username for authentication (optional).</param>
    /// <param name="password">The password for authentication (optional).</param>
    /// <returns>The constructed URI.</returns>
    public static Uri BuildUri(
        CameraProtocol protocol,
        string ipAddress,
        int port,
        string? path = null,
        string? userName = null,
        string? password = null)
    {
        var scheme = protocol.ToScheme();
        var userInfo = BuildUserInfo(userName, password);
        var pathSegment = BuildPath(path);

        return new Uri($"{scheme}://{userInfo}{ipAddress}:{port}{pathSegment}");
    }

    /// <summary>
    /// Gets the default port for the specified protocol.
    /// </summary>
    /// <param name="protocol">The camera protocol.</param>
    /// <returns>The default port number.</returns>
    public static int GetDefaultPort(CameraProtocol protocol) => protocol switch
    {
        CameraProtocol.Rtsp => 554,
        CameraProtocol.Http => 80,
        CameraProtocol.Https => 443,
        _ => 554,
    };

    /// <summary>
    /// Builds the user info portion of a URI.
    /// </summary>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The user info string including the @ symbol, or empty string if no credentials.</returns>
    private static string BuildUserInfo(
        string? userName,
        string? password)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return string.Empty;
        }

        var escapedUser = Uri.EscapeDataString(userName);
        var escapedPassword = Uri.EscapeDataString(password ?? string.Empty);

        return $"{escapedUser}:{escapedPassword}@";
    }

    /// <summary>
    /// Builds the path portion of a URI.
    /// </summary>
    /// <param name="path">The path string.</param>
    /// <returns>The normalized path starting with /, or empty string if no path.</returns>
    private static string BuildPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return $"/{path.TrimStart('/')}";
    }
}