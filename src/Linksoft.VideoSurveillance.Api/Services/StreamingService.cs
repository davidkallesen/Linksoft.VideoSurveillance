namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Manages per-camera FFmpeg transcoding processes that convert RTSP streams
/// to HLS segments for browser consumption.
/// </summary>
public sealed class StreamingService : IDisposable
{
    private readonly ICameraStorageService storage;
    private readonly ILogger<StreamingService> logger;
    private readonly ConcurrentDictionary<Guid, StreamSession> sessions = new();
    private readonly string hlsOutputRoot;
    private bool disposed;

    public StreamingService(
        ICameraStorageService storage,
        ILogger<StreamingService> logger)
    {
        this.storage = storage;
        this.logger = logger;

        hlsOutputRoot = Path.Combine(Path.GetTempPath(), "linksoft-hls");
        Directory.CreateDirectory(hlsOutputRoot);
    }

    /// <summary>
    /// Gets the HLS output directory root path.
    /// </summary>
    public string HlsOutputRoot => hlsOutputRoot;

    /// <summary>
    /// Starts HLS streaming for a camera. Returns the playlist path.
    /// If already streaming, increments the viewer count and returns existing path.
    /// </summary>
    public string StartStream(Guid cameraId)
    {
        var session = sessions.GetOrAdd(cameraId, id => CreateSession(id));

        session.IncrementViewers();

        return session.PlaylistPath;
    }

    /// <summary>
    /// Decrements the viewer count for a camera stream.
    /// Stops the FFmpeg process when no viewers remain.
    /// </summary>
    public void StopStream(Guid cameraId)
    {
        if (!sessions.TryGetValue(cameraId, out var session))
        {
            return;
        }

        var remaining = session.DecrementViewers();

        if (remaining <= 0 && sessions.TryRemove(cameraId, out var removed))
        {
            removed.Dispose();
            logger.LogInformation(
                "HLS stream stopped for camera {CameraId} (no viewers)",
                cameraId);
        }
    }

    /// <summary>
    /// Gets whether a camera is currently streaming.
    /// </summary>
    public bool IsStreaming(Guid cameraId)
        => sessions.ContainsKey(cameraId);

    /// <summary>
    /// Gets the current viewer count for a camera stream.
    /// </summary>
    public int GetViewerCount(Guid cameraId)
        => sessions.TryGetValue(cameraId, out var session)
            ? session.ViewerCount
            : 0;

    /// <summary>
    /// Gets the HLS playlist path for a camera, or null if not streaming.
    /// </summary>
    public string? GetPlaylistPath(Guid cameraId)
        => sessions.TryGetValue(cameraId, out var session)
            ? session.PlaylistPath
            : null;

    /// <summary>
    /// Gets whether the FFmpeg process for a camera has exited (crashed or finished).
    /// </summary>
    public bool HasProcessExited(Guid cameraId)
        => sessions.TryGetValue(cameraId, out var session) && session.HasExited;

    /// <summary>
    /// Gets recent FFmpeg stderr output for a camera session.
    /// </summary>
    public string GetProcessError(Guid cameraId)
        => sessions.TryGetValue(cameraId, out var session)
            ? session.GetRecentErrors()
            : string.Empty;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var session in sessions.Values)
        {
            session.Dispose();
        }

        sessions.Clear();
    }

    private StreamSession CreateSession(Guid cameraId)
    {
        var camera = storage.GetCameraById(cameraId)
            ?? throw new InvalidOperationException($"Camera {cameraId} not found.");

        var outputDir = Path.Combine(hlsOutputRoot, cameraId.ToString("N"));
        Directory.CreateDirectory(outputDir);

        var playlistPath = Path.Combine(outputDir, "stream.m3u8");
        var streamUri = camera.BuildUri();
        var transport = camera.Stream.RtspTransport ?? "tcp";

        var args = string.Join(
            ' ',
            "-v info",
            "-fflags nobuffer",
            "-flags low_delay",
            "-probesize 512000",
            "-analyzeduration 500000",
            $"-rtsp_transport {transport}",
            "-timeout 10000000",
            $"-i \"{streamUri}\"",
            "-c:v libx264 -preset ultrafast -tune zerolatency -g 30",
            "-c:a aac -b:a 64k",
            "-f hls",
            "-hls_time 1",
            "-hls_list_size 3",
            "-hls_flags delete_segments+append_list",
            "-hls_init_time 0",
            $"\"{playlistPath}\"");

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
        };

        logger.LogInformation(
            "Starting FFmpeg for camera {CameraId}: ffmpeg {Args}",
            cameraId,
            args);

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg HLS process.");

        var session = new StreamSession(process, playlistPath, outputDir);

        // Log stderr asynchronously for diagnostics
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                session.AddErrorLine(e.Data);
                logger.LogInformation("[FFmpeg {CameraId}] {Line}", cameraId, e.Data);
            }
        };
        process.BeginErrorReadLine();

        logger.LogInformation(
            "HLS stream started for camera {CameraId} -> {PlaylistPath}",
            cameraId,
            playlistPath);

        return session;
    }

    private sealed class StreamSession : IDisposable
    {
        private readonly Process process;
        private readonly string outputDir;
        private readonly List<string> errorLines = [];
        private int viewerCount;

        public StreamSession(
            Process process,
            string playlistPath,
            string outputDir)
        {
            this.process = process;
            this.outputDir = outputDir;
            PlaylistPath = playlistPath;
        }

        public string PlaylistPath { get; }

        public int ViewerCount => Volatile.Read(ref viewerCount);

        public bool HasExited => process.HasExited;

        public int ExitCode => process.HasExited ? process.ExitCode : -1;

        public void AddErrorLine(string line) => errorLines.Add(line);

        public string GetRecentErrors()
        {
            lock (errorLines)
            {
                return string.Join(Environment.NewLine, errorLines.TakeLast(10));
            }
        }

        public void IncrementViewers()
            => Interlocked.Increment(ref viewerCount);

        public int DecrementViewers()
            => Interlocked.Decrement(ref viewerCount);

        public void Dispose()
        {
            try
            {
                if (!process.HasExited)
                {
                    process.StandardInput.Write('q');
                    process.StandardInput.Flush();

                    if (!process.WaitForExit(5000))
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
            finally
            {
                process.Dispose();
            }

            try
            {
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup
            }
        }
    }
}