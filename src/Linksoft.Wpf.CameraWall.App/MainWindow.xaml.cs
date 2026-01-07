namespace Linksoft.Wpf.CameraWall.App;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow
{
    private readonly MainWindowViewModel viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        this.viewModel = viewModel;
        DataContext = viewModel;

        Loaded += OnLoaded;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
    }

    private void OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        viewModel.OnLoaded(this, e);
        viewModel.Initialize(CameraWallControl);
    }

    private void OnClosing(
        object? sender,
        CancelEventArgs e)
        => viewModel.OnClosing(this, e);

    private void OnKeyDown(
        object sender,
        KeyEventArgs e)
        => viewModel.OnKeyDown(this, e);

    private void OnKeyUp(
        object sender,
        KeyEventArgs e)
        => viewModel.OnKeyUp(this, e);

    private void CameraWall_FullScreenRequested(
        object? sender,
        CameraConfiguration e)
    {
        viewModel.ShowFullScreen(e);
    }

    private void CameraWall_ConnectionStateChanged(
        object? sender,
        CameraConnectionChangedEventArgs e)
    {
        viewModel.OnConnectionStateChanged(e);
    }

    private void CameraWall_PositionChanged(
        object? sender,
        CameraPositionChangedEventArgs e)
    {
        viewModel.OnPositionChanged(e);
    }

    private void CameraWall_EditCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        viewModel.EditCamera(e);
    }

    private void CameraWall_DeleteCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        viewModel.DeleteCamera(e);
    }
}