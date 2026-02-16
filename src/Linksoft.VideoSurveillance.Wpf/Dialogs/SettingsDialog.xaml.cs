namespace Linksoft.VideoSurveillance.Wpf.Dialogs;

/// <summary>
/// Dialog for application settings.
/// </summary>
public partial class SettingsDialog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseRequested += (_, e) =>
        {
            DialogResult = e.DialogResult;
            Close();
        };

        // Live theme preview when theme base changes
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SettingsDialogViewModel.SelectedThemeBase)
                or nameof(SettingsDialogViewModel.ThemeAccent))
            {
                viewModel.ApplyThemePreview();
            }
        };
    }
}
