namespace Linksoft.VideoSurveillance.Wpf.ViewModels;

/// <summary>
/// View model for the layout list view with full CRUD and apply operations.
/// </summary>
public partial class LayoutListViewModel : ViewModelBase
{
    private readonly GatewayService gatewayService;

    [ObservableProperty]
    private ObservableCollection<LayoutItemViewModel> layouts = [];

    [ObservableProperty]
    private bool isLoading;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutListViewModel"/> class.
    /// </summary>
    public LayoutListViewModel(GatewayService gatewayService)
    {
        ArgumentNullException.ThrowIfNull(gatewayService);

        this.gatewayService = gatewayService;
    }

    [RelayCommand("Load")]
    private async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var result = await gatewayService
                .GetLayoutsAsync()
                .ConfigureAwait(false);

            Application.Current.Dispatcher.Invoke(() =>
            {
                Layouts.Clear();

                if (result is not null)
                {
                    foreach (var layout in result)
                    {
                        Layouts.Add(LayoutItemViewModel.FromLayout(layout));
                    }
                }
            });
        }
        catch (HttpRequestException)
        {
            Application.Current.Dispatcher.Invoke(() => Layouts.Clear());
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() => IsLoading = false);
        }
    }

    [RelayCommand("AddLayout")]
    private async Task AddLayoutAsync()
    {
        var viewModel = new LayoutEditDialogViewModel();
        var dialog = new LayoutEditDialog(viewModel)
        {
            Owner = Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await gatewayService
                .CreateLayoutAsync(viewModel.BuildCreateRequest())
                .ConfigureAwait(false);

            await LoadAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(
                    $"Failed to create layout: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));
        }
    }

    [RelayCommand("EditLayout")]
    private async Task EditLayoutAsync(LayoutItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        // Build a Layout object from the item to pass to the dialog VM
        var layout = new Layout(
            Id: item.Id,
            Name: item.Name,
            Rows: item.Rows,
            Columns: item.Columns,
            Cameras: null!);

        var result = false;
        LayoutEditDialogViewModel? viewModel = null;

        Application.Current.Dispatcher.Invoke(() =>
        {
            viewModel = new LayoutEditDialogViewModel(layout);
            var dialog = new LayoutEditDialog(viewModel)
            {
                Owner = Application.Current.MainWindow,
            };

            result = dialog.ShowDialog() == true;
        });

        if (!result || viewModel?.LayoutId is null)
        {
            return;
        }

        try
        {
            await gatewayService
                .UpdateLayoutAsync(viewModel.LayoutId.Value, viewModel.BuildUpdateRequest())
                .ConfigureAwait(false);

            await LoadAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(
                    $"Failed to update layout: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));
        }
    }

    [RelayCommand("DeleteLayout")]
    private async Task DeleteLayoutAsync(LayoutItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var confirmed = MessageBox.Show(
            $"Are you sure you want to delete layout '{item.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmed != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await gatewayService
                .DeleteLayoutAsync(item.Id)
                .ConfigureAwait(false);

            await LoadAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(
                    $"Failed to delete layout: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));
        }
    }

    [RelayCommand("ApplyLayout")]
    private async Task ApplyLayoutAsync(LayoutItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            await gatewayService
                .ApplyLayoutAsync(item.Id)
                .ConfigureAwait(false);

            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(
                    $"Layout '{item.Name}' applied successfully.",
                    "Layout Applied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information));
        }
        catch (HttpRequestException ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(
                    $"Failed to apply layout: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error));
        }
    }
}