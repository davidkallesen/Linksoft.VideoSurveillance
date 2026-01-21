namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Application-level advanced settings for debugging and logging.
/// </summary>
public class AdvancedSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled.
    /// </summary>
    public bool EnableDebugLogging { get; set; }

    /// <summary>
    /// Gets or sets the path to the log file.
    /// </summary>
    public string? LogFilePath { get; set; }
}