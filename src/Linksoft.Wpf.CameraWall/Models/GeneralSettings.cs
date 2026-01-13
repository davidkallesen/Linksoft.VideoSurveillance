namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// General application settings.
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// Gets or sets the theme base (Dark or Light).
    /// </summary>
    public string ThemeBase { get; set; } = "Dark";

    /// <summary>
    /// Gets or sets the theme accent color.
    /// </summary>
    public string ThemeAccent { get; set; } = "Blue";

    /// <summary>
    /// Gets or sets the UI language LCID (e.g., "1033" for en-US).
    /// </summary>
    public string Language { get; set; } = "1033";

    /// <summary>
    /// Gets or sets a value indicating whether to connect cameras on startup.
    /// </summary>
    public bool ConnectCamerasOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to start the application maximized (fullscreen).
    /// </summary>
    public bool StartMaximized { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to start with the ribbon collapsed.
    /// </summary>
    public bool StartRibbonCollapsed { get; set; }

    /// <summary>
    /// Creates a deep copy of the settings.
    /// </summary>
    /// <returns>A new instance with copied values.</returns>
    public GeneralSettings Clone()
        => new()
        {
            ThemeBase = ThemeBase,
            ThemeAccent = ThemeAccent,
            Language = Language,
            ConnectCamerasOnStartup = ConnectCamerasOnStartup,
            StartMaximized = StartMaximized,
            StartRibbonCollapsed = StartRibbonCollapsed,
        };
}