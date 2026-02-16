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
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
    public new Fluent.RibbonTitleBar? TitleBar => null;
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}