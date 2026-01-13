// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Default implementation of <see cref="ICameraWallManager"/>.
/// </summary>
[Registration(Lifetime.Singleton)]
public partial class CameraWallManager : ObservableObject, ICameraWallManager
{
    private readonly ICameraStorageService storageService;
    private readonly IDialogService dialogService;
    private readonly IApplicationSettingsService settingsService;

    [ObservableProperty(DependentPropertyNames = [nameof(CanCreateNewLayout)])]
    private UserControls.CameraWall? cameraWall;

    [ObservableProperty]
    private string statusText = Translations.Ready;

    [ObservableProperty(DependentPropertyNames = [nameof(CanReconnectAll)])]
    private int cameraCount;

    [ObservableProperty]
    private int connectedCount;

    [ObservableProperty]
    private CameraLayout? selectedStartupLayout;

    [ObservableProperty(
        AfterChangedCallback = nameof(OnCurrentLayoutChangedCallback),
        DependentPropertyNames = [nameof(CanDeleteCurrentLayout), nameof(CanSetCurrentAsStartup)])]
    private CameraLayout? currentLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraWallManager"/> class.
    /// </summary>
    /// <param name="storageService">The camera storage service.</param>
    /// <param name="dialogService">The dialog service.</param>
    /// <param name="settingsService">The application settings service.</param>
    public CameraWallManager(
        ICameraStorageService storageService,
        IDialogService dialogService,
        IApplicationSettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(settingsService);

        this.storageService = storageService;
        this.dialogService = dialogService;
        this.settingsService = settingsService;

        Layouts = new ObservableCollection<CameraLayout>(storageService.GetAllLayouts());

        EnsureDefaultLayoutExists();
        LoadStartupLayout();
    }

    /// <inheritdoc />
    public event EventHandler<string>? StatusChanged;

    /// <inheritdoc />
    public ObservableCollection<CameraLayout> Layouts { get; }

    /// <inheritdoc />
    public bool CanCreateNewLayout => CameraWall is not null;

    /// <inheritdoc />
    public bool CanDeleteCurrentLayout
        => CurrentLayout is not null &&
           Layouts.Count > 1 &&
           CurrentLayout.Id != storageService.StartupLayoutId;

    /// <inheritdoc />
    public bool CanSetCurrentAsStartup
        => CurrentLayout is not null &&
           CurrentLayout.Id != storageService.StartupLayoutId;

    /// <inheritdoc />
    public bool CanReconnectAll
        => CameraCount > 0;

    /// <inheritdoc />
    public void Initialize(UserControls.CameraWall cameraWallControl)
    {
        CameraWall = cameraWallControl;
        ApplyDisplaySettings();
        LoadStartupCameras();
    }

    /// <inheritdoc />
    public void ApplyDisplaySettings()
    {
        if (CameraWall is null)
        {
            return;
        }

        var display = settingsService.Display;
        CameraWall.ShowOverlayTitle = display.ShowOverlayTitle;
        CameraWall.ShowOverlayDescription = display.ShowOverlayDescription;
        CameraWall.ShowOverlayConnectionStatus = display.ShowOverlayConnectionStatus;
        CameraWall.ShowOverlayTime = display.ShowOverlayTime;
        CameraWall.OverlayOpacity = display.OverlayOpacity;
        CameraWall.AllowDragAndDropReorder = display.AllowDragAndDropReorder;
        CameraWall.AutoSave = display.AutoSaveLayoutChanges;
        CameraWall.SnapshotDirectory = display.SnapshotDirectory;
    }

    /// <inheritdoc />
    public void AddCamera()
    {
        UpdateStatus(Translations.AddCameraDialog);

        var existingIpAddresses = GetExistingIpAddresses(excludeCameraId: null);
        var camera = dialogService.ShowCameraConfigurationDialog(camera: null, isNew: true, existingIpAddresses);

        if (camera is not null)
        {
            storageService.AddOrUpdateCamera(camera);
            Messenger.Default.Send(new CameraAddMessage(camera));
            SaveCurrentLayout();
            CameraCount = storageService.GetAllCameras().Count;
            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.AddedCamera1, camera.DisplayName));
        }
        else
        {
            UpdateStatus(Translations.AddCameraCancelled);
        }
    }

    /// <inheritdoc />
    public void EditCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.EditingCamera1, camera.DisplayName));

        // Exclude current camera's IP when editing (allow keeping same IP)
        var existingIpAddresses = GetExistingIpAddresses(excludeCameraId: camera.Id);
        var result = dialogService.ShowCameraConfigurationDialog(camera, isNew: false, existingIpAddresses);

        if (result is not null)
        {
            storageService.AddOrUpdateCamera(camera);
            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.UpdatedCamera1, camera.DisplayName));
        }
        else
        {
            UpdateStatus(Translations.EditCameraCancelled);
        }
    }

    private List<string> GetExistingIpAddresses(Guid? excludeCameraId)
        => storageService
            .GetAllCameras()
            .Where(c => excludeCameraId is null || c.Id != excludeCameraId)
            .Select(c => c.IpAddress)
            .Where(ip => !string.IsNullOrWhiteSpace(ip))
            .ToList();

    /// <inheritdoc />
    public void DeleteCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var confirmed = dialogService.ShowConfirmation(
            string.Format(CultureInfo.CurrentCulture, Translations.ConfirmDeleteCamera1, camera.DisplayName),
            Translations.DeleteCamera);

        if (confirmed)
        {
            storageService.DeleteCamera(camera.Id);
            Messenger.Default.Send(new CameraRemoveMessage(camera.Id));
            SaveCurrentLayout();
            CameraCount = storageService.GetAllCameras().Count;
            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.DeletedCamera1, camera.DisplayName));
        }
    }

    /// <inheritdoc />
    public void ShowFullScreen(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.FullScreenCamera1, camera.DisplayName));
        dialogService.ShowFullScreenCamera(camera);
    }

    /// <inheritdoc />
    public void ReconnectAll()
    {
        UpdateStatus(Translations.ReconnectingAllCameras);
        CameraWall?.ReconnectAll();
        UpdateStatus(Translations.AllCamerasReconnected);
    }

    /// <inheritdoc />
    public void CreateNewLayout()
    {
        if (CameraWall is null)
        {
            return;
        }

        var layoutName = dialogService.ShowInputBox(
            Translations.NewLayout,
            Translations.EnterLayoutName,
            $"Layout {DateTime.Now:yyyy-MM-dd HH:mm}");

        if (string.IsNullOrWhiteSpace(layoutName))
        {
            UpdateStatus(Translations.NewLayoutCancelled);
            return;
        }

        // Check for duplicate layout name (case-insensitive)
        if (Layouts.Any(l => l.Name.Equals(layoutName, StringComparison.OrdinalIgnoreCase)))
        {
            dialogService.ShowError(Translations.LayoutNameAlreadyExists);
            return;
        }

        var layout = new CameraLayout
        {
            Name = layoutName,
            Items = [],
        };

        storageService.AddOrUpdateLayout(layout);
        Layouts.Add(layout);
        CurrentLayout = layout;

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.LayoutCreated1, layout.Name));
    }

    /// <inheritdoc />
    public void DeleteCurrentLayout()
    {
        if (CurrentLayout is null)
        {
            return;
        }

        var confirmed = dialogService.ShowConfirmation(
            string.Format(CultureInfo.CurrentCulture, Translations.ConfirmDeleteLayout1, CurrentLayout.Name),
            Translations.DeleteLayout);

        if (confirmed)
        {
            var layoutToDelete = CurrentLayout;
            storageService.DeleteLayout(layoutToDelete.Id);
            Layouts.Remove(layoutToDelete);
            CurrentLayout = Layouts.FirstOrDefault();

            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.DeletedLayout1, layoutToDelete.Name));
        }
    }

    /// <inheritdoc />
    public void SetCurrentAsStartup()
    {
        if (CurrentLayout is null)
        {
            return;
        }

        storageService.StartupLayoutId = CurrentLayout.Id;
        SelectedStartupLayout = CurrentLayout;

        OnPropertyChanged(nameof(CanSetCurrentAsStartup));
        OnPropertyChanged(nameof(CanDeleteCurrentLayout));
        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.SetStartupLayout1, CurrentLayout.Name));
    }

    /// <inheritdoc />
    public void SaveCurrentLayout()
    {
        if (CameraWall is null || CurrentLayout is null)
        {
            return;
        }

        CurrentLayout.Items = CameraWall.GetCurrentLayout();
        CurrentLayout.ModifiedAt = DateTime.UtcNow;
        storageService.AddOrUpdateLayout(CurrentLayout);
    }

    /// <inheritdoc />
    public void OnConnectionStateChanged(CameraConnectionChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.CameraStateChanged2, e.Camera.DisplayName, e.NewState));
        UpdateConnectionCount();
    }

    /// <inheritdoc />
    public void OnPositionChanged(CameraPositionChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Auto-save layout if enabled
        if (settingsService.Display.AutoSaveLayoutChanges)
        {
            SaveCurrentLayout();
        }

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.PositionChanged1, e.Camera.DisplayName));
    }

    /// <inheritdoc />
    public void ShowAboutDialog()
        => dialogService.ShowAboutDialog();

    /// <inheritdoc />
    public void ShowCheckForUpdatesDialog()
        => dialogService.ShowCheckForUpdatesDialog();

    /// <inheritdoc />
    public void ShowSettingsDialog()
    {
        if (dialogService.ShowSettingsDialog())
        {
            ApplyDisplaySettings();
        }
    }

    private void OnCurrentLayoutChangedCallback()
    {
        if (CameraWall is null || CurrentLayout is null)
        {
            return;
        }

        var cameras = storageService.GetAllCameras();
        CameraWall.ApplyLayout(CurrentLayout, cameras);
        CameraCount = CameraWall.CameraTiles?.Count ?? 0;
        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.LoadedLayout1, CurrentLayout.Name));
    }

    private void EnsureDefaultLayoutExists()
    {
        if (Layouts.Count > 0)
        {
            return;
        }

        var defaultLayout = new CameraLayout
        {
            Name = "Default",
        };

        storageService.AddOrUpdateLayout(defaultLayout);
        storageService.StartupLayoutId = defaultLayout.Id;
        Layouts.Add(defaultLayout);
    }

    private void LoadStartupLayout()
    {
        if (storageService.StartupLayoutId.HasValue)
        {
            SelectedStartupLayout = Layouts.FirstOrDefault(l => l.Id == storageService.StartupLayoutId.Value);
            CurrentLayout = SelectedStartupLayout;
        }
        else if (Layouts.Count > 0)
        {
            // If no startup layout is set, use the first layout
            CurrentLayout = Layouts[0];
        }
    }

    private void LoadStartupCameras()
    {
        if (CameraWall is null)
        {
            return;
        }

        var cameras = storageService.GetAllCameras();

        if (SelectedStartupLayout is not null)
        {
            CameraWall.ApplyLayout(SelectedStartupLayout, cameras);
        }
        else
        {
            // Load all cameras if no startup layout
            foreach (var camera in cameras)
            {
                Messenger.Default.Send(new CameraAddMessage(camera));
            }
        }

        CameraCount = cameras.Count;
        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.LoadedCameras1, cameras.Count));
    }

    private void UpdateConnectionCount()
    {
        // This would be tracked properly with events from each tile
        // For now, just update based on loaded cameras
    }

    private void UpdateStatus(string status)
    {
        StatusText = status;
        StatusChanged?.Invoke(this, status);
    }
}