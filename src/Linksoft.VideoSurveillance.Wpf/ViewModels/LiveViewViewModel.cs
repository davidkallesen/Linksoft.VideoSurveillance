namespace Linksoft.VideoSurveillance.Wpf.ViewModels;

/// <summary>
/// View model for the live camera grid view.
/// Manages camera tiles, grid layout, and stream lifecycle.
/// </summary>
public sealed partial class LiveViewViewModel : ViewModelBase, IDisposable
{
    private readonly GatewayService gatewayService;
    private readonly SurveillanceHubService hubService;
    private readonly IVideoPlayerFactory videoPlayerFactory;
    private readonly string apiBaseAddress;
    private bool disposed;

    [ObservableProperty]
    private ObservableCollection<CameraTileViewModel> cameraTiles = [];

    [ObservableProperty]
    private int gridRows;

    [ObservableProperty]
    private int gridColumns;

    [ObservableProperty]
    private bool isLoading;

    public LiveViewViewModel(
        GatewayService gatewayService,
        SurveillanceHubService hubService,
        IVideoPlayerFactory videoPlayerFactory,
        string apiBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(gatewayService);
        ArgumentNullException.ThrowIfNull(hubService);
        ArgumentNullException.ThrowIfNull(videoPlayerFactory);

        this.gatewayService = gatewayService;
        this.hubService = hubService;
        this.videoPlayerFactory = videoPlayerFactory;
        this.apiBaseAddress = apiBaseAddress;
    }

    [RelayCommand("Load")]
    private async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var cameras = await gatewayService
                .GetCamerasAsync()
                .ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Stop & dispose existing tiles
                StopAndDisposeTiles();

                if (cameras is null || cameras.Length == 0)
                {
                    GridRows = 0;
                    GridColumns = 0;
                    return;
                }

                // Create a tile per camera
                foreach (var camera in cameras)
                {
                    var tile = new CameraTileViewModel(videoPlayerFactory, hubService, apiBaseAddress)
                    {
                        CameraId = camera.Id,
                        DisplayName = camera.DisplayName,
                        Description = camera.Description ?? string.Empty,
                        ConnectionState = camera.ConnectionState?.ToString()?.ToLowerInvariant() ?? "disconnected",
                        IsRecording = camera.IsRecording,
                    };

                    CameraTiles.Add(tile);
                }

                // Calculate grid dimensions
                var (rows, columns) = GridLayoutHelper.CalculateGridDimensions(CameraTiles.Count);
                GridRows = rows;
                GridColumns = columns;
            });

            // Start all streams (off UI thread)
            var tiles = await Application.Current.Dispatcher.InvokeAsync(() => CameraTiles.ToList());
            foreach (var tile in tiles)
            {
                try
                {
                    await tile.StartStreamAsync().ConfigureAwait(false);
                }
                catch (HttpRequestException)
                {
                    // Stream start failed - tile will show "No Stream"
                }
            }
        }
        catch (HttpRequestException)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StopAndDisposeTiles();
                GridRows = 0;
                GridColumns = 0;
            });
        }
        finally
        {
            await Application.Current.Dispatcher.InvokeAsync(() => IsLoading = false);
        }
    }

    [RelayCommand("StopAll")]
    private async Task StopAllAsync()
    {
        var tiles = await Application.Current.Dispatcher.InvokeAsync(() => CameraTiles.ToList());

        foreach (var tile in tiles)
        {
            try
            {
                await tile.StopStreamAsync().ConfigureAwait(false);
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        await Application.Current.Dispatcher.InvokeAsync(StopAndDisposeTiles);
    }

    public void OpenFullScreen(CameraTileViewModel tile)
    {
        ArgumentNullException.ThrowIfNull(tile);

        var viewModel = new FullScreenCameraWindowViewModel(
            hubService,
            tile.CameraId,
            tile.Player,
            tile.DisplayName,
            tile.Description);

        // Window self-disposes on close (via Closed event handler)
#pragma warning disable CA2000
        var window = new FullScreenCameraWindow(viewModel);
        window.Show();
#pragma warning restore CA2000
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        StopAndDisposeTiles();
        GC.SuppressFinalize(this);
    }

    private void StopAndDisposeTiles()
    {
        foreach (var tile in CameraTiles)
        {
            tile.Dispose();
        }

        CameraTiles.Clear();
    }
}