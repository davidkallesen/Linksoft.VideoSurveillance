// ReSharper disable InvertIf
namespace Linksoft.Wpf.CameraWall.Dialogs;

public partial class CameraConfigurationDialogViewModel : ViewModelDialogBase
{
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
        Camera = camera ?? throw new ArgumentNullException(nameof(camera));
        this.isNew = isNew;
        this.existingIpAddresses = existingIpAddresses ?? [];

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
        get => Camera.Protocol.ToString();
        set
        {
            if (Enum.TryParse<CameraProtocol>(value, out var protocol))
            {
                Camera.Protocol = protocol;
                RaisePropertyChanged();
            }
        }
    }

    public IDictionary<string, string> OverlayPositionItems { get; } = Enum
        .GetValues<OverlayPosition>()
        .ToDictionary(p => p.ToString(), p => p.ToString(), StringComparer.Ordinal);

    public string SelectedOverlayPositionKey
    {
        get => Camera.OverlayPosition.ToString();
        set
        {
            if (Enum.TryParse<OverlayPosition>(value, out var position))
            {
                Camera.OverlayPosition = position;
                RaisePropertyChanged();
            }
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
        Camera.IpAddress = e.SelectedHost.IpAddress.ToString();

        // Set display name from hostname if available and display name is default
        if (!string.IsNullOrWhiteSpace(e.SelectedHost.Hostname) &&
            (string.IsNullOrWhiteSpace(Camera.DisplayName) || Camera.DisplayName == Translations.NewCamera))
        {
            Camera.DisplayName = e.SelectedHost.Hostname;
        }

        // Auto-detect protocol and port from open ports
        var openPorts = e.SelectedHost.OpenPortNumbers.ToList();
        if (openPorts.Contains(554))
        {
            Camera.Protocol = CameraProtocol.Rtsp;
            Camera.Port = 554;
        }
        else if (openPorts.Contains(8554))
        {
            Camera.Protocol = CameraProtocol.Rtsp;
            Camera.Port = 8554;
        }
        else if (openPorts.Contains(80))
        {
            Camera.Protocol = CameraProtocol.Http;
            Camera.Port = 80;
        }
        else if (openPorts.Contains(8080))
        {
            Camera.Protocol = CameraProtocol.Http;
            Camera.Port = 8080;
        }
        else if (openPorts.Count > 0)
        {
            // Use first open port
            Camera.Port = openPorts[0];
        }

        // Notify property changes for combo boxes
        OnPropertyChanged(nameof(SelectedProtocolKey));
    }

    private void OnCameraPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        // Reset test result when connection-related properties change
        if (e.PropertyName is nameof(Camera.Protocol)
            or nameof(Camera.IpAddress)
            or nameof(Camera.Port)
            or nameof(Camera.Path)
            or nameof(Camera.UserName)
            or nameof(Camera.Password))
        {
            ClearTestResult();
        }

        // Validate IP address uniqueness when IP changes
        if (e.PropertyName == nameof(Camera.IpAddress))
        {
            ValidateIpAddress();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void ValidateIpAddress()
    {
        if (string.IsNullOrWhiteSpace(Camera.IpAddress))
        {
            IpAddressError = null;
            return;
        }

        var isDuplicate = existingIpAddresses.Contains(Camera.IpAddress, StringComparer.OrdinalIgnoreCase);
        IpAddressError = isDuplicate ? Translations.IpAddressAlreadyExists : null;
    }

    private bool IsIpAddressUnique()
    {
        if (string.IsNullOrWhiteSpace(Camera.IpAddress))
        {
            return true;
        }

        return !existingIpAddresses.Contains(Camera.IpAddress, StringComparer.OrdinalIgnoreCase);
    }

    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnection()
    {
        IsTesting = true;
        ClearTestResult();

        try
        {
            if (Camera.Protocol == CameraProtocol.Rtsp)
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();

                await tcpClient
                    .ConnectAsync(Camera.IpAddress, Camera.Port)
                    .ConfigureAwait(false);

                SetTestResult(Translations.ConnectionSuccessful);
            }
            else
            {
                var uri = Camera.BuildUri();

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);

                var response = await httpClient
                    .GetAsync(uri)
                    .ConfigureAwait(false);

                SetTestResult(response.IsSuccessStatusCode
                    ? Translations.ConnectionSuccessful
                    : string.Format(CultureInfo.CurrentCulture, Translations.FailedWithStatus1, response.StatusCode));
            }
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

    private bool CanTestConnection()
        => !string.IsNullOrWhiteSpace(Camera.IpAddress) && !IsTesting;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: true));
    }

    private bool CanSave()
        => !string.IsNullOrWhiteSpace(Camera.DisplayName) &&
           !string.IsNullOrWhiteSpace(Camera.IpAddress) &&
           IsIpAddressUnique();

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, new DialogClosedEventArgs(dialogResult: false));
    }
}