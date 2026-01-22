// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// JSON file-based implementation of <see cref="ICameraStorageService"/>.
/// </summary>
[Registration(Lifetime.Singleton)]
public class CameraStorageService : ICameraStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(),
            new CameraConfigurationJsonValueConverter(),
        },
    };

    private readonly string storagePath;
    private CameraStorageData data = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraStorageService"/> class.
    /// Uses the default storage location from <see cref="ApplicationPaths.DefaultCameraDataPath"/>.
    /// </summary>
    public CameraStorageService()
        : this(ApplicationPaths.DefaultCameraDataPath)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraStorageService"/> class
    /// with a custom storage path.
    /// </summary>
    /// <param name="storagePath">The path to the storage file.</param>
    public CameraStorageService(string storagePath)
    {
        this.storagePath = storagePath;
        Load();
    }

    /// <inheritdoc/>
    public Guid? StartupLayoutId
    {
        get => data.StartupLayoutId;
        set
        {
            data.StartupLayoutId = value;
            Save();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<CameraConfiguration> GetAllCameras()
        => data.Cameras.AsReadOnly();

    /// <inheritdoc/>
    public CameraConfiguration? GetCameraById(Guid id)
        => data.Cameras.Find(c => c.Id == id);

    /// <inheritdoc/>
    public void AddOrUpdateCamera(CameraConfiguration camera)
    {
        ArgumentNullException.ThrowIfNull(camera);

        var existingIndex = data.Cameras.FindIndex(c => c.Id == camera.Id);
        if (existingIndex >= 0)
        {
            data.Cameras[existingIndex] = camera;
        }
        else
        {
            data.Cameras.Add(camera);
        }

        Save();
    }

    /// <inheritdoc/>
    public bool DeleteCamera(Guid id)
    {
        var camera = data.Cameras.Find(c => c.Id == id);
        if (camera is null)
        {
            return false;
        }

        data.Cameras.Remove(camera);

        // Remove camera from all layouts
        foreach (var layout in data.Layouts)
        {
            layout.Items.RemoveAll(item => item.CameraId == id);
        }

        Save();
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<CameraLayout> GetAllLayouts()
        => data.Layouts.AsReadOnly();

    /// <inheritdoc/>
    public CameraLayout? GetLayoutById(Guid id)
        => data.Layouts.Find(l => l.Id == id);

    /// <inheritdoc/>
    public void AddOrUpdateLayout(CameraLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var existingIndex = data.Layouts.FindIndex(l => l.Id == layout.Id);
        if (existingIndex >= 0)
        {
            layout.ModifiedAt = DateTime.UtcNow;
            data.Layouts[existingIndex] = layout;
        }
        else
        {
            data.Layouts.Add(layout);
        }

        Save();
    }

    /// <inheritdoc/>
    public bool DeleteLayout(Guid id)
    {
        var layout = data.Layouts.Find(l => l.Id == id);
        if (layout is null)
        {
            return false;
        }

        data.Layouts.Remove(layout);

        // Clear startup layout if deleted
        if (data.StartupLayoutId == id)
        {
            data.StartupLayoutId = null;
        }

        Save();
        return true;
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

            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(storagePath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - storage failures shouldn't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to save camera storage: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Load()
    {
        try
        {
            if (!File.Exists(storagePath))
            {
                data = new CameraStorageData();
                return;
            }

            var json = File.ReadAllText(storagePath);
            data = JsonSerializer.Deserialize<CameraStorageData>(json, JsonOptions) ?? new CameraStorageData();
        }
        catch (Exception ex)
        {
            // Log error and start with empty data
            System.Diagnostics.Debug.WriteLine($"Failed to load camera storage: {ex.Message}");
            data = new CameraStorageData();
        }
    }
}