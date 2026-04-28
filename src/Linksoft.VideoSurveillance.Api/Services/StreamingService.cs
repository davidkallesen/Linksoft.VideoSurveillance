namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Manages per-camera FFmpeg transcoding processes that convert RTSP streams
/// to HLS segments for browser consumption.
/// </summary>
public sealed partial class StreamingService : IDisposable
{
    // A client that drops its socket without calling StopStream leaves the
    // viewer count > 0 and the FFmpeg transcoder running. The reaper closes
    // sessions whose last activity is older than this threshold.
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ReaperInterval = TimeSpan.FromSeconds(30);

    private readonly ICameraStorageService storage;
    private readonly ILogger<StreamingService> logger;
    private readonly ConcurrentDictionary<Guid, StreamSession> sessions = new();
    private readonly string hlsOutputRoot;
    private readonly Timer reaperTimer;
    private bool disposed;

    public StreamingService(
        ICameraStorageService storage,
        ILogger<StreamingService> logger)
    {
        this.storage = storage;
        this.logger = logger;

        hlsOutputRoot = Path.Combine(Path.GetTempPath(), "linksoft-hls");
        Directory.CreateDirectory(hlsOutputRoot);

        reaperTimer = new Timer(ReapIdleSessions, state: null, ReaperInterval, ReaperInterval);
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
        session.TouchActivity();

        return session.PlaylistPath;
    }

    /// <summary>
    /// Refreshes the last-activity timestamp for a stream so it isn't reaped
    /// for inactivity. Clients should call this periodically (e.g. once per
    /// HLS playlist poll) so a dropped connection eventually triggers reap.
    /// </summary>
    public void Heartbeat(Guid cameraId)
    {
        if (sessions.TryGetValue(cameraId, out var session))
        {
            session.TouchActivity();
        }
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
            LogHlsStreamStopped(cameraId);
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

        reaperTimer.Dispose();

        foreach (var session in sessions.Values)
        {
            session.Dispose();
        }

        sessions.Clear();
    }

    private void ReapIdleSessions(object? state)
    {
        if (disposed)
        {
            return;
        }

        var cutoff = DateTime.UtcNow - InactivityTimeout;
        foreach (var (cameraId, session) in sessions)
        {
            if (session.LastActivityUtc < cutoff)
            {
                ReapSession(cameraId);
            }
        }
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "removed is unconditionally disposed inside the try block.")]
    private void ReapSession(Guid cameraId)
    {
        if (!sessions.TryRemove(cameraId, out var removed))
        {
            return;
        }

        try
        {
            removed.Dispose();
            LogHlsStreamReaped(cameraId);
        }
        catch (Exception ex)
        {
            LogHlsStreamReapFailed(ex, cameraId);
        }
    }

    private StreamSession CreateSession(Guid cameraId)
    {
        var camera = storage.GetCameraById(cameraId)
            ?? throw new InvalidOperationException($"Camera {cameraId} not found.");

        var outputDir = Path.Combine(hlsOutputRoot, cameraId.ToString("N"));

        // Clean stale files from previous sessions (e.g. after Aspire restart)
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, recursive: true);
        }

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
            "-hls_time 2",
            "-hls_list_size 5",
            "-hls_flags delete_segments",
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

        LogStartingFfmpeg(cameraId, args);

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg HLS process.");

        var session = new StreamSession(process, playlistPath, outputDir);

        // Log stderr asynchronously for diagnostics
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                session.AddErrorLine(e.Data);
                LogFfmpegOutput(cameraId, e.Data);
            }
        };
        process.BeginErrorReadLine();

        LogHlsStreamStarted(cameraId, playlistPath);

        return session;
    }

    private sealed class StreamSession : IDisposable
    {
        private readonly Process process;
        private readonly string outputDir;
        private readonly List<string> errorLines = [];
        private int viewerCount;
        private long lastActivityUtcTicks;

        public StreamSession(
            Process process,
            string playlistPath,
            string outputDir)
        {
            this.process = process;
            this.outputDir = outputDir;
            PlaylistPath = playlistPath;
            lastActivityUtcTicks = DateTime.UtcNow.Ticks;
        }

        public string PlaylistPath { get; }

        public int ViewerCount => Volatile.Read(ref viewerCount);

        public bool HasExited => process.HasExited;

        public int ExitCode => process.HasExited ? process.ExitCode : -1;

        public DateTime LastActivityUtc
            => new(Interlocked.Read(ref lastActivityUtcTicks), DateTimeKind.Utc);

        public void TouchActivity()
            => Interlocked.Exchange(ref lastActivityUtcTicks, DateTime.UtcNow.Ticks);

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