namespace Linksoft.Wpf.CameraWall.Services;

using CoreConnectionState = Linksoft.VideoSurveillance.Enums.ConnectionState;
using CoreConnectionStateChangedEventArgs = Linksoft.VideoSurveillance.Events.ConnectionStateChangedEventArgs;
using CoreStreamSettings = Linksoft.VideoSurveillance.Models.Settings.StreamSettings;

/// <summary>
/// FlyleafLib-based implementation of <see cref="IMediaPipeline"/>.
/// Wraps an existing FlyleafLib <see cref="Player"/> to provide recording
/// and frame capture for platform-agnostic services.
/// </summary>
public sealed class FlyleafLibMediaPipeline : IMediaPipeline
{
    private readonly Player player;
    private CoreConnectionState currentState = CoreConnectionState.Disconnected;
    private bool isRecording;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlyleafLibMediaPipeline"/> class.
    /// </summary>
    /// <param name="player">The FlyleafLib player to wrap.</param>
    public FlyleafLibMediaPipeline(Player player)
    {
        this.player = player ?? throw new ArgumentNullException(nameof(player));
        this.player.PropertyChanged += OnPlayerPropertyChanged;
    }

    /// <inheritdoc />
    public event EventHandler<CoreConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Gets the underlying FlyleafLib player for direct UI binding (e.g., FlyleafHost).
    /// </summary>
    public Player FlyleafPlayer => player;

    /// <inheritdoc />
    public bool IsRecordingActive => isRecording;

    /// <inheritdoc />
    public double CurrentFps => player.Video?.FPSCurrent ?? 0;

    /// <inheritdoc />
    public long FramesDecoded => player.Video?.FramesDisplayed ?? 0;

    /// <inheritdoc />
    public void Open(
        Uri streamUri,
        CoreStreamSettings settings)
    {
        ArgumentNullException.ThrowIfNull(streamUri);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.UseLowLatencyMode)
        {
            player.Config.Demuxer.BufferDuration = settings.BufferDurationMs * 10000L;
            player.Config.Demuxer.FormatOpt["rtsp_transport"] = settings.RtspTransport;
            player.Config.Demuxer.FormatOpt["fflags"] = "nobuffer";
        }

        player.Open(streamUri.ToString());
    }

    /// <inheritdoc />
    public void Close()
    {
        player.Stop();
    }

    /// <inheritdoc />
    public void StartRecording(string outputFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        var filePath = outputFilePath;
        player.StartRecording(ref filePath, useRecommendedExtension: false);
        isRecording = true;
    }

    /// <inheritdoc />
    public void StopRecording()
    {
        player.StopRecording();
        isRecording = false;
    }

    /// <inheritdoc />
    public async Task<byte[]?> CaptureFrameAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var tempFile = Path.Combine(Path.GetTempPath(), $"pipeline_{Guid.NewGuid():N}.png");
        try
        {
            player.TakeSnapshotToFile(tempFile);

            // Wait briefly for the file to be written (FlyleafLib writes async)
            var maxWaitMs = 500;
            var waitedMs = 0;
            while (waitedMs < maxWaitMs)
            {
                ct.ThrowIfCancellationRequested();

                if (File.Exists(tempFile))
                {
                    var fileInfo = new FileInfo(tempFile);
                    if (fileInfo.Length > 0)
                    {
                        break;
                    }
                }

                await Task.Delay(50, ct).ConfigureAwait(false);
                waitedMs += 50;
            }

            if (!File.Exists(tempFile))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(tempFile, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        player.PropertyChanged -= OnPlayerPropertyChanged;

        if (isRecording)
        {
            try
            {
                player.StopRecording();
            }
            catch
            {
                // Best effort during disposal
            }

            isRecording = false;
        }

        // Note: Player lifecycle is managed by CameraTile, not by this wrapper
        disposed = true;
    }

    private void OnPlayerPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Player.Status))
        {
            return;
        }

        var newState = MapFlyleafStatus(player.Status);
        if (newState == currentState)
        {
            return;
        }

        var previousState = currentState;
        currentState = newState;
        ConnectionStateChanged?.Invoke(
            this,
            new CoreConnectionStateChangedEventArgs(previousState, newState));
    }

    private static CoreConnectionState MapFlyleafStatus(Status status)
        => status switch
        {
            Status.Playing => CoreConnectionState.Connected,
            Status.Paused => CoreConnectionState.Connected,
            Status.Opening => CoreConnectionState.Connecting,
            Status.Stopped => CoreConnectionState.Disconnected,
            Status.Ended => CoreConnectionState.Disconnected,
            Status.Failed => CoreConnectionState.Error,
            _ => CoreConnectionState.Disconnected,
        };
}