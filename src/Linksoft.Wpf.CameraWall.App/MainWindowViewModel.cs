namespace Linksoft.Wpf.CameraWall.App;

/// <summary>
/// View model for the main window. Acts as a thin binding layer between the UI and the camera wall manager.
/// </summary>
public partial class MainWindowViewModel : MainWindowViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="cameraWallManager">The camera wall manager.</param>
    /// <param name="settingsService">The application settings service.</param>
    public MainWindowViewModel(
        ICameraWallManager cameraWallManager,
        IApplicationSettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(cameraWallManager);
        ArgumentNullException.ThrowIfNull(settingsService);

        Manager = cameraWallManager;
        Manager.PropertyChanged += OnManagerPropertyChanged;

        IsRibbonMinimized = settingsService.General.StartRibbonCollapsed;
    }

    /// <summary>
    /// Gets the camera wall manager.
    /// </summary>
    public ICameraWallManager Manager { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the ribbon is minimized (collapsed).
    /// </summary>
    [ObservableProperty]
    private bool isRibbonMinimized;

    /// <summary>
    /// Gets the collection of available layouts.
    /// </summary>
    public ObservableCollection<CameraLayout> Layouts
        => Manager.Layouts;

    /// <summary>
    /// Gets or sets the currently active layout.
    /// </summary>
    public CameraLayout? CurrentLayout
    {
        get => Manager.CurrentLayout;
        set => Manager.CurrentLayout = value;
    }

    /// <summary>
    /// Gets the startup layout.
    /// </summary>
    public CameraLayout? SelectedStartupLayout
        => Manager.SelectedStartupLayout;

    /// <summary>
    /// Gets the number of cameras in the current layout.
    /// </summary>
    public int CameraCount
        => Manager.CameraCount;

    /// <summary>
    /// Gets the number of connected cameras.
    /// </summary>
    public int ConnectedCount
        => Manager.ConnectedCount;

    /// <summary>
    /// Gets the current status text.
    /// </summary>
    public string StatusText
        => Manager.StatusText;

    /// <summary>
    /// Initializes the view model with the camera grid control.
    /// </summary>
    /// <param name="cameraGridControl">The camera grid control.</param>
    public void Initialize(CameraGrid cameraGridControl)
        => Manager.Initialize(cameraGridControl);

    /// <summary>
    /// Shows a camera in full screen.
    /// </summary>
    /// <param name="camera">The camera to show.</param>
    public void ShowFullScreen(CameraConfiguration camera)
        => Manager.ShowFullScreen(camera);

    /// <summary>
    /// Handles connection state changes.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    public void OnConnectionStateChanged(CameraConnectionChangedEventArgs e)
        => Manager.OnConnectionStateChanged(e);

    /// <summary>
    /// Handles position changes.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    public void OnPositionChanged(CameraPositionChangedEventArgs e)
        => Manager.OnPositionChanged(e);

    /// <summary>
    /// Opens the camera configuration dialog for editing.
    /// </summary>
    /// <param name="camera">The camera to edit.</param>
    public void EditCamera(CameraConfiguration camera)
        => Manager.EditCamera(camera);

    /// <summary>
    /// Deletes a camera after confirmation.
    /// </summary>
    /// <param name="camera">The camera to delete.</param>
    public void DeleteCamera(CameraConfiguration camera)
        => Manager.DeleteCamera(camera);

    private void OnManagerPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        // Forward property change notifications only for properties exposed on this ViewModel
        if (e.PropertyName
            is nameof(CurrentLayout)
            or nameof(SelectedStartupLayout)
            or nameof(CameraCount)
            or nameof(ConnectedCount)
            or nameof(StatusText))
        {
            OnPropertyChanged(e.PropertyName);
        }

        // Handle CanExecute invalidation for commands
        if (e.PropertyName
            is nameof(ICameraWallManager.CameraCount)
            or nameof(ICameraWallManager.CameraGrid)
            or nameof(ICameraWallManager.CurrentLayout)
            or nameof(ICameraWallManager.CanCreateNewLayout)
            or nameof(ICameraWallManager.CanRenameCurrentLayout)
            or nameof(ICameraWallManager.CanAssignCameraToLayout)
            or nameof(ICameraWallManager.CanDeleteCurrentLayout)
            or nameof(ICameraWallManager.CanSetCurrentAsStartup)
            or nameof(ICameraWallManager.CanReconnectAll))
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    [RelayCommand]
    private void AddCamera()
        => Manager.AddCamera();

    [RelayCommand(CanExecute = nameof(CanNewLayout))]
    private void NewLayout()
        => Manager.CreateNewLayout();

    private bool CanNewLayout()
        => Manager.CanCreateNewLayout;

    [RelayCommand(CanExecute = nameof(CanRenameLayout))]
    private void RenameLayout()
        => Manager.RenameCurrentLayout();

    private bool CanRenameLayout()
        => Manager.CanRenameCurrentLayout;

    [RelayCommand(CanExecute = nameof(CanAssignCamera))]
    private void AssignCamera()
        => Manager.AssignCameraToLayout();

    private bool CanAssignCamera()
        => Manager.CanAssignCameraToLayout;

    [RelayCommand(CanExecute = nameof(CanDeleteLayout))]
    private void DeleteLayout()
        => Manager.DeleteCurrentLayout();

    private bool CanDeleteLayout()
        => Manager.CanDeleteCurrentLayout;

    [RelayCommand(CanExecute = nameof(CanSetAsStartup))]
    private void SetAsStartup()
        => Manager.SetCurrentAsStartup();

    private bool CanSetAsStartup()
        => Manager.CanSetCurrentAsStartup;

    [RelayCommand(CanExecute = nameof(CanReconnectAll))]
    private void ReconnectAll()
        => Manager.ReconnectAll();

    private bool CanReconnectAll()
        => Manager.CanReconnectAll;

    [RelayCommand]
    private void ShowAbout()
        => Manager.ShowAboutDialog();

    [RelayCommand]
    private void CheckForUpdates()
        => Manager.ShowCheckForUpdatesDialog();

    [RelayCommand]
    private void ShowSettings()
        => Manager.ShowSettingsDialog();

    [RelayCommand]
    private void ShowRecordingsBrowser()
        => Manager.ShowRecordingsBrowserDialog();

    [RelayCommand]
    private static void Exit()
        => Application.Current.Shutdown();
}