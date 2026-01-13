namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Root container for all application settings.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Gets or sets the general settings.
    /// </summary>
    public GeneralSettings General { get; set; } = new();

    /// <summary>
    /// Gets or sets the display settings.
    /// </summary>
    public DisplaySettings Display { get; set; } = new();
}