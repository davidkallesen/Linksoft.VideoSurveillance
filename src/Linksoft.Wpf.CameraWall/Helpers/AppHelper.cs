namespace Linksoft.Wpf.CameraWall.Helpers;

/// <summary>
/// Helper methods for application initialization.
/// </summary>
public static class AppHelper
{
    /// <summary>
    /// Renders a loading initialization message to the splash screen.
    /// </summary>
    /// <param name="logger">Optional logger for debug output.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="percentage">The progress percentage (0-100).</param>
    public static void RenderLoadingInitializeMessage(
        ILogger? logger,
        string message,
        int percentage)
    {
        var msg = "Initialize: " + message;
        MessageListener.Instance.ReceiveMessage(msg);
        PercentListener.Instance.ReceivePercent(percentage);
        logger?.LogDebug("{Message} - {Percentage}%", msg, percentage);
    }
}