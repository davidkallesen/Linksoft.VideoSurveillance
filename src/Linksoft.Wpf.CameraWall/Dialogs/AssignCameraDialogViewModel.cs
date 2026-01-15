namespace Linksoft.Wpf.CameraWall.Dialogs;

/// <summary>
/// View model for the Assign Camera to Layout dialog.
/// Supports dual-list multi-select for assigning/unassigning cameras.
/// </summary>
public partial class AssignCameraDialogViewModel : ViewModelDialogBase
{
    private readonly List<Guid> originalAssignedCameraIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssignCameraDialogViewModel"/> class.
    /// </summary>
    /// <param name="layoutName">The name of the layout being edited.</param>
    /// <param name="availableCameras">Cameras not currently in the layout.</param>
    /// <param name="assignedCameras">Cameras currently in the layout (in order).</param>
    public AssignCameraDialogViewModel(
        string layoutName,
        IEnumerable<CameraConfiguration> availableCameras,
        IEnumerable<CameraConfiguration> assignedCameras)
    {
        ArgumentNullException.ThrowIfNull(layoutName);
        ArgumentNullException.ThrowIfNull(availableCameras);
        ArgumentNullException.ThrowIfNull(assignedCameras);

        LayoutName = layoutName;

        AvailableCameras = new ObservableCollection<CameraConfiguration>(
            availableCameras.OrderBy(c => c.Display.DisplayName, StringComparer.OrdinalIgnoreCase));

        // Keep assigned cameras in their original order (for order tracking)
        var assignedList = assignedCameras.ToList();
        AssignedCameras = new ObservableCollection<CameraConfiguration>(assignedList);

        // Store original state for IsDirty comparison
        originalAssignedCameraIds = assignedList
            .Select(c => c.Id)
            .ToList();

        SelectedAvailableCameras = [];
        SelectedAssignedCameras = [];
    }

    /// <summary>
    /// Occurs when the dialog requests to be closed.
    /// </summary>
    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    /// <summary>
    /// Gets the dialog title.
    /// </summary>
    public static string DialogTitle => Translations.AssignCameraToLayout;

    /// <summary>
    /// Gets the name of the layout being edited.
    /// </summary>
    public string LayoutName { get; }

    /// <summary>
    /// Gets the cameras available for assignment (not in current layout).
    /// </summary>
    public ObservableCollection<CameraConfiguration> AvailableCameras { get; }

    /// <summary>
    /// Gets the cameras assigned to the current layout.
    /// </summary>
    public ObservableCollection<CameraConfiguration> AssignedCameras { get; }

    /// <summary>
    /// Gets or sets the selected cameras in the available list.
    /// </summary>
    [ObservableProperty(AfterChangedCallback = nameof(OnSelectionChanged))]
    private IList<CameraConfiguration> selectedAvailableCameras = [];

    /// <summary>
    /// Gets or sets the selected cameras in the assigned list.
    /// </summary>
    [ObservableProperty(AfterChangedCallback = nameof(OnSelectionChanged))]
    private IList<CameraConfiguration> selectedAssignedCameras = [];

    private static void OnSelectionChanged()
        => CommandManager.InvalidateRequerySuggested();

    /// <summary>
    /// Moves selected cameras from available to assigned.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveToAssigned))]
    private void MoveToAssigned()
    {
        var toMove = SelectedAvailableCameras.ToList();
        foreach (var camera in toMove)
        {
            AvailableCameras.Remove(camera);
            InsertSorted(AssignedCameras, camera);
        }

        SelectedAvailableCameras = [];
        IsDirty = true;
    }

    private bool CanMoveToAssigned()
        => SelectedAvailableCameras.Count > 0;

    /// <summary>
    /// Moves selected cameras from assigned to available.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveToAvailable))]
    private void MoveToAvailable()
    {
        var toMove = SelectedAssignedCameras.ToList();
        foreach (var camera in toMove)
        {
            AssignedCameras.Remove(camera);
            InsertSorted(AvailableCameras, camera);
        }

        SelectedAssignedCameras = [];
        IsDirty = true;
    }

    private bool CanMoveToAvailable()
        => SelectedAssignedCameras.Count > 0;

    /// <summary>
    /// Moves all cameras from available to assigned.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveAllToAssigned))]
    private void MoveAllToAssigned()
    {
        var toMove = AvailableCameras.ToList();
        foreach (var camera in toMove)
        {
            AvailableCameras.Remove(camera);
            InsertSorted(AssignedCameras, camera);
        }

        IsDirty = true;
    }

    private bool CanMoveAllToAssigned()
        => AvailableCameras.Count > 0;

    /// <summary>
    /// Moves all cameras from assigned to available.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveAllToAvailable))]
    private void MoveAllToAvailable()
    {
        var toMove = AssignedCameras.ToList();
        foreach (var camera in toMove)
        {
            AssignedCameras.Remove(camera);
            InsertSorted(AvailableCameras, camera);
        }

        IsDirty = true;
    }

    private bool CanMoveAllToAvailable()
        => AssignedCameras.Count > 0;

    /// <summary>
    /// Moves the selected assigned camera up in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp()
    {
        if (SelectedAssignedCameras.Count != 1)
        {
            return;
        }

        var camera = SelectedAssignedCameras[0];
        var index = AssignedCameras.IndexOf(camera);

        if (index > 0)
        {
            AssignedCameras.Move(index, index - 1);
            IsDirty = true;
        }
    }

    private bool CanMoveUp()
    {
        if (SelectedAssignedCameras.Count != 1)
        {
            return false;
        }

        var camera = SelectedAssignedCameras[0];
        var index = AssignedCameras.IndexOf(camera);
        return index > 0;
    }

    /// <summary>
    /// Moves the selected assigned camera down in the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown()
    {
        if (SelectedAssignedCameras.Count != 1)
        {
            return;
        }

        var camera = SelectedAssignedCameras[0];
        var index = AssignedCameras.IndexOf(camera);

        if (index < AssignedCameras.Count - 1)
        {
            AssignedCameras.Move(index, index + 1);
            IsDirty = true;
        }
    }

    private bool CanMoveDown()
    {
        if (SelectedAssignedCameras.Count != 1)
        {
            return false;
        }

        var camera = SelectedAssignedCameras[0];
        var index = AssignedCameras.IndexOf(camera);
        return index < AssignedCameras.Count - 1;
    }

    /// <summary>
    /// Gets a value indicating whether there are actual changes compared to the original state.
    /// </summary>
    /// <returns><c>true</c> if the assigned cameras list differs from the original; otherwise, <c>false</c>.</returns>
    public bool HasActualChanges()
    {
        var currentIds = AssignedCameras
            .Select(c => c.Id)
            .ToList();
        return !originalAssignedCameraIds.SequenceEqual(currentIds);
    }

    [RelayCommand]
    private void Ok()
        => CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));

    [RelayCommand]
    private void Cancel()
        => CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));

    private static void InsertSorted(
        ObservableCollection<CameraConfiguration> collection,
        CameraConfiguration camera)
    {
        var index = 0;
        while (index < collection.Count &&
               string.Compare(
                   collection[index].Display.DisplayName,
                   camera.Display.DisplayName,
                   StringComparison.OrdinalIgnoreCase) < 0)
        {
            index++;
        }

        collection.Insert(index, camera);
    }
}
