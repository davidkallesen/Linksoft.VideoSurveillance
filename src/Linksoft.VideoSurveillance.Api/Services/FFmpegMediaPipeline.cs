namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side <see cref="IMediaPipeline"/> implementation using FFmpeg subprocess.
/// Captures RTSP streams for recording and frame capture without WPF/FlyleafLib dependencies.
/// </summary>
public sealed class FFmpegMediaPipeline : IMediaPipeline
{
    private readonly ILogger<FFmpegMediaPipeline> logger;
    private Uri? streamUri;
    private StreamSettings? settings;
    private Process? recordProcess;
    private bool disposed;
    private long framesDecoded;

    public FFmpegMediaPipeline(ILogger<FFmpegMediaPipeline> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public bool IsRecordingActive => recordProcess is { HasExited: false };

    /// <inheritdoc />
    public double CurrentFps => 0;

    /// <inheritdoc />
    public long FramesDecoded => Interlocked.Read(ref framesDecoded);

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc />
    public void Open(
        Uri streamUri,
        StreamSettings settings)
    {
        ArgumentNullException.ThrowIfNull(streamUri);
        ArgumentNullException.ThrowIfNull(settings);

        this.streamUri = streamUri;
        this.settings = settings;

        RaiseConnectionStateChanged(
            ConnectionState.Disconnected,
            ConnectionState.Connected);

        logger.LogInformation(
            "FFmpeg pipeline opened for {StreamUri} (transport={Transport})",
            streamUri,
            settings.RtspTransport);
    }

    /// <inheritdoc />
    public void Close()
    {
        StopRecording();

        var previousState = streamUri is not null
            ? ConnectionState.Connected
            : ConnectionState.Disconnected;

        streamUri = null;
        settings = null;

        if (previousState == ConnectionState.Connected)
        {
            RaiseConnectionStateChanged(
                previousState,
                ConnectionState.Disconnected);
        }

        logger.LogInformation("FFmpeg pipeline closed");
    }

    /// <inheritdoc />
    public void StartRecording(string outputFilePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputFilePath);

        if (streamUri is null || settings is null)
        {
            throw new InvalidOperationException("Pipeline must be opened before starting recording.");
        }

        if (IsRecordingActive)
        {
            logger.LogWarning("Recording is already active, ignoring StartRecording call");
            return;
        }

        var transport = settings.RtspTransport ?? "tcp";
        var args = $"-rtsp_transport {transport} -i \"{streamUri}\" -c copy -f mp4 -y \"{outputFilePath}\"";

        recordProcess = StartFFmpegProcess(args);

        logger.LogInformation(
            "FFmpeg recording started: {OutputFile}",
            outputFilePath);
    }

    /// <inheritdoc />
    public void StopRecording()
    {
        if (recordProcess is null or { HasExited: true })
        {
            recordProcess = null;
            return;
        }

        try
        {
            // Send 'q' to FFmpeg stdin to gracefully stop recording
            recordProcess.StandardInput.Write('q');
            recordProcess.StandardInput.Flush();

            if (!recordProcess.WaitForExit(5000))
            {
                recordProcess.Kill(entireProcessTree: true);
                logger.LogWarning("FFmpeg recording process did not exit gracefully, killed");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error stopping FFmpeg recording process");
            try
            {
                recordProcess.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
        }
        finally
        {
            recordProcess.Dispose();
            recordProcess = null;
        }

        logger.LogInformation("FFmpeg recording stopped");
    }

    /// <inheritdoc />
    public Task<byte[]?> CaptureFrameAsync(CancellationToken ct = default)
    {
        if (streamUri is null || settings is null)
        {
            return Task.FromResult<byte[]?>(null);
        }

        return CaptureFrameCoreAsync(ct);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Close();
    }

    private async Task<byte[]?> CaptureFrameCoreAsync(CancellationToken ct)
    {
        var tempFile = Path.Combine(
            Path.GetTempPath(),
            $"frame_{Guid.NewGuid():N}.png");

        var transport = settings!.RtspTransport ?? "tcp";
        var args = $"-rtsp_transport {transport} -i \"{streamUri}\" -frames:v 1 -q:v 2 -y \"{tempFile}\"";

        try
        {
            using var process = StartFFmpegProcess(args);

            await process
                .WaitForExitAsync(ct)
                .ConfigureAwait(false);

            if (process.ExitCode != 0 || !File.Exists(tempFile))
            {
                logger.LogWarning("FFmpeg frame capture failed with exit code {ExitCode}", process.ExitCode);
                return null;
            }

            Interlocked.Increment(ref framesDecoded);

            return await File.ReadAllBytesAsync(tempFile, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to capture frame via FFmpeg");
            return null;
        }
        finally
        {
            try
            {
                File.Delete(tempFile);
            }
            catch (IOException)
            {
                // Best effort cleanup
            }
        }
    }

    private static Process StartFFmpegProcess(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        return Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start FFmpeg process. Ensure FFmpeg is installed and on the PATH.");
    }

    private void RaiseConnectionStateChanged(
        ConnectionState previousState,
        ConnectionState newState)
    {
        ConnectionStateChanged?.Invoke(
            this,
            new ConnectionStateChangedEventArgs(previousState, newState));
    }
}