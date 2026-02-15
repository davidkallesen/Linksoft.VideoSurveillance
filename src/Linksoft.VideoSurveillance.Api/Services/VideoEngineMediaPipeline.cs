namespace Linksoft.VideoSurveillance.Api.Services;

/// <summary>
/// Server-side <see cref="IMediaPipeline"/> implementation using the in-process
/// <see cref="IVideoPlayer"/> from <c>Linksoft.VideoEngine</c>.
/// Replaces the previous FFmpeg subprocess approach for recording and snapshots.
/// </summary>
public sealed class VideoEngineMediaPipeline : IMediaPipeline
{
    private readonly IVideoPlayer player;
    private ConnectionState currentState = ConnectionState.Disconnected;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoEngineMediaPipeline"/> class.
    /// </summary>
    /// <param name="player">The video player to wrap.</param>
    public VideoEngineMediaPipeline(IVideoPlayer player)
    {
        this.player = player ?? throw new ArgumentNullException(nameof(player));
        this.player.StateChanged += OnPlayerStateChanged;
    }

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc />
    public bool IsRecordingActive => player.IsRecording;

    /// <inheritdoc />
    public double CurrentFps => player.CurrentFps;

    /// <inheritdoc />
    public long FramesDecoded => player.FramesDecoded;

    /// <inheritdoc />
    public void Open(
        Uri streamUri,
        StreamSettings settings)
    {
        ArgumentNullException.ThrowIfNull(streamUri);
        ArgumentNullException.ThrowIfNull(settings);

        var options = new StreamOptions
        {
            UseLowLatencyMode = settings.UseLowLatencyMode,
            MaxLatencyMs = settings.MaxLatencyMs,
            RtspTransport = settings.RtspTransport,
            BufferDurationMs = settings.BufferDurationMs,
        };

        player.Open(streamUri, options);
    }

    /// <inheritdoc />
    public void Close()
    {
        player.Close();
    }

    /// <inheritdoc />
    public void StartRecording(string outputFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);
        player.StartRecording(outputFilePath);
    }

    /// <inheritdoc />
    public void StopRecording()
    {
        player.StopRecording();
    }

    /// <inheritdoc />
    public Task<byte[]?> CaptureFrameAsync(CancellationToken ct = default)
        => player.CaptureFrameAsync(ct);

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        player.StateChanged -= OnPlayerStateChanged;

        if (player.IsRecording)
        {
            try
            {
                player.StopRecording();
            }
            catch
            {
                // Best effort during disposal
            }
        }

        player.Dispose();
        disposed = true;
    }

    private void OnPlayerStateChanged(
        object? sender,
        PlayerStateChangedEventArgs e)
    {
        var newState = MapPlayerState(e.NewState);
        if (newState == currentState)
        {
            return;
        }

        var previousState = currentState;
        currentState = newState;
        ConnectionStateChanged?.Invoke(
            this,
            new ConnectionStateChangedEventArgs(previousState, newState));
    }

    private static ConnectionState MapPlayerState(PlayerState state)
        => state switch
        {
            PlayerState.Playing => ConnectionState.Connected,
            PlayerState.Opening => ConnectionState.Connecting,
            PlayerState.Stopped => ConnectionState.Disconnected,
            PlayerState.Error => ConnectionState.Error,
            _ => ConnectionState.Disconnected,
        };
}