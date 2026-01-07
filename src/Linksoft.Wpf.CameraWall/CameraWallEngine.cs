// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
namespace Linksoft.Wpf.CameraWall;

/// <summary>
/// Engine initialization for the CameraWall library.
/// Must be called before using any CameraWall controls.
/// </summary>
public static class CameraWallEngine
{
    private static readonly SemaphoreSlim InitializationLock = new(1, 1);
    private static bool isInitialized;

    /// <summary>
    /// Gets a value indicating whether the engine has been initialized.
    /// </summary>
    public static bool IsInitialized => isInitialized;

    /// <summary>
    /// Initializes the CameraWall engine with default settings.
    /// </summary>
    public static void Initialize()
    {
        Initialize(ffmpegPath: null);
    }

    /// <summary>
    /// Initializes the CameraWall engine with a custom FFmpeg path.
    /// </summary>
    /// <param name="ffmpegPath">The path to FFmpeg binaries. If null, auto-discovery is used.</param>
    public static void Initialize(string? ffmpegPath)
    {
        var acquiredLock = false;

        try
        {
            InitializationLock.Wait();
            acquiredLock = true;

            if (isInitialized)
            {
                return;
            }

            var engineConfig = new EngineConfig
            {
                FFmpegPath = ffmpegPath ?? DiscoverFFmpegPath(),
                UIRefresh = true,
                UIRefreshInterval = 100,
            };

            Engine.Start(engineConfig);
            isInitialized = true;
        }
        finally
        {
            if (acquiredLock)
            {
                InitializationLock.Release();
            }
        }
    }

    /// <summary>
    /// Discovers the FFmpeg path by checking common locations.
    /// </summary>
    /// <returns>The discovered FFmpeg path, or null if not found.</returns>
    private static string? DiscoverFFmpegPath()
    {
        // Check common FFmpeg locations
        var possiblePaths = new[]
        {
            // Application directory
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg"),

            // Program Files
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"),

            // Local AppData
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin"),

            // Common installations
            @"C:\ffmpeg\bin",
            @"C:\Program Files\ffmpeg\bin",
        };

        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path) && ContainsFFmpegBinaries(path))
            {
                return path;
            }
        }

        // Return null to let FlyleafLib handle discovery
        return null;
    }

    /// <summary>
    /// Checks if a directory contains FFmpeg binaries.
    /// </summary>
    /// <param name="path">The directory path to check.</param>
    /// <returns>True if FFmpeg binaries are found.</returns>
    private static bool ContainsFFmpegBinaries(string path)
    {
        try
        {
            var files = Directory.GetFiles(path, "avcodec*.dll");
            return files.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}