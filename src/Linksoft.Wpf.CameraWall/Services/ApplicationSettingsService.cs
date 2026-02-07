// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// JSON file-based implementation of <see cref="IApplicationSettingsService"/>.
/// Settings are stored at <see cref="ApplicationPaths.DefaultSettingsPath"/>.
/// </summary>
[Registration(Lifetime.Singleton)]
public class ApplicationSettingsService : IApplicationSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string storagePath;
    private ApplicationSettings appSettings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettingsService"/> class.
    /// Uses the default storage location from <see cref="ApplicationPaths.DefaultSettingsPath"/>.
    /// </summary>
    public ApplicationSettingsService()
        : this(ApplicationPaths.DefaultSettingsPath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettingsService"/> class
    /// with a custom storage path.
    /// </summary>
    /// <param name="storagePath">The path to the storage file.</param>
    public ApplicationSettingsService(string storagePath)
    {
        this.storagePath = storagePath;
        EnsureSettingsFileExists();
        Load();
    }

    /// <inheritdoc/>
    public GeneralSettings General => appSettings.General;

    /// <inheritdoc/>
    public CameraDisplayAppSettings CameraDisplay => appSettings.CameraDisplay;

    /// <inheritdoc/>
    public ConnectionAppSettings Connection => appSettings.Connection;

    /// <inheritdoc/>
    public PerformanceSettings Performance => appSettings.Performance;

    /// <inheritdoc/>
    public MotionDetectionSettings MotionDetection
        => appSettings.MotionDetection;

    /// <inheritdoc/>
    public RecordingSettings Recording => appSettings.Recording;

    /// <inheritdoc/>
    public AdvancedSettings Advanced => appSettings.Advanced;

    /// <inheritdoc/>
    public void SaveGeneral(GeneralSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.General = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SaveCameraDisplay(CameraDisplayAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.CameraDisplay = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SaveConnection(ConnectionAppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.Connection = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SavePerformance(PerformanceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.Performance = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SaveMotionDetection(MotionDetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.MotionDetection = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SaveRecording(RecordingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.Recording = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SaveAdvanced(AdvancedSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.appSettings.Advanced = settings;
        Save();
    }

    /// <inheritdoc/>
    public void ApplyDefaultsToCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        // Apply connection defaults
        camera.Connection.Protocol = Connection.DefaultProtocol;
        camera.Connection.Port = Connection.DefaultPort;

        // Apply display defaults
        camera.Display.OverlayPosition = CameraDisplay.OverlayPosition;

        // Apply performance/stream defaults
        camera.Stream.UseLowLatencyMode = Performance.LowLatencyMode;
        camera.Stream.MaxLatencyMs = Performance.MaxLatencyMs;
        camera.Stream.RtspTransport = Performance.RtspTransport;
        camera.Stream.BufferDurationMs = Performance.BufferDurationMs;
    }

    /// <inheritdoc/>
    public T GetEffectiveValue<T>(
        CameraConfiguration camera,
        T appDefault,
        Func<CameraOverrides?, T?> overrideSelector)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(overrideSelector);

        var overrideValue = overrideSelector(camera.Overrides);
        return overrideValue ?? appDefault;
    }

    /// <inheritdoc/>
    public string? GetEffectiveStringValue(
        CameraConfiguration camera,
        string? appDefault,
        Func<CameraOverrides?, string?> overrideSelector)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(overrideSelector);

        var overrideValue = overrideSelector(camera.Overrides);
        return overrideValue ?? appDefault;
    }

    /// <inheritdoc/>
    public void Load()
    {
        try
        {
            if (!File.Exists(storagePath))
            {
                appSettings = new ApplicationSettings();
                return;
            }

            var json = File.ReadAllText(storagePath);
            appSettings = JsonSerializer.Deserialize<ApplicationSettings>(json, JsonOptions) ?? new ApplicationSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load application settings: {ex.Message}");
            appSettings = new ApplicationSettings();
        }
    }

    /// <inheritdoc/>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(appSettings, JsonOptions);
            File.WriteAllText(storagePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save application settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures the settings file exists. If not, creates it with default values.
    /// </summary>
    private void EnsureSettingsFileExists()
    {
        if (File.Exists(storagePath))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create default settings file
            var defaultSettings = new ApplicationSettings();
            var json = JsonSerializer.Serialize(defaultSettings, JsonOptions);
            File.WriteAllText(storagePath, json);

            System.Diagnostics.Debug.WriteLine($"Created default settings file: {storagePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create default settings file: {ex.Message}");
        }
    }
}