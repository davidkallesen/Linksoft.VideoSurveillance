// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Default implementation of <see cref="IDialogService"/>.
/// </summary>
[Registration(Lifetime.Singleton)]
public class DialogService : IDialogService
{
    private readonly IApplicationSettingsService settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    /// <param name="settingsService">The application settings service.</param>
    public DialogService(IApplicationSettingsService settingsService)
        => this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

    /// <inheritdoc />
    public CameraConfiguration? ShowCameraConfigurationDialog(
        CameraConfiguration? camera,
        bool isNew,
        IReadOnlyCollection<string> existingIpAddresses)
    {
        var cameraConfig = camera ?? new CameraConfiguration { Display = { DisplayName = Translations.NewCamera } };

        // Apply default settings to new cameras
        if (isNew)
        {
            settingsService.ApplyDefaultsToCamera(cameraConfig);
        }

        var viewModel = new CameraConfigurationDialogViewModel(cameraConfig, isNew, existingIpAddresses);
        var dialog = new CameraConfigurationDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        return dialog.ShowDialog() == true
            ? cameraConfig
            : null;
    }

    /// <inheritdoc />
    public string? ShowInputBox(
        string title,
        string prompt,
        string defaultText = "")
        => ShowInputBox(title, prompt, defaultText, [], string.Empty);

    /// <inheritdoc />
    public string? ShowInputBox(
        string title,
        string prompt,
        string defaultText,
        IReadOnlyCollection<string> forbiddenValues,
        string forbiddenValueError)
    {
        ArgumentNullException.ThrowIfNull(forbiddenValues);

        var labelTextBox = new LabelTextBox
        {
            LabelText = prompt,
            Text = defaultText,
            IsMandatory = true,
            MinLength = 1,
        };

        var dialogBox = new InputDialogBox(
            Application.Current.MainWindow!,
            title,
            labelTextBox);

        // Add validation for forbidden values
        if (forbiddenValues.Count > 0)
        {
            var forbiddenSet = new HashSet<string>(forbiddenValues, StringComparer.OrdinalIgnoreCase);

            labelTextBox.TextChanged += (_, _) =>
            {
                var currentText = labelTextBox.Text.Trim();
                var isForbidden = forbiddenSet.Contains(currentText);
                labelTextBox.ValidationText = isForbidden
                    ? forbiddenValueError
                    : string.Empty;
            };

            // Initial validation check
            var initialText = labelTextBox.Text?.Trim() ?? string.Empty;
            if (forbiddenSet.Contains(initialText))
            {
                labelTextBox.ValidationText = forbiddenValueError;
            }
        }

        var dialogResult = dialogBox.ShowDialog();
        if (dialogResult.HasValue && dialogResult.Value)
        {
            return ((LabelTextBox)dialogBox.Data).Text;
        }

        return null;
    }

    /// <inheritdoc />
    public bool ShowConfirmation(
        string message,
        string title)
    {
        var dialogBox = new QuestionDialogBox(
            Application.Current.MainWindow!,
            title,
            message)
        {
            Width = 400,
        };

        var dialogResult = dialogBox.ShowDialog();
        return dialogResult.HasValue && dialogResult.Value;
    }

    /// <inheritdoc />
    public void ShowError(
        string message,
        string? title = null)
    {
        MessageBox.Show(
            Application.Current.MainWindow!,
            message,
            title ?? Translations.Error,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    /// <inheritdoc />
    public void ShowInfo(
        string message,
        string? title = null)
    {
        MessageBox.Show(
            Application.Current.MainWindow!,
            message,
            title ?? Translations.Information,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <inheritdoc />
    public void ShowAboutDialog()
    {
        var version = Atc.Helpers.AssemblyHelper.GetSystemVersion();
        var year = DateTime.Now.Year;

        var dialog = new AboutDialog(version.ToString(), year)
        {
            Owner = Application.Current.MainWindow,
        };

        dialog.ShowDialog();
    }

    /// <inheritdoc />
    public void ShowCheckForUpdatesDialog()
    {
        using var gitHubReleaseService = new GitHubReleaseService();
        var viewModel = new CheckForUpdatesDialogViewModel(gitHubReleaseService);
        var dialog = new CheckForUpdatesDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        dialog.ShowDialog();
    }

    /// <inheritdoc />
    public void ShowFullScreenCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        using var viewModel = new FullScreenCameraWindowViewModel(camera);
        using var window = new FullScreenCameraWindow(viewModel);
        window.Owner = Application.Current.MainWindow;

        window.ShowDialog();
    }

    /// <inheritdoc />
    public bool ShowSettingsDialog()
    {
        // Set CultureManager.UiCulture to saved language before dialog loads
        // This ensures LabelLanguageSelector shows the correct language on initialization
        // (LanguageSelector falls back to Thread.CurrentThread.CurrentUICulture.LCID when SelectedKey is empty)
        if (NumberHelper.TryParseToInt(settingsService.General.Language, out var lcid))
        {
            CultureManager.UiCulture = new CultureInfo(lcid);
        }

        var viewModel = new SettingsDialogViewModel(settingsService);
        var dialog = new SettingsDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        return dialog.ShowDialog() == true;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<CameraConfiguration>? ShowAssignCameraDialog(
        string layoutName,
        IReadOnlyCollection<CameraConfiguration> availableCameras,
        IReadOnlyCollection<CameraConfiguration> assignedCameras)
    {
        ArgumentNullException.ThrowIfNull(layoutName);
        ArgumentNullException.ThrowIfNull(availableCameras);
        ArgumentNullException.ThrowIfNull(assignedCameras);

        var viewModel = new AssignCameraDialogViewModel(layoutName, availableCameras, assignedCameras);
        var dialog = new AssignCameraDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        // Only return result if dialog was confirmed AND there are actual changes
        if (dialog.ShowDialog() == true && viewModel.HasActualChanges())
        {
            return viewModel.AssignedCameras.ToList();
        }

        return null;
    }
}