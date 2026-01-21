// ReSharper disable InvertIf
namespace Linksoft.Wpf.CameraWall.Dialogs;

[SuppressMessage("", "S2325:Make properties static", Justification = "XAML binding requires instance properties")]
public partial class CameraConfigurationDialogViewModel : ViewModelDialogBase
{
    private readonly CameraConfiguration originalCamera;
    private readonly bool isNew;
    private readonly IReadOnlyCollection<string> existingIpAddresses;
    private readonly IApplicationSettingsService settingsService;

    [ObservableProperty(AfterChangedCallback = nameof(OnIsTestingChanged))]
    private bool isTesting;

    private string? testResultInternal;

    public string TestResult => testResultInternal ?? Translations.NotTested;

    private void ClearTestResult()
    {
        if (testResultInternal is not null)
        {
            testResultInternal = null;
            OnPropertyChanged(nameof(TestResult));
        }
    }

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
        IReadOnlyCollection<string> existingIpAddresses,
        IApplicationSettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(settingsService);

        originalCamera = camera;
        this.isNew = isNew;
        this.existingIpAddresses = existingIpAddresses ?? [];
        this.settingsService = settingsService;

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

    /// <summary>
    /// Gets a value indicating whether this is a new camera being added.
    /// </summary>
    public bool IsNew => isNew;

    /// <summary>
    /// Gets a value indicating whether an existing camera is being edited.
    /// When editing, connection settings (IP, port, protocol) are read-only.
    /// </summary>
    public bool IsEditing => !isNew;

    /// <summary>
    /// Gets a value indicating whether connection settings can be modified.
    /// Connection settings are only editable for new cameras.
    /// </summary>
    public bool CanEditConnectionSettings => isNew;

    public IDictionary<string, string> ProtocolItems { get; } = Enum<CameraProtocol>.ToDictionaryWithStringKey();

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

    public IDictionary<string, string> OverlayPositionItems
        => DropDownItemsFactory.OverlayPositionItems;

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

    public IDictionary<string, string> RtspTransportItems
        => DropDownItemsFactory.RtspTransportItems;

    public IDictionary<string, string> VideoQualityItems
        => DropDownItemsFactory.VideoQualityItems;

    public IDictionary<string, string> RecordingFormatItems
        => DropDownItemsFactory.RecordingFormatItems;

    public IDictionary<string, string> OpacityItems
        => DropDownItemsFactory.OverlayOpacityItems;

    public string SelectedRtspTransportKey
    {
        get => Camera.Stream.RtspTransport;
        set
        {
            Camera.Stream.RtspTransport = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayTitle
    {
        get => Camera.Overrides?.ShowOverlayTitle is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.ShowOverlayTitle = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.ShowOverlayTitle = settingsService.CameraDisplay.ShowOverlayTitle;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayTitle));
        }
    }

    public bool OverrideShowOverlayTitle
    {
        get => Camera.Overrides?.ShowOverlayTitle ?? settingsService.CameraDisplay.ShowOverlayTitle;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.ShowOverlayTitle = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayDescription
    {
        get => Camera.Overrides?.ShowOverlayDescription is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.ShowOverlayDescription = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.ShowOverlayDescription = settingsService.CameraDisplay.ShowOverlayDescription;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayDescription));
        }
    }

    public bool OverrideShowOverlayDescription
    {
        get => Camera.Overrides?.ShowOverlayDescription ?? settingsService.CameraDisplay.ShowOverlayDescription;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.ShowOverlayDescription = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayTime
    {
        get => Camera.Overrides?.ShowOverlayTime is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.ShowOverlayTime = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.ShowOverlayTime = settingsService.CameraDisplay.ShowOverlayTime;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayTime));
        }
    }

    public bool OverrideShowOverlayTime
    {
        get => Camera.Overrides?.ShowOverlayTime ?? settingsService.CameraDisplay.ShowOverlayTime;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.ShowOverlayTime = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultShowOverlayConnectionStatus
    {
        get => Camera.Overrides?.ShowOverlayConnectionStatus is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.ShowOverlayConnectionStatus = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.ShowOverlayConnectionStatus = settingsService.CameraDisplay.ShowOverlayConnectionStatus;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideShowOverlayConnectionStatus));
        }
    }

    public bool OverrideShowOverlayConnectionStatus
    {
        get => Camera.Overrides?.ShowOverlayConnectionStatus ?? settingsService.CameraDisplay.ShowOverlayConnectionStatus;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.ShowOverlayConnectionStatus = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultOverlayOpacity
    {
        get => Camera.Overrides?.OverlayOpacity is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.OverlayOpacity = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.OverlayOpacity = settingsService.CameraDisplay.OverlayOpacity;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideOverlayOpacity));
        }
    }

    public string OverrideOverlayOpacity
    {
        get
        {
            var opacity = Camera.Overrides?.OverlayOpacity ?? settingsService.CameraDisplay.OverlayOpacity;
            return opacity.ToString("F1", CultureInfo.InvariantCulture);
        }

        set
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity))
            {
                EnsureOverrides();
                Camera.Overrides!.OverlayOpacity = opacity;
                RaisePropertyChanged();
            }
        }
    }

    public bool UseDefaultConnectionTimeout
    {
        get => Camera.Overrides?.ConnectionTimeoutSeconds is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.ConnectionTimeoutSeconds = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.ConnectionTimeoutSeconds = settingsService.Connection.ConnectionTimeoutSeconds;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideConnectionTimeout));
        }
    }

    public int OverrideConnectionTimeout
    {
        get => Camera.Overrides?.ConnectionTimeoutSeconds ?? settingsService.Connection.ConnectionTimeoutSeconds;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.ConnectionTimeoutSeconds = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultReconnectDelay
    {
        get => Camera.Overrides?.ReconnectDelaySeconds is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.ReconnectDelaySeconds = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.ReconnectDelaySeconds = settingsService.Connection.ReconnectDelaySeconds;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideReconnectDelay));
        }
    }

    public int OverrideReconnectDelay
    {
        get => Camera.Overrides?.ReconnectDelaySeconds ?? settingsService.Connection.ReconnectDelaySeconds;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.ReconnectDelaySeconds = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultMaxReconnectAttempts
    {
        get => Camera.Overrides?.MaxReconnectAttempts is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.MaxReconnectAttempts = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.MaxReconnectAttempts = settingsService.Connection.MaxReconnectAttempts;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideMaxReconnectAttempts));
        }
    }

    public int OverrideMaxReconnectAttempts
    {
        get => Camera.Overrides?.MaxReconnectAttempts ?? settingsService.Connection.MaxReconnectAttempts;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.MaxReconnectAttempts = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultAutoReconnect
    {
        get => Camera.Overrides?.AutoReconnectOnFailure is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.AutoReconnectOnFailure = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.AutoReconnectOnFailure = settingsService.Connection.AutoReconnectOnFailure;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideAutoReconnect));
        }
    }

    public bool OverrideAutoReconnect
    {
        get => Camera.Overrides?.AutoReconnectOnFailure ?? settingsService.Connection.AutoReconnectOnFailure;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.AutoReconnectOnFailure = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultVideoQuality
    {
        get => Camera.Overrides?.VideoQuality is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.VideoQuality = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.VideoQuality = settingsService.Performance.VideoQuality;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideVideoQuality));
        }
    }

    public string OverrideVideoQuality
    {
        get => Camera.Overrides?.VideoQuality ?? settingsService.Performance.VideoQuality;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.VideoQuality = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultHardwareAcceleration
    {
        get => Camera.Overrides?.HardwareAcceleration is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.HardwareAcceleration = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.HardwareAcceleration = settingsService.Performance.HardwareAcceleration;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideHardwareAcceleration));
        }
    }

    public bool OverrideHardwareAcceleration
    {
        get => Camera.Overrides?.HardwareAcceleration ?? settingsService.Performance.HardwareAcceleration;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.HardwareAcceleration = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingPath
    {
        get => Camera.Overrides?.RecordingPath is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.RecordingPath = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.RecordingPath = settingsService.Recording.RecordingPath ?? string.Empty;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingPath));
        }
    }

    public string OverrideRecordingPath
    {
        get => Camera.Overrides?.RecordingPath ?? settingsService.Recording.RecordingPath ?? string.Empty;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.RecordingPath = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingFormat
    {
        get => Camera.Overrides?.RecordingFormat is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.RecordingFormat = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.RecordingFormat = settingsService.Recording.RecordingFormat;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingFormat));
        }
    }

    public string OverrideRecordingFormat
    {
        get => Camera.Overrides?.RecordingFormat ?? settingsService.Recording.RecordingFormat;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.RecordingFormat = value;
            RaisePropertyChanged();
        }
    }

    public bool UseDefaultRecordingOnMotion
    {
        get => Camera.Overrides?.EnableRecordingOnMotion is null;
        set
        {
            if (value)
            {
                if (Camera.Overrides is not null)
                {
                    Camera.Overrides.EnableRecordingOnMotion = null;
                }
            }
            else
            {
                EnsureOverrides();
                Camera.Overrides!.EnableRecordingOnMotion = settingsService.Recording.EnableRecordingOnMotion;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(OverrideRecordingOnMotion));
        }
    }

    public bool OverrideRecordingOnMotion
    {
        get => Camera.Overrides?.EnableRecordingOnMotion ?? settingsService.Recording.EnableRecordingOnMotion;
        set
        {
            EnsureOverrides();
            Camera.Overrides!.EnableRecordingOnMotion = value;
            RaisePropertyChanged();
        }
    }

    private void EnsureOverrides()
    {
        Camera.Overrides ??= new CameraOverrides();
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

    [RelayCommand(CanExecute = nameof(CanTestConnection))]
    private async Task TestConnection()
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