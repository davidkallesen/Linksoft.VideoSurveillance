namespace Linksoft.Wpf.CameraWall.App.Configuration;

/// <summary>
/// UI configuration options.
/// </summary>
public class ApplicationUiOptions
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
    /// Gets or sets the UI language.
    /// </summary>
    public string Language { get; set; } = "en-US";
}