namespace Linksoft.VideoSurveillance.Wpf.Dialogs;

using System.Reflection;

/// <summary>
/// View model for the Check for Updates dialog.
/// </summary>
public sealed partial class CheckForUpdatesDialogViewModel : ViewModelBase
{
    private readonly IGitHubReleaseService gitHubReleaseService;

    [ObservableProperty]
    private string currentVersion = string.Empty;

    [ObservableProperty]
    private string latestVersion = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty(AfterChangedCallback = nameof(OnIsCheckingChanged))]
    private bool isChecking;

    [ObservableProperty]
    private bool hasNewVersion;

    [ObservableProperty]
    private Uri? downloadUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckForUpdatesDialogViewModel"/> class.
    /// </summary>
    /// <param name="gitHubReleaseService">The GitHub release service.</param>
    public CheckForUpdatesDialogViewModel(
        IGitHubReleaseService gitHubReleaseService)
    {
        ArgumentNullException.ThrowIfNull(gitHubReleaseService);

        this.gitHubReleaseService = gitHubReleaseService;

        // Get current version from assembly
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        CurrentVersion = version?.ToString(3) ?? "1.0.0";

        StatusMessage = "Checking for updates...";
    }

    /// <summary>
    /// Occurs when the dialog requests to be closed.
    /// </summary>
    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    private static void OnIsCheckingChanged()
        => CommandManager.InvalidateRequerySuggested();

    /// <summary>
    /// Checks for updates asynchronously.
    /// </summary>
    public async Task CheckForUpdatesAsync()
    {
        IsChecking = true;
        HasNewVersion = false;
        StatusMessage = "Checking for updates...";

        try
        {
            var latest = await gitHubReleaseService
                .GetLatestVersionAsync()
                .ConfigureAwait(true);

            if (latest is not null)
            {
                LatestVersion = latest.ToString(3);

                if (Version.TryParse(CurrentVersion, out var current) && latest > current)
                {
                    HasNewVersion = true;
                    StatusMessage = "A new version is available!";
                    DownloadUrl = await gitHubReleaseService
                        .GetLatestMsiDownloadUrlAsync()
                        .ConfigureAwait(true);
                }
                else
                {
                    StatusMessage = "You are up to date.";
                }
            }
            else
            {
                StatusMessage = "Unable to check for updates. Please try again later.";
            }
        }
        catch
        {
            StatusMessage = "Unable to check for updates. Please try again later.";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownloadLatest))]
    private void DownloadLatest()
    {
        if (DownloadUrl is not null)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = DownloadUrl.ToString(),
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Silently fail if unable to open browser
            }
        }
    }

    private bool CanDownloadLatest()
        => HasNewVersion && DownloadUrl is not null && !IsChecking;

    [RelayCommand]
    private void Close()
        => CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));
}
