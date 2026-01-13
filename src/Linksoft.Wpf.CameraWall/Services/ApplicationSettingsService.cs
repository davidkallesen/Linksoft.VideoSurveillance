// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// JSON file-based implementation of <see cref="IApplicationSettingsService"/>.
/// Settings are stored in %ProgramData%\Linksoft\CameraWall\settings.json.
/// </summary>
[Registration(Lifetime.Singleton)]
public class ApplicationSettingsService : IApplicationSettingsService
{
    /// <summary>
    /// The settings file name.
    /// </summary>
    public const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string storagePath;
    private ApplicationSettings settings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettingsService"/> class.
    /// Uses the default storage location: %ProgramData%\Linksoft\CameraWall\settings.json.
    /// </summary>
    public ApplicationSettingsService()
        : this(GetDefaultStoragePath())
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
    public GeneralSettings General => settings.General;

    /// <inheritdoc/>
    public DisplaySettings Display => settings.Display;

    /// <inheritdoc/>
    public void SaveGeneral(GeneralSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings.General = settings;
        Save();
    }

    /// <inheritdoc/>
    public void SaveDisplay(DisplaySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings.Display = settings;
        Save();
    }

    /// <inheritdoc/>
    public void Load()
    {
        try
        {
            if (!File.Exists(storagePath))
            {
                settings = new ApplicationSettings();
                return;
            }

            var json = File.ReadAllText(storagePath);
            settings = JsonSerializer.Deserialize<ApplicationSettings>(json, JsonOptions) ?? new ApplicationSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load application settings: {ex.Message}");
            settings = new ApplicationSettings();
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

            var json = JsonSerializer.Serialize(settings, JsonOptions);
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

    /// <summary>
    /// Gets the default storage path for application settings.
    /// </summary>
    /// <returns>The default storage file path in %ProgramData%\Linksoft\CameraWall.</returns>
    private static string GetDefaultStoragePath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Linksoft",
            "CameraWall",
            SettingsFileName);
}