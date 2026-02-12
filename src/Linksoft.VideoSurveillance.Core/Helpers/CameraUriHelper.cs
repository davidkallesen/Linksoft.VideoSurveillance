namespace Linksoft.VideoSurveillance.Helpers;

/// <summary>
/// Helper class for building camera stream URIs.
/// </summary>
public static class CameraUriHelper
{
    /// <summary>
    /// Builds a URI for the specified camera configuration.
    /// </summary>
    public static Uri BuildUri(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        return camera.BuildUri();
    }

    /// <summary>
    /// Builds a URI from individual components.
    /// </summary>
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
    public static int GetDefaultPort(CameraProtocol protocol) => protocol switch
    {
        CameraProtocol.Rtsp => 554,
        CameraProtocol.Http => 80,
        CameraProtocol.Https => 443,
        _ => 554,
    };

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

    private static string BuildPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return $"/{path.TrimStart('/')}";
    }
}