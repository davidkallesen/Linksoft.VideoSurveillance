namespace Linksoft.Wpf.CameraWall.App;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Fluent.IRibbonWindow
{
    /// <summary>
    /// Gets the title bar. Returns null since NiceWindow doesn't use Fluent.Ribbon's title bar.
    /// Implementing IRibbonWindow prevents binding warnings from Ribbon's internal FindAncestor bindings.
    /// </summary>
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
    public new Fluent.RibbonTitleBar? TitleBar => null;
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required

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
        viewModel.Initialize(CameraGridControl);
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

    private void CameraGrid_FullScreenRequested(
        object? sender,
        CameraConfiguration e)
    {
        viewModel.ShowFullScreen(e);
    }

    private void CameraGrid_ConnectionStateChanged(
        object? sender,
        CameraConnectionChangedEventArgs e)
    {
        viewModel.OnConnectionStateChanged(e);
    }

    private void CameraGrid_PositionChanged(
        object? sender,
        CameraPositionChangedEventArgs e)
    {
        viewModel.OnPositionChanged(e);
    }

    private void CameraGrid_EditCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        viewModel.EditCamera(e);
    }

    private void CameraGrid_DeleteCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        viewModel.DeleteCamera(e);
    }

    // Hide CameraGrid when Backstage opens to avoid z-order issues with hardware-accelerated video
    private void OnBackstageIsOpenChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
        => CameraGridControl.Visibility = (bool)e.NewValue
            ? Visibility.Hidden
            : Visibility.Visible;
}