namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for displaying dialogs.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows the camera configuration dialog.
    /// </summary>
    /// <param name="camera">The camera to edit, or null for a new camera.</param>
    /// <param name="isNew">Whether this is a new camera.</param>
    /// <param name="existingIpAddresses">Existing IP addresses to validate uniqueness against.</param>
    /// <returns>The configured camera if saved, or null if cancelled.</returns>
    CameraConfiguration? ShowCameraConfigurationDialog(
        CameraConfiguration? camera,
        bool isNew,
        IReadOnlyCollection<string> existingIpAddresses);

    /// <summary>
    /// Shows an input box dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="defaultText">The default input text.</param>
    /// <returns>The entered text if OK was clicked, or null if cancelled.</returns>
    string? ShowInputBox(
        string title,
        string prompt,
        string defaultText = "");

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    /// <param name="message">The confirmation message.</param>
    /// <param name="title">The dialog title.</param>
    /// <returns>True if confirmed, false otherwise.</returns>
    bool ShowConfirmation(
        string message,
        string title);

    /// <summary>
    /// Shows an error message dialog.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="title">The dialog title. Defaults to localized "Error" if null.</param>
    void ShowError(
        string message,
        string? title = null);

    /// <summary>
    /// Shows an information message dialog.
    /// </summary>
    /// <param name="message">The information message.</param>
    /// <param name="title">The dialog title. Defaults to localized "Information" if null.</param>
    void ShowInfo(
        string message,
        string? title = null);

    /// <summary>
    /// Shows the about dialog with application information.
    /// </summary>
    void ShowAboutDialog();

    /// <summary>
    /// Shows the check for updates dialog.
    /// </summary>
    void ShowCheckForUpdatesDialog();

    /// <summary>
    /// Shows a camera in fullscreen mode.
    /// </summary>
    /// <param name="camera">The camera to display.</param>
    void ShowFullScreenCamera(CameraConfiguration camera);

    /// <summary>
    /// Shows the settings dialog.
    /// </summary>
    /// <returns>True if settings were saved, false if cancelled.</returns>
    bool ShowSettingsDialog();
}