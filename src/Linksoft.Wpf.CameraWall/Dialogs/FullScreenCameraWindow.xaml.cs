namespace Linksoft.Wpf.CameraWall.Dialogs;

/// <summary>
/// Fullscreen window for displaying a single camera stream.
/// </summary>
public partial class FullScreenCameraWindow : IDisposable
{
    private const int WmKeyDown = 0x0100;
    private const int WmRightButtonUp = 0x0205;
    private const int VkEscape = 0x1B;

    private readonly FullScreenCameraWindowViewModel viewModel;
    private Point lastMousePosition;
    private bool disposed;

    public FullScreenCameraWindow(FullScreenCameraWindowViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        this.viewModel = viewModel;
        DataContext = viewModel;

        viewModel.CloseRequested += OnCloseRequested;
        Closed += OnWindowClosed;

        // Use InputManager to capture mouse input before FlyleafHost intercepts it
        InputManager.Current.PreProcessInput += OnPreProcessInput;

        // Use ComponentDispatcher to capture keyboard at Win32 level (FlyleafHost uses HwndHost)
        ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
            InputManager.Current.PreProcessInput -= OnPreProcessInput;
            viewModel.CloseRequested -= OnCloseRequested;
            Closed -= OnWindowClosed;
            viewModel.Dispose();
        }

        disposed = true;
    }

    private void OnCloseRequested(
        object? sender,
        DialogClosedEventArgs e)
        => Close();

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
        => Dispose();

    private void OnPreProcessInput(
        object sender,
        PreProcessInputEventArgs e)
    {
        // Skip if disposed or not active
        if (disposed || !IsActive)
        {
            return;
        }

        try
        {
            switch (e.StagingItem.Input)
            {
                case MouseButtonEventArgs mouseButtonArgs:
                    HandleMouseButtonInput(mouseButtonArgs);
                    break;
                case MouseEventArgs mouseArgs:
                    HandleMouseInput(mouseArgs);
                    break;
            }
        }
        catch
        {
            // Silently ignore any errors to avoid interfering with other windows
        }
    }

    private void HandleMouseInput(MouseEventArgs e)
    {
        var currentPosition = e.GetPosition(this);

        if (currentPosition == lastMousePosition)
        {
            return;
        }

        lastMousePosition = currentPosition;
        viewModel.OnMouseMoved();
    }

    private void HandleMouseButtonInput(MouseButtonEventArgs e)
    {
        // Show context menu on right-click release
        if (e is { ChangedButton: MouseButton.Right, ButtonState: MouseButtonState.Released })
        {
            ShowContextMenu();
        }
    }

    private void OnThreadFilterMessage(
        ref MSG msg,
        ref bool handled)
    {
        // Skip if disposed, already handled, or not active
        if (disposed || handled || !IsActive)
        {
            return;
        }

        try
        {
            switch (msg.message)
            {
                // Handle ESC key at Win32 message level
                case WmKeyDown when (int)msg.wParam == VkEscape:
                    viewModel.CloseCommand.Execute(parameter: null);
                    handled = true;
                    break;

                // Handle right-click to show context menu
                case WmRightButtonUp:
                    ShowContextMenu();
                    handled = true;
                    break;
            }
        }
        catch
        {
            // Silently ignore any errors to avoid interfering with other windows
        }
    }

    private void ShowContextMenu()
    {
        // Create context menu dynamically to avoid binding conflicts
        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(new MenuItem
        {
            Header = Translations.Close,
            Command = viewModel.CloseCommand,
        });
        contextMenu.IsOpen = true;
    }
}