namespace Linksoft.VideoSurveillance.Wpf.App;

/// <summary>
/// View model for the main window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GatewayService gatewayService;
    private readonly SurveillanceHubService hubService;
    private readonly IGitHubReleaseService gitHubReleaseService;
    private readonly LiveViewViewModel liveViewViewModel;
    private readonly DashboardViewModel dashboardViewModel;
    private readonly CameraListViewModel cameraListViewModel;
    private readonly LayoutListViewModel layoutListViewModel;
    private readonly RecordingsViewModel recordingsViewModel;

    [ObservableProperty]
    private string statusText = "Ready";

    [ObservableProperty]
    private string serverUrl;

    [ObservableProperty]
    private string hubConnectionState = "Disconnected";

    [ObservableProperty]
    private ViewModelBase? currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel(
        GatewayService gatewayService,
        SurveillanceHubService hubService,
        IGitHubReleaseService gitHubReleaseService,
        LiveViewViewModel liveViewViewModel,
        DashboardViewModel dashboardViewModel,
        CameraListViewModel cameraListViewModel,
        LayoutListViewModel layoutListViewModel,
        RecordingsViewModel recordingsViewModel,
        string apiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(gatewayService);
        ArgumentNullException.ThrowIfNull(hubService);
        ArgumentNullException.ThrowIfNull(gitHubReleaseService);
        ArgumentNullException.ThrowIfNull(liveViewViewModel);
        ArgumentNullException.ThrowIfNull(dashboardViewModel);
        ArgumentNullException.ThrowIfNull(cameraListViewModel);
        ArgumentNullException.ThrowIfNull(layoutListViewModel);
        ArgumentNullException.ThrowIfNull(recordingsViewModel);

        this.gatewayService = gatewayService;
        this.hubService = hubService;
        this.gitHubReleaseService = gitHubReleaseService;
        this.liveViewViewModel = liveViewViewModel;
        this.dashboardViewModel = dashboardViewModel;
        this.cameraListViewModel = cameraListViewModel;
        this.layoutListViewModel = layoutListViewModel;
        this.recordingsViewModel = recordingsViewModel;
        serverUrl = apiBaseAddress;

        // Default view
        CurrentView = liveViewViewModel;

        hubService.OnHubConnectionStateChanged += state =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                HubConnectionState = state;
            });
        };
    }

    [RelayCommand]
    private void ViewLiveView()
    {
        CurrentView = liveViewViewModel;
        liveViewViewModel.LoadCommand.Execute(parameter: null);
    }

    [RelayCommand]
    private void ViewDashboard()
    {
        CurrentView = dashboardViewModel;
        dashboardViewModel.LoadCommand.Execute(parameter: null);
    }

    [RelayCommand]
    private void ViewCameras()
    {
        CurrentView = cameraListViewModel;
        cameraListViewModel.LoadCommand.Execute(parameter: null);
    }

    [RelayCommand]
    private void ViewLayouts()
    {
        CurrentView = layoutListViewModel;
        layoutListViewModel.LoadCommand.Execute(parameter: null);
    }

    [RelayCommand]
    private void ViewRecordings()
    {
        CurrentView = recordingsViewModel;
        recordingsViewModel.LoadCommand.Execute(parameter: null);
    }

    [RelayCommand("ShowSettings")]
    private async Task ShowSettingsAsync()
    {
        var viewModel = new SettingsDialogViewModel(gatewayService);
        await viewModel.LoadSettingsAsync().ConfigureAwait(true);

        var dialog = new SettingsDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private static void ShowAbout()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
        var year = DateTime.Now.Year;

        var dialog = new AboutDialog(version, year)
        {
            Owner = Application.Current.MainWindow,
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private void ShowCheckForUpdates()
    {
        var viewModel = new CheckForUpdatesDialogViewModel(gitHubReleaseService);

        var dialog = new CheckForUpdatesDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private static void Exit()
        => Application.Current.Shutdown();
}