#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace Linksoft.Wpf.CameraWall.UserControls;

/// <summary>
/// Control for displaying multiple camera tiles in a dynamic grid layout.
/// </summary>
public partial class CameraWall
{
    [DependencyProperty(DefaultValue = 1)]
    private int gridRowCount = 1;

    [DependencyProperty(PropertyChangedCallback = nameof(OnCameraTilesChanged))]
    private ObservableCollection<CameraConfiguration> cameraTiles = [];

    [DependencyProperty(DefaultValue = true)]
    private bool autoSave = true;

    private Point dragStartPoint;
    private bool isDragging;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraWall"/> class.
    /// </summary>
    public CameraWall()
    {
        InitializeComponent();
        CameraTiles = [];

        RegisterMessages();
    }

    /// <summary>
    /// Occurs when a full screen request is made for a camera.
    /// </summary>
    public event EventHandler<CameraConfiguration>? FullScreenRequested;

    /// <summary>
    /// Occurs when a camera's connection state changes.
    /// </summary>
    public event EventHandler<CameraConnectionChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Occurs when camera positions change (for auto-save).
    /// </summary>
    public event EventHandler<CameraPositionChangedEventArgs>? PositionChanged;

    /// <summary>
    /// Occurs when an edit camera request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? EditCameraRequested;

    /// <summary>
    /// Occurs when a delete camera request is made.
    /// </summary>
    public event EventHandler<CameraConfiguration>? DeleteCameraRequested;

    /// <summary>
    /// Adds a camera to the wall.
    /// </summary>
    /// <param name="camera">The camera configuration to add.</param>
    public void AddCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        CameraTiles.Add(camera);
        UpdateGridLayout();
        UpdateEmptyState();
    }

    /// <summary>
    /// Removes a camera from the wall.
    /// </summary>
    /// <param name="cameraId">The identifier of the camera to remove.</param>
    /// <returns>True if the camera was removed.</returns>
    public bool RemoveCamera(Guid cameraId)
    {
        var camera = CameraTiles.FirstOrDefault(c => c.Id == cameraId);
        if (camera is null)
        {
            return false;
        }

        CameraTiles.Remove(camera);
        UpdateGridLayout();
        UpdateEmptyState();
        return true;
    }

    /// <summary>
    /// Swaps a camera with an adjacent camera.
    /// </summary>
    /// <param name="cameraId">The identifier of the camera to swap.</param>
    /// <param name="direction">The direction to swap.</param>
    public void SwapCamera(
        Guid cameraId,
        SwapDirection direction)
    {
        if (CameraTiles is null)
        {
            return;
        }

        var index = CameraTiles
            .ToList()
            .FindIndex(c => c.Id == cameraId);

        if (index < 0)
        {
            return;
        }

        var targetIndex = direction == SwapDirection.Left ? index - 1 : index + 1;

        if (targetIndex < 0 || targetIndex >= CameraTiles.Count)
        {
            return;
        }

        var camera = CameraTiles[index];

        // Use remove/insert instead of Move() to avoid WPF UI Automation bug
        // in ItemAutomationPeer.GetNameCore()
        CameraTiles.RemoveAt(index);
        CameraTiles.Insert(targetIndex, camera);

        PositionChanged?.Invoke(this, new CameraPositionChangedEventArgs(camera, index, targetIndex));
    }

    /// <summary>
    /// Clears all cameras from the wall.
    /// </summary>
    public void Clear()
    {
        CameraTiles.Clear();
        UpdateGridLayout();
        UpdateEmptyState();
    }

    /// <summary>
    /// Gets the current layout as a list of camera layout items.
    /// </summary>
    /// <returns>The list of camera layout items in order.</returns>
    [SuppressMessage("Design", "MA0016:Prefer using collection abstraction instead of implementation", Justification = "Return type matches CameraLayout.Items property type")]
    public List<CameraLayoutItem> GetCurrentLayout()
        => CameraTiles
            .Select(
                (camera, index) => new CameraLayoutItem
                {
                    CameraId = camera.Id,
                    OrderNumber = index,
                })
            .ToList();

    /// <summary>
    /// Applies a layout to the camera wall.
    /// </summary>
    /// <param name="layout">The layout to apply.</param>
    /// <param name="cameras">The available cameras.</param>
    public void ApplyLayout(
        CameraLayout layout,
        IEnumerable<CameraConfiguration> cameras)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(cameras);

        Clear();

        var cameraDict = cameras.ToDictionary(c => c.Id);

        foreach (var item in layout.Items.OrderBy(i => i.OrderNumber))
        {
            if (cameraDict.TryGetValue(item.CameraId, out var camera))
            {
                AddCamera(camera);
            }
        }
    }

    private static void OnCameraTilesChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is CameraWall wall)
        {
            wall.UpdateGridLayout();
            wall.UpdateEmptyState();
            wall.UpdateSwapCapabilities();

            if (e.OldValue is ObservableCollection<CameraConfiguration> oldCollection)
            {
                oldCollection.CollectionChanged -= wall.OnCameraTilesCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<CameraConfiguration> newCollection)
            {
                newCollection.CollectionChanged += wall.OnCameraTilesCollectionChanged;
            }
        }
    }

    private void OnCameraTilesCollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateGridLayout();
        UpdateEmptyState();
        UpdateSwapCapabilities();
    }

    private void RegisterMessages()
    {
        Messenger.Default.Register<CameraAddMessage>(this, OnCameraAddMessage);
        Messenger.Default.Register<CameraRemoveMessage>(this, OnCameraRemoveMessage);
        Messenger.Default.Register<CameraSwapMessage>(this, OnCameraSwapMessage);
    }

    private void OnCameraAddMessage(CameraAddMessage message)
    {
        Dispatcher.Invoke(() => AddCamera(message.Camera));
    }

    private void OnCameraRemoveMessage(CameraRemoveMessage message)
    {
        Dispatcher.Invoke(() => RemoveCamera(message.CameraId));
    }

    private void OnCameraSwapMessage(CameraSwapMessage message)
    {
        Dispatcher.Invoke(() => SwapCamera(message.CameraId, message.Direction));
    }

    private void UpdateGridLayout()
    {
        GridRowCount = GridLayoutHelper.CalculateRowCount(CameraTiles.Count);
    }

    private void UpdateEmptyState()
    {
        EmptyStateText.Visibility = CameraTiles.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateSwapCapabilities()
    {
        if (CameraTiles is null)
        {
            return;
        }

        for (var i = 0; i < CameraTiles.Count; i++)
        {
            CameraTiles[i].CanSwapLeft = i > 0;
            CameraTiles[i].CanSwapRight = i < CameraTiles.Count - 1;
        }
    }

    private void CameraTile_FullScreenRequested(
        object? sender,
        CameraConfiguration e)
    {
        FullScreenRequested?.Invoke(this, e);
    }

    private void CameraTile_SwapLeftRequested(
        object? sender,
        CameraConfiguration e)
    {
        SwapCamera(e.Id, SwapDirection.Left);
    }

    private void CameraTile_SwapRightRequested(
        object? sender,
        CameraConfiguration e)
    {
        SwapCamera(e.Id, SwapDirection.Right);
    }

    private void CameraTile_ConnectionStateChanged(
        object? sender,
        CameraConnectionChangedEventArgs e)
    {
        ConnectionStateChanged?.Invoke(this, e);
    }

    private void CameraTile_EditCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        EditCameraRequested?.Invoke(this, e);
    }

    private void CameraTile_DeleteCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        DeleteCameraRequested?.Invoke(this, e);
    }

    private void CameraTile_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || isDragging)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var diff = dragStartPoint - currentPosition;

        if ((Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
             Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance) ||
            sender is not CameraTile { Camera: not null } tile)
        {
            return;
        }

        isDragging = true;
        DragDrop.DoDragDrop(tile, tile.Camera, DragDropEffects.Move);
        isDragging = false;
    }

    private void CameraTile_DragOver(
        object sender,
        DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(CameraConfiguration)))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void CameraTile_Drop(
        object sender,
        DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(CameraConfiguration)))
        {
            return;
        }

        var sourceCamera = (CameraConfiguration)e.Data.GetData(typeof(CameraConfiguration))!;
        if (CameraTiles is null ||
            sender is not CameraTile targetTile ||
            targetTile.Camera is null)
        {
            return;
        }

        var sourceIndex = CameraTiles
            .ToList()
            .FindIndex(c => c.Id == sourceCamera.Id);
        var targetIndex = CameraTiles
            .ToList()
            .FindIndex(c => c.Id == targetTile.Camera.Id);

        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        // Use remove/insert instead of Move() to avoid WPF UI Automation bug
        // in ItemAutomationPeer.GetNameCore()
        CameraTiles.RemoveAt(sourceIndex);
        CameraTiles.Insert(targetIndex, sourceCamera);

        PositionChanged?.Invoke(this, new CameraPositionChangedEventArgs(sourceCamera, sourceIndex, targetIndex));

        e.Handled = true;
    }
}