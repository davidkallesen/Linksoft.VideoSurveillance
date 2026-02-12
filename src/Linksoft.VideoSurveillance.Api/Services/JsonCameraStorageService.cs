namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// JSON file-based implementation of <see cref="ICameraStorageService"/> for the server edition.
/// </summary>
public sealed class JsonCameraStorageService : ICameraStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string storagePath;
    private readonly ILogger<JsonCameraStorageService> logger;
    private CameraStorageData data = new();

    public JsonCameraStorageService(ILogger<JsonCameraStorageService> logger)
        : this(ApplicationPaths.DefaultCameraDataPath, logger)
    {
    }

    public JsonCameraStorageService(
        string storagePath,
        ILogger<JsonCameraStorageService> logger)
    {
        this.storagePath = storagePath;
        this.logger = logger;
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
            logger.LogWarning(ex, "Failed to save camera storage to {Path}", storagePath);
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

            logger.LogInformation(
                "Loaded {CameraCount} cameras and {LayoutCount} layouts from {Path}",
                data.Cameras.Count,
                data.Layouts.Count,
                storagePath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load camera storage from {Path}", storagePath);
            data = new CameraStorageData();
        }
    }
}