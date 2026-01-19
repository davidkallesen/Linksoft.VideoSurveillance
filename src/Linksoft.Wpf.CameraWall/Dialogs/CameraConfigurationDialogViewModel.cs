// ReSharper disable InvertIf
namespace Linksoft.Wpf.CameraWall.Dialogs;

public partial class CameraConfigurationDialogViewModel : ViewModelDialogBase
{
    private readonly CameraConfiguration originalCamera;
    private readonly bool isNew;
    private readonly IReadOnlyCollection<string> existingIpAddresses;

    [ObservableProperty(AfterChangedCallback = nameof(OnIsTestingChanged))]
    private bool isTesting;

    private string? testResultInternal;

    /// <summary>
    /// Gets the test result, returning "Not tested" translation when null.
    /// </summary>
    public string TestResult
    {
        get => testResultInternal ?? Translations.NotTested;
    }

    /// <summary>
    /// Clears the test result (resets to "Not tested").
    /// </summary>
    private void ClearTestResult()
    {
        if (testResultInternal is not null)
        {
            testResultInternal = null;
            OnPropertyChanged(nameof(TestResult));
        }
    }

    /// <summary>
    /// Sets the test result value.
    /// </summary>
    private void SetTestResult(string value)
    {
        if (testResultInternal != value)
        {
            testResultInternal = value;
            OnPropertyChanged(nameof(TestResult));
        }
    }

    [ObservableProperty]
    private string? ipAddressError;

    public CameraConfigurationDialogViewModel(
        CameraConfiguration camera,
        bool isNew,
        IReadOnlyCollection<string> existingIpAddresses)
    {
        ArgumentNullException.ThrowIfNull(camera);

        originalCamera = camera;
        this.isNew = isNew;
        this.existingIpAddresses = existingIpAddresses ?? [];

        // Create a clone for editing - changes only apply when Save is clicked
        Camera = camera.Clone();

        Camera.PropertyChanged += OnCameraPropertyChanged;
        NetworkScanner.EntrySelected += OnNetworkScannerEntrySelected;
    }

    private static void OnIsTestingChanged()
        => CommandManager.InvalidateRequerySuggested();

    public event EventHandler<DialogClosedEventArgs>? CloseRequested;

    public CameraConfiguration Camera { get; }

    public string DialogTitle
        => isNew
            ? Translations.AddCamera
            : Translations.EditCamera;

    public IDictionary<string, string> ProtocolItems { get; } = Enum
        .GetValues<CameraProtocol>()
        .ToDictionary(p => p.ToString(), p => p.ToString(), StringComparer.Ordinal);

    public string SelectedProtocolKey
    {
        get => Camera.Connection.Protocol.ToString();
        set
        {
            if (Enum.TryParse<CameraProtocol>(value, out var protocol))
            {
                Camera.Connection.Protocol = protocol;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> OverlayPositionItems { get; } = Enum
        .GetValues<OverlayPosition>()
        .ToDictionary(p => p.ToString(), p => p.ToString(), StringComparer.Ordinal);

    public string SelectedOverlayPositionKey
    {
        get => Camera.Display.OverlayPosition.ToString();
        set
        {
            if (Enum.TryParse<OverlayPosition>(value, out var position))
            {
                Camera.Display.OverlayPosition = position;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> RtspTransportItems { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["tcp"] = "TCP",
        ["udp"] = "UDP",
    };

    public string SelectedRtspTransportKey
    {
        get => Camera.Stream.RtspTransport;
        set
        {
            Camera.Stream.RtspTransport = value;
            RaisePropertyChanged();
        }
    }

    public NetworkScannerViewModel NetworkScanner { get; } = CreateNetworkScanner();

    private static NetworkScannerViewModel CreateNetworkScanner()
    {
        var scanner = new NetworkScannerViewModel
        {
            // Common camera ports: RTSP, HTTP, HTTP alt, RTSP alt
            PortsNumbers = [554, 80, 8080, 8554],
        };

        var localIpAddress = IPv4AddressHelper.GetLocalAddress();
        if (localIpAddress is not null)
        {
            // Use /24 subnet (255.255.255.0) which is typical for most local networks
            var (startIp, endIp) = IPv4AddressHelper.GetFirstAndLastAddressInRange(localIpAddress, cidrLength: 24);
            scanner.StartIpAddress = startIp.ToString();
            scanner.EndIpAddress = endIp.ToString();
        }

        return scanner;
    }

    private void OnNetworkScannerEntrySelected(
        object? sender,
        NetworkHostSelectedEventArgs e)
    {
        if (e.SelectedHost is null)
        {
            return;
        }

        // Populate IP address
        Camera.Connection.IpAddress = e.SelectedHost.IpAddress.ToString();

        // Set display name from hostname if available and display name is default
        if (!string.IsNullOrWhiteSpace(e.SelectedHost.Hostname) &&
            (string.IsNullOrWhiteSpace(Camera.Display.DisplayName) || Camera.Display.DisplayName == Translations.NewCamera))
        {
            Camera.Display.DisplayName = e.SelectedHost.Hostname;
        }

        // Auto-detect protocol and port from open ports
        var openPorts = e.SelectedHost.OpenPortNumbers.ToList();
        if (openPorts.Contains(554))
        {
            Camera.Connection.Protocol = CameraProtocol.Rtsp;
            Camera.Connection.Port = 554;
        }
        else if (openPorts.Contains(8554))
        {
            Camera.Connection.Protocol = CameraProtocol.Rtsp;
            Camera.Connection.Port = 8554;
        }
        else if (openPorts.Contains(80))
        {
            Camera.Connection.Protocol = CameraProtocol.Http;
            Camera.Connection.Port = 80;
        }
        else if (openPorts.Contains(8080))
        {
            Camera.Connection.Protocol = CameraProtocol.Http;
            Camera.Connection.Port = 8080;
        }
        else if (openPorts.Count > 0)
        {
            // Use first open port
            Camera.Connection.Port = openPorts[0];
        }

        // Notify property changes for combo boxes
        OnPropertyChanged(nameof(SelectedProtocolKey));
    }

    private void OnCameraPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        // Reset test result when connection or authentication properties change
        if (e.PropertyName is nameof(Camera.Connection) or nameof(Camera.Authentication))
        {
            ClearTestResult();
        }

        // Validate IP address uniqueness when connection changes
        if (e.PropertyName == nameof(Camera.Connection))
        {
            ValidateIpAddress();
        }

        // Always invalidate commands when any property changes (for Save button CanExecute)
        CommandManager.InvalidateRequerySuggested();
    }

    private void ValidateIpAddress()
    {
        if (string.IsNullOrWhiteSpace(Camera.Connection.IpAddress))
        {
            IpAddressError = null;
            return;
        }

        var isDuplicate = existingIpAddresses.Contains(Camera.Connection.IpAddress, StringComparer.OrdinalIgnoreCase);
        IpAddressError = isDuplicate ? Translations.IpAddressAlreadyExists : null;
    }

    private bool IsIpAddressUnique()
    {
        if (string.IsNullOrWhiteSpace(Camera.Connection.IpAddress))
        {
            return true;
        }

        return !existingIpAddresses.Contains(Camera.Connection.IpAddress, StringComparer.OrdinalIgnoreCase);
    }

    [RelayCommand("TestConnection", CanExecute = nameof(CanTestConnection))]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        ClearTestResult();

        try
        {
            var uri = Camera.BuildUri();
            await TestStreamWithPlayerAsync(uri).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SetTestResult(string.Format(CultureInfo.CurrentCulture, Translations.FailedWithStatus1, ex.Message));
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task TestStreamWithPlayerAsync(Uri uri)
    {
        using var player = CreateTestPlayer();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var tcs = new TaskCompletionSource<bool>();

        SubscribeToPlayerStatus(player, tcs);

        try
        {
            player.Open(uri.ToString());
            await WaitForConnectionResultAsync(tcs, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            player.PropertyChanged -= OnPlayerStatusChanged;
            player.Stop();
        }

        void OnPlayerStatusChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Player.Status))
            {
                return;
            }

            switch (player.Status)
            {
                case Status.Playing:
                    tcs.TrySetResult(true);
                    break;
                case Status.Failed:
                    tcs.TrySetResult(false);
                    break;
            }
        }

        void SubscribeToPlayerStatus(
            Player p,
            TaskCompletionSource<bool> taskSource)
        {
            p.PropertyChanged += OnPlayerStatusChanged;
        }
    }

    private static Player CreateTestPlayer()
    {
        var config = new Config
        {
            Player =
            {
                AutoPlay = true,
            },
            Video =
            {
                BackColor = Colors.Black,
            },
            Audio =
            {
                Enabled = false,
            },
        };

        return new Player(config);
    }

    private async Task WaitForConnectionResultAsync(
        TaskCompletionSource<bool> tcs,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await tcs.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            SetTestResult(success ? Translations.ConnectionSuccessful : Translations.StreamOpenFailed);
        }
        catch (OperationCanceledException)
        {
            SetTestResult(Translations.ConnectionTimedOut);
        }
    }

    private bool CanTestConnection()
        => !string.IsNullOrWhiteSpace(Camera.Connection.IpAddress) && !IsTesting;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        // Copy edited values back to the original camera
        originalCamera.CopyFrom(Camera);

        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    private bool CanSave()
        => !string.IsNullOrWhiteSpace(Camera.Display.DisplayName) &&
           !string.IsNullOrWhiteSpace(Camera.Connection.IpAddress) &&
           IsIpAddressUnique() &&
           HasChanges();

    private bool HasChanges()
    {
        // For new cameras, always consider as having changes if fields are filled
        if (isNew)
        {
            return true;
        }

        // For existing cameras, compare with original to detect changes
        return !Camera.ValueEquals(originalCamera);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));
    }
}