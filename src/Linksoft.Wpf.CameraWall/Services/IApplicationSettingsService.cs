namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for managing application settings persistence.
/// </summary>
public interface IApplicationSettingsService
{
    /// <summary>
    /// Gets the current general settings.
    /// </summary>
    GeneralSettings General { get; }

    /// <summary>
    /// Gets the current display settings.
    /// </summary>
    DisplaySettings Display { get; }

    /// <summary>
    /// Updates and saves the general settings.
    /// </summary>
    /// <param name="settings">The new settings to save.</param>
    void SaveGeneral(GeneralSettings settings);

    /// <summary>
    /// Updates and saves the display settings.
    /// </summary>
    /// <param name="settings">The new settings to save.</param>
    void SaveDisplay(DisplaySettings settings);

    /// <summary>
    /// Reloads settings from storage.
    /// </summary>
    void Load();

    /// <summary>
    /// Saves all settings to storage.
    /// </summary>
    void Save();
}