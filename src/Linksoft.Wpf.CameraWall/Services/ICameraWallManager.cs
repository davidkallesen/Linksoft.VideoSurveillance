namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Manager for camera wall operations including layouts, cameras, and state management.
/// </summary>
public interface ICameraWallManager : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the collection of available layouts.
    /// </summary>
    ObservableCollection<CameraLayout> Layouts { get; }

    /// <summary>
    /// Gets or sets the currently active layout.
    /// </summary>
    CameraLayout? CurrentLayout { get; set; }

    /// <summary>
    /// Gets the startup layout.
    /// </summary>
    CameraLayout? SelectedStartupLayout { get; }

    /// <summary>
    /// Gets the number of cameras in the current layout.
    /// </summary>
    int CameraCount { get; }

    /// <summary>
    /// Gets the number of connected cameras.
    /// </summary>
    int ConnectedCount { get; }

    /// <summary>
    /// Gets the current status text.
    /// </summary>
    string StatusText { get; }

    /// <summary>
    /// Gets the camera wall control reference.
    /// </summary>
    UserControls.CameraWall? CameraWall { get; }

    /// <summary>
    /// Occurs when the status text changes.
    /// </summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Initializes the manager with the camera wall control.
    /// </summary>
    /// <param name="cameraWallControl">The camera wall control.</param>
    void Initialize(UserControls.CameraWall cameraWallControl);

    /// <summary>
    /// Adds a new camera.
    /// </summary>
    void AddCamera();

    /// <summary>
    /// Edits an existing camera.
    /// </summary>
    /// <param name="camera">The camera to edit.</param>
    void EditCamera(CameraConfiguration camera);

    /// <summary>
    /// Deletes a camera after confirmation.
    /// </summary>
    /// <param name="camera">The camera to delete.</param>
    void DeleteCamera(CameraConfiguration camera);

    /// <summary>
    /// Shows a camera in full screen.
    /// </summary>
    /// <param name="camera">The camera to show.</param>
    void ShowFullScreen(CameraConfiguration camera);

    /// <summary>
    /// Refreshes all cameras.
    /// </summary>
    void RefreshAll();

    /// <summary>
    /// Creates a new layout.
    /// </summary>
    void CreateNewLayout();

    /// <summary>
    /// Deletes the current layout.
    /// </summary>
    void DeleteCurrentLayout();

    /// <summary>
    /// Sets the current layout as the startup layout.
    /// </summary>
    void SetCurrentAsStartup();

    /// <summary>
    /// Saves the current layout.
    /// </summary>
    void SaveCurrentLayout();

    /// <summary>
    /// Handles connection state changes.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    void OnConnectionStateChanged(CameraConnectionChangedEventArgs e);

    /// <summary>
    /// Handles position changes.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    void OnPositionChanged(CameraPositionChangedEventArgs e);

    /// <summary>
    /// Gets a value indicating whether a new layout can be created.
    /// </summary>
    bool CanCreateNewLayout { get; }

    /// <summary>
    /// Gets a value indicating whether the current layout can be deleted.
    /// </summary>
    bool CanDeleteCurrentLayout { get; }

    /// <summary>
    /// Gets a value indicating whether the current layout can be set as startup.
    /// </summary>
    bool CanSetCurrentAsStartup { get; }

    /// <summary>
    /// Gets a value indicating whether all cameras can be refreshed.
    /// </summary>
    bool CanRefreshAll { get; }

    /// <summary>
    /// Shows the about dialog.
    /// </summary>
    void ShowAboutDialog();

    /// <summary>
    /// Shows the check for updates dialog.
    /// </summary>
    void ShowCheckForUpdatesDialog();
}