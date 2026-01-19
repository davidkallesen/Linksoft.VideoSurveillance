namespace Linksoft.Wpf.CameraWall.Dialogs;

/// <summary>
/// Dialog for assigning/unassigning cameras to the current layout.
/// </summary>
public partial class AssignCameraDialog
{
    private readonly AssignCameraDialogViewModel viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssignCameraDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public AssignCameraDialog(AssignCameraDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        this.viewModel = viewModel;

        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, e) =>
        {
            DialogResult = e.DialogResult;
            Close();
        };
    }

    private void OnAvailableSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        viewModel.SelectedAvailableCameras = AvailableListBox.SelectedItems
            .Cast<CameraConfiguration>()
            .ToList();
    }

    private void OnAssignedSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        viewModel.SelectedAssignedCameras = AssignedListBox.SelectedItems
            .Cast<CameraConfiguration>()
            .ToList();
    }
}