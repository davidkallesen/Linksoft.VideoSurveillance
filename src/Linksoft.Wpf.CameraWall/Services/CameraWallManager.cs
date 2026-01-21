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
    private Linksoft.Wpf.CameraWall.UserControls.CameraGrid? cameraGrid;

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
    public bool CanCreateNewLayout => CameraGrid is not null;

    /// <inheritdoc />
    public bool CanRenameCurrentLayout => CurrentLayout is not null;

    /// <inheritdoc />
    public bool CanAssignCameraToLayout
        => CameraGrid is not null && CurrentLayout is not null;

    /// <inheritdoc />
    public bool CanDeleteCurrentLayout
        => CurrentLayout is not null &&
           Layouts.Count > 1 &&
           CurrentLayout.Id != storageService.StartupLayoutId &&
           CurrentLayout.Items.Count == 0;

    /// <inheritdoc />
    public bool CanSetCurrentAsStartup
        => CurrentLayout is not null &&
           CurrentLayout.Id != storageService.StartupLayoutId;

    /// <inheritdoc />
    public bool CanReconnectAll
        => CameraCount > 0;

    /// <inheritdoc />
    public void Initialize(CameraGrid cameraGridControl)
    {
        CameraGrid = cameraGridControl;
        ApplyDisplaySettings();
        LoadStartupCameras();
    }

    /// <inheritdoc />
    public void ApplyDisplaySettings()
    {
        if (CameraGrid is null)
        {
            return;
        }

        var general = settingsService.General;
        var display = settingsService.CameraDisplay;
        var connection = settingsService.Connection;

        // General settings
        CameraGrid.AutoConnectOnLoad = general.ConnectCamerasOnStartup;

        // Display settings
        CameraGrid.ShowOverlayTitle = display.ShowOverlayTitle;
        CameraGrid.ShowOverlayDescription = display.ShowOverlayDescription;
        CameraGrid.ShowOverlayConnectionStatus = display.ShowOverlayConnectionStatus;
        CameraGrid.ShowOverlayTime = display.ShowOverlayTime;
        CameraGrid.OverlayOpacity = display.OverlayOpacity;
        CameraGrid.AllowDragAndDropReorder = display.AllowDragAndDropReorder;
        CameraGrid.AutoSave = display.AutoSaveLayoutChanges;
        CameraGrid.SnapshotDirectory = display.SnapshotDirectory;

        // Connection settings
        CameraGrid.ConnectionTimeoutSeconds = connection.ConnectionTimeoutSeconds;
        CameraGrid.ReconnectDelaySeconds = connection.ReconnectDelaySeconds;
        CameraGrid.MaxReconnectAttempts = connection.MaxReconnectAttempts;
        CameraGrid.AutoReconnectOnFailure = connection.AutoReconnectOnFailure;

        // Notification settings
        CameraGrid.ShowNotificationOnDisconnect = connection.ShowNotificationOnDisconnect;
        CameraGrid.ShowNotificationOnReconnect = connection.ShowNotificationOnReconnect;
        CameraGrid.PlayNotificationSound = connection.PlayNotificationSound;
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
            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.AddedCamera1, camera.Display.DisplayName));
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

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.EditingCamera1, camera.Display.DisplayName));

        // Exclude current camera's IP when editing (allow keeping same IP)
        var existingIpAddresses = GetExistingIpAddresses(excludeCameraId: camera.Id);
        var result = dialogService.ShowCameraConfigurationDialog(camera, isNew: false, existingIpAddresses);

        if (result is not null)
        {
            storageService.AddOrUpdateCamera(camera);
            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.UpdatedCamera1, camera.Display.DisplayName));
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
            .Select(c => c.Connection.IpAddress)
            .Where(ip => !string.IsNullOrWhiteSpace(ip))
            .ToList();

    /// <inheritdoc />
    public void DeleteCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var confirmed = dialogService.ShowConfirmation(
            string.Format(CultureInfo.CurrentCulture, Translations.ConfirmDeleteCamera1, camera.Display.DisplayName),
            Translations.DeleteCamera);

        if (confirmed)
        {
            storageService.DeleteCamera(camera.Id);
            Messenger.Default.Send(new CameraRemoveMessage(camera.Id));
            SaveCurrentLayout();
            CameraCount = storageService.GetAllCameras().Count;
            UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.DeletedCamera1, camera.Display.DisplayName));
        }
    }

    /// <inheritdoc />
    public void ShowFullScreen(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.FullScreenCamera1, camera.Display.DisplayName));
        dialogService.ShowFullScreenCamera(camera);
    }

    /// <inheritdoc />
    public void ReconnectAll()
    {
        UpdateStatus(Translations.ReconnectingAllCameras);
        CameraGrid?.ReconnectAll();
        UpdateStatus(Translations.AllCamerasReconnected);
    }

    /// <inheritdoc />
    public void CreateNewLayout()
    {
        if (CameraGrid is null)
        {
            return;
        }

        var existingNames = Layouts.Select(l => l.Name).ToList();
        var layoutName = dialogService.ShowInputBox(
            Translations.NewLayout,
            Translations.EnterLayoutName,
            $"Layout {DateTime.Now:yyyy-MM-dd HH:mm}",
            existingNames,
            Translations.LayoutNameAlreadyExists);

        if (string.IsNullOrWhiteSpace(layoutName))
        {
            UpdateStatus(Translations.NewLayoutCancelled);
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
    public void RenameCurrentLayout()
    {
        if (CurrentLayout is null)
        {
            return;
        }

        var oldName = CurrentLayout.Name;

        // Exclude current layout name from forbidden values (allow keeping the same name)
        var existingNames = Layouts
            .Where(l => l.Id != CurrentLayout.Id)
            .Select(l => l.Name)
            .ToList();

        var newName = dialogService.ShowInputBox(
            Translations.RenameLayout,
            Translations.EnterNewLayoutName,
            oldName,
            existingNames,
            Translations.LayoutNameAlreadyExists);

        if (string.IsNullOrWhiteSpace(newName) || newName == oldName)
        {
            UpdateStatus(Translations.RenameLayoutCancelled);
            return;
        }

        CurrentLayout.Name = newName;
        CurrentLayout.ModifiedAt = DateTime.UtcNow;
        storageService.AddOrUpdateLayout(CurrentLayout);

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.LayoutRenamed2, oldName, newName));
    }

    /// <inheritdoc />
    public void AssignCameraToLayout()
    {
        if (CameraGrid is null || CurrentLayout is null)
        {
            return;
        }

        var allCameras = storageService.GetAllCameras();
        var cameraDict = allCameras.ToDictionary(c => c.Id);

        // Get assigned cameras in their current layout order
        var assignedCameras = CurrentLayout.Items
            .Where(i => cameraDict.ContainsKey(i.CameraId))
            .OrderBy(i => i.OrderNumber)
            .Select(i => cameraDict[i.CameraId])
            .ToList();

        var assignedCameraIds = assignedCameras
            .Select(c => c.Id)
            .ToHashSet();

        var availableCameras = allCameras
            .Where(c => !assignedCameraIds.Contains(c.Id))
            .ToList();

        UpdateStatus(Translations.AssignCameraToLayout);

        var result = dialogService.ShowAssignCameraDialog(
            CurrentLayout.Name,
            availableCameras,
            assignedCameras);

        if (result is null)
        {
            UpdateStatus(Translations.AssignCameraCancelled);
            return;
        }

        ApplyLayoutCameraChanges(result, assignedCameraIds);
    }

    private void ApplyLayoutCameraChanges(
        IReadOnlyCollection<CameraConfiguration> newAssignedCameras,
        HashSet<Guid> previousAssignedIds)
    {
        if (CameraGrid is null || CurrentLayout is null)
        {
            return;
        }

        // Build a dictionary of existing layout items by camera ID to preserve their IDs
        var existingItemsDict = CurrentLayout.Items.ToDictionary(i => i.CameraId);

        // Build new layout items preserving existing IDs where possible
        var newLayoutItems = new List<CameraLayoutItem>();
        var orderNumber = 0;

        foreach (var camera in newAssignedCameras)
        {
            CameraLayoutItem item;
            if (existingItemsDict.TryGetValue(camera.Id, out var existingItem))
            {
                // Preserve existing item ID, update order
                item = existingItem;
                item.OrderNumber = orderNumber;
            }
            else
            {
                // New camera - create new layout item
                item = new CameraLayoutItem
                {
                    CameraId = camera.Id,
                    OrderNumber = orderNumber,
                };
            }

            newLayoutItems.Add(item);
            orderNumber++;
        }

        // Calculate what changed for status message
        var newAssignedIds = newAssignedCameras
            .Select(c => c.Id)
            .ToHashSet();

        var (addedCount, removedCount) = CalculateLayoutChangeCounts(newAssignedIds, previousAssignedIds);

        // Update the layout items directly
        CurrentLayout.Items = newLayoutItems;
        CurrentLayout.ModifiedAt = DateTime.UtcNow;
        storageService.AddOrUpdateLayout(CurrentLayout);

        // Reload the CameraGrid with the new order
        var allCameras = storageService.GetAllCameras();
        CameraGrid.ApplyLayout(CurrentLayout, allCameras);

        CameraCount = CameraGrid.CameraTiles?.Count ?? 0;
        OnPropertyChanged(nameof(CanDeleteCurrentLayout));
        UpdateLayoutChangeStatus(addedCount, removedCount);
    }

    private static (int Added, int Removed) CalculateLayoutChangeCounts(
        HashSet<Guid> newAssignedIds,
        HashSet<Guid> previousAssignedIds)
    {
        var added = newAssignedIds
            .Except(previousAssignedIds)
            .Count();

        var removed = previousAssignedIds
            .Except(newAssignedIds)
            .Count();

        return (added, removed);
    }

    private void UpdateLayoutChangeStatus(
        int addedCount,
        int removedCount)
    {
        if (addedCount > 0 || removedCount > 0)
        {
            UpdateStatus(string.Format(
                CultureInfo.CurrentCulture,
                Translations.LayoutCamerasUpdated2,
                addedCount,
                removedCount));
        }
        else
        {
            UpdateStatus(Translations.LayoutOrderUpdated);
        }
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
        if (CameraGrid is null || CurrentLayout is null)
        {
            return;
        }

        CurrentLayout.Items = CameraGrid.GetCurrentLayout();
        CurrentLayout.ModifiedAt = DateTime.UtcNow;
        storageService.AddOrUpdateLayout(CurrentLayout);
    }

    /// <inheritdoc />
    public void OnConnectionStateChanged(CameraConnectionChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.CameraStateChanged2, e.Camera.Display.DisplayName, e.NewState));
        UpdateConnectionCount();
    }

    /// <inheritdoc />
    public void OnPositionChanged(CameraPositionChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Auto-save layout if enabled
        if (settingsService.CameraDisplay.AutoSaveLayoutChanges)
        {
            SaveCurrentLayout();
        }

        UpdateStatus(string.Format(CultureInfo.CurrentCulture, Translations.PositionChanged1, e.Camera.Display.DisplayName));
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
        if (CameraGrid is null || CurrentLayout is null)
        {
            return;
        }

        var cameras = storageService.GetAllCameras();
        CameraGrid.ApplyLayout(CurrentLayout, cameras);
        CameraCount = CameraGrid.CameraTiles?.Count ?? 0;
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
        if (CameraGrid is null)
        {
            return;
        }

        var cameras = storageService.GetAllCameras();

        if (SelectedStartupLayout is not null)
        {
            CameraGrid.ApplyLayout(SelectedStartupLayout, cameras);
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