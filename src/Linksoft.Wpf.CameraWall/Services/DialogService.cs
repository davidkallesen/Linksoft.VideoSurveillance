// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Default implementation of <see cref="IDialogService"/>.
/// </summary>
[Registration(Lifetime.Singleton)]
public class DialogService : IDialogService
{
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

        var content = $"""
            Linksoft Camera Wall

            {Translations.Version}: {version}

            {Translations.ApplicationDescription}

            {Translations.Copyright} {year} Linksoft
            {Translations.AllRightsReserved}
            """;

        var dialogBox = new InfoDialogBox(
            Application.Current.MainWindow!,
            new DialogBoxSettings(DialogBoxType.Ok, LogCategoryType.Information)
            {
                TitleBarText = Translations.AboutLinksoftCameraWall,
                Width = 750,
                Height = 320,
            },
            content);

        dialogBox.ShowDialog();
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
        var window = new FullScreenCameraWindow(viewModel);
        window.ShowDialog();
    }
}