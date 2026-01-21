#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0414 // Field is assigned but its value is never used
namespace Linksoft.Wpf.CameraWall.UserControls;

/// <summary>
/// Control for displaying multiple camera tiles in a dynamic grid layout.
/// </summary>
public partial class CameraGrid
{
    [DependencyProperty(DefaultValue = 1)]
    private int gridRowCount;

    [DependencyProperty(PropertyChangedCallback = nameof(OnCameraTilesChanged))]
    private ObservableCollection<CameraConfiguration> cameraTiles = [];

    [DependencyProperty(DefaultValue = true)]
    private bool autoSave = true;

    [DependencyProperty(DefaultValue = true)]
    private bool showOverlayTitle;

    [DependencyProperty(DefaultValue = true)]
    private bool showOverlayDescription;

    [DependencyProperty(DefaultValue = true)]
    private bool showOverlayConnectionStatus;

    [DependencyProperty(DefaultValue = false)]
    private bool showOverlayTime;

    [DependencyProperty(DefaultValue = 0.6)]
    private double overlayOpacity;

    [DependencyProperty(DefaultValue = true)]
    private bool allowDragAndDropReorder;

    [DependencyProperty]
    private string? snapshotDirectory;

    [DependencyProperty(DefaultValue = true)]
    private bool autoConnectOnLoad;

    // Connection settings
    [DependencyProperty(DefaultValue = 10)]
    private int connectionTimeoutSeconds;

    [DependencyProperty(DefaultValue = 5)]
    private int reconnectDelaySeconds;

    [DependencyProperty(DefaultValue = 3)]
    private int maxReconnectAttempts;

    [DependencyProperty(DefaultValue = true)]
    private bool autoReconnectOnFailure;

    // Notification settings
    [DependencyProperty(DefaultValue = true)]
    private bool showNotificationOnDisconnect;

    [DependencyProperty(DefaultValue = false)]
    private bool showNotificationOnReconnect;

    [DependencyProperty(DefaultValue = false)]
    private bool playNotificationSound;

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraGrid"/> class.
    /// </summary>
    public CameraGrid()
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
    /// This optimized implementation swaps the Player instances between CameraTile controls
    /// to avoid disconnecting and reconnecting streams.
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

        var sourceIndex = CameraTiles
            .ToList()
            .FindIndex(c => c.Id == cameraId);

        if (sourceIndex < 0)
        {
            return;
        }

        var targetIndex = direction == SwapDirection.Left ? sourceIndex - 1 : sourceIndex + 1;

        if (targetIndex < 0 || targetIndex >= CameraTiles.Count)
        {
            return;
        }

        var sourceCamera = CameraTiles[sourceIndex];
        PerformSwap(sourceIndex, targetIndex, sourceCamera);
    }

    /// <summary>
    /// Gets the CameraTile control at the specified collection index.
    /// </summary>
    /// <param name="index">The index in the CameraTiles collection.</param>
    /// <returns>The CameraTile control, or null if not found.</returns>
    private CameraTile? GetCameraTileAt(int index)
    {
        var container = CameraItemsControl.ItemContainerGenerator.ContainerFromIndex(index);
        if (container is null)
        {
            return null;
        }

        return VisualTreeHelperEx.FindChild<CameraTile>(container);
    }

    /// <summary>
    /// Performs an optimized swap between two camera positions.
    /// Keeps players in place and swaps streams to avoid disconnecting/reconnecting.
    /// </summary>
    private void PerformSwap(
        int sourceIndex,
        int targetIndex,
        CameraConfiguration sourceCamera)
    {
        var sourceTile = GetCameraTileAt(sourceIndex);
        var targetTile = GetCameraTileAt(targetIndex);

        if (sourceTile is not null && targetTile is not null)
        {
            var targetCamera = CameraTiles[targetIndex];
            var sourceUri = sourceCamera.BuildUri();
            var targetUri = targetCamera.BuildUri();

            sourceTile.PrepareForSwap();
            targetTile.PrepareForSwap();

            CameraTiles[sourceIndex] = targetCamera;
            CameraTiles[targetIndex] = sourceCamera;

            sourceTile.CompleteSwap();
            targetTile.CompleteSwap();

            OpenStreamsInParallel(sourceTile.Player, targetUri, targetTile.Player, sourceUri);
        }
        else
        {
            CameraTiles.RemoveAt(sourceIndex);
            CameraTiles.Insert(targetIndex, sourceCamera);
        }

        UpdateSwapCapabilities();
        PositionChanged?.Invoke(this, new CameraPositionChangedEventArgs(sourceCamera, sourceIndex, targetIndex));
    }

    /// <summary>
    /// Opens streams on two players in parallel to minimize swap time.
    /// </summary>
    private static void OpenStreamsInParallel(
        Player? player1,
        Uri uri1,
        Player? player2,
        Uri uri2)
    {
        if (player1 is null || player2 is null)
        {
            return;
        }

        var uri1Str = uri1.ToString();
        var uri2Str = uri2.ToString();

        _ = Task.Run(() => player1.Open(uri1Str));
        _ = Task.Run(() => player2.Open(uri2Str));
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
    /// Reconnects all camera streams.
    /// </summary>
    public void ReconnectAll()
    {
        for (var i = 0; i < CameraTiles.Count; i++)
        {
            var tile = GetCameraTileAt(i);
            tile?.Reconnect();
        }
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
        if (d is CameraGrid grid)
        {
            grid.UpdateGridLayout();
            grid.UpdateEmptyState();
            grid.UpdateSwapCapabilities();

            if (e.OldValue is ObservableCollection<CameraConfiguration> oldCollection)
            {
                oldCollection.CollectionChanged -= grid.OnCameraTilesCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<CameraConfiguration> newCollection)
            {
                newCollection.CollectionChanged += grid.OnCameraTilesCollectionChanged;
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

    private void OnFullScreenRequested(
        object? sender,
        CameraConfiguration e)
    {
        FullScreenRequested?.Invoke(this, e);
    }

    private void OnSwapLeftRequested(
        object? sender,
        CameraConfiguration e)
    {
        SwapCamera(e.Id, SwapDirection.Left);
    }

    private void OnSwapRightRequested(
        object? sender,
        CameraConfiguration e)
    {
        SwapCamera(e.Id, SwapDirection.Right);
    }

    private void OnConnectionStateChanged(
        object? sender,
        CameraConnectionChangedEventArgs e)
    {
        ConnectionStateChanged?.Invoke(this, e);
    }

    private void OnEditCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        EditCameraRequested?.Invoke(this, e);
    }

    private void OnDeleteCameraRequested(
        object? sender,
        CameraConfiguration e)
    {
        DeleteCameraRequested?.Invoke(this, e);
    }

    private void OnCameraDropped(
        object? sender,
        CameraConfiguration sourceCamera)
    {
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

        PerformSwap(sourceIndex, targetIndex, sourceCamera);
    }
}