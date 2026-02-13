namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side <see cref="IMediaPipeline"/> implementation using FFmpeg subprocess.
/// Captures RTSP streams for recording and frame capture without WPF/FlyleafLib dependencies.
/// </summary>
public sealed class FFmpegMediaPipeline : IMediaPipeline
{
    private readonly ILogger<FFmpegMediaPipeline> logger;
    private readonly VideoTranscodeCodec transcodeCodec;
    private Uri? streamUri;
    private StreamSettings? settings;
    private Process? recordProcess;
    private bool disposed;
    private long framesDecoded;

    public FFmpegMediaPipeline(
        ILogger<FFmpegMediaPipeline> logger,
        VideoTranscodeCodec transcodeCodec)
    {
        this.logger = logger;
        this.transcodeCodec = transcodeCodec;
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
        var (format, codecArgs) = BuildOutputArgs(outputFilePath);
        var args = $"-rtsp_transport {transport} -i \"{streamUri}\" {codecArgs} -f {format} -y \"{outputFilePath}\"";

        recordProcess = StartFFmpegProcess(args);
        BeginReadingStderr(recordProcess);

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
            BeginReadingStderr(process);

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

    /// <summary>
    /// Asynchronously drains stderr from the FFmpeg process to prevent pipe
    /// buffer deadlocks, and logs each line at Information level.
    /// </summary>
    private void BeginReadingStderr(Process process)
    {
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                logger.LogInformation("FFmpeg: {Line}", e.Data);
            }
        };

        process.BeginErrorReadLine();
    }

    /// <summary>
    /// Returns the FFmpeg format name and codec/mux arguments appropriate for
    /// the output file extension and transcode setting.
    /// </summary>
    private (string Format, string CodecArgs) BuildOutputArgs(
        string outputFilePath)
    {
        var ext = Path.GetExtension(outputFilePath).ToUpperInvariant();
        var videoCodec = transcodeCodec switch
        {
            VideoTranscodeCodec.H264 => "-c:v libx264 -preset ultrafast -tune zerolatency",
            _ => "-c:v copy",
        };

        return ext switch
        {
            // MKV (Matroska) supports virtually all codecs including pcm_mulaw,
            // so we can copy audio streams without transcoding. EBML is also
            // inherently resilient to truncation — most of the file remains
            // playable even after an unclean shutdown.
            ".MKV" => ("matroska", $"{videoCodec} -c:a copy"),

            // MP4 does not support pcm_mulaw/pcm_alaw, so transcode audio to AAC.
            // frag_keyframe writes self-contained fragments on each keyframe so
            // the file is playable even if FFmpeg is killed without 'q'.
            _ => ("mp4", $"{videoCodec} -c:a aac -movflags frag_keyframe"),
        };
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