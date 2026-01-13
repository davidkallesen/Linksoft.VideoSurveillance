// ReSharper disable RedundantArgumentDefaultValue

using AssemblyHelper = Atc.Helpers.AssemblyHelper;

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
        var cameraConfig = camera ?? new CameraConfiguration { DisplayName = Translations.NewCamera };
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
    {
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
        var version = AssemblyHelper.GetSystemVersion();
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
        var window = new FullScreenCameraWindow(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

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
}