namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// JSON file-based implementation of <see cref="ICameraStorageService"/> for the server edition.
/// </summary>
public sealed partial class JsonCameraStorageService : ICameraStorageService
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
        if (!SafeJsonFile.TryWrite(storagePath, data, JsonOptions))
        {
            LogStorageSaveFailed(new IOException("Atomic save failed"), storagePath);
        }
    }

    /// <inheritdoc/>
    public void Load()
    {
        var loaded = SafeJsonFile.TryRead<CameraStorageData>(storagePath, JsonOptions);
        if (loaded is not null)
        {
            data = loaded;
            LogStorageLoaded(data.Cameras.Count, data.Layouts.Count, storagePath);
        }
        else
        {
            data = new CameraStorageData();
        }
    }
}