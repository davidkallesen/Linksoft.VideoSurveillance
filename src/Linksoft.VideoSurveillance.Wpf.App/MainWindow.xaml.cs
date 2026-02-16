namespace Linksoft.VideoSurveillance.Wpf.App;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Fluent.IRibbonWindow
{
    /// <summary>
    /// Gets the title bar. Returns null since NiceWindow doesn't use Fluent.Ribbon's title bar.
    /// Implementing IRibbonWindow prevents binding warnings from Ribbon's internal FindAncestor bindings.
    /// </summary>
    public Fluent.RibbonTitleBar? TitleBar => null;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}