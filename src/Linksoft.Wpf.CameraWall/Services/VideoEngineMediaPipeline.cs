namespace Linksoft.Wpf.CameraWall.Services;

using CoreConnectionState = Linksoft.VideoSurveillance.Enums.ConnectionState;
using CoreConnectionStateChangedEventArgs = Linksoft.VideoSurveillance.Events.ConnectionStateChangedEventArgs;
using CoreStreamSettings = Linksoft.VideoSurveillance.Models.Settings.StreamSettings;

/// <summary>
/// VideoEngine-based implementation of <see cref="IMediaPipeline"/>.
/// Wraps an <see cref="IVideoPlayer"/> to provide recording
/// and frame capture for platform-agnostic services.
/// </summary>
public sealed class VideoEngineMediaPipeline : IMediaPipeline
{
    private readonly IVideoPlayer player;
    private CoreConnectionState currentState = CoreConnectionState.Disconnected;
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
    public event EventHandler<CoreConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc />
    public bool IsRecordingActive => player.IsRecording;

    /// <inheritdoc />
    public double CurrentFps => player.CurrentFps;

    /// <inheritdoc />
    public long FramesDecoded => player.FramesDecoded;

    /// <inheritdoc />
    public void Open(
        Uri streamUri,
        CoreStreamSettings settings)
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

        // Note: Player lifecycle is managed by CameraTile, not by this wrapper
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
            new CoreConnectionStateChangedEventArgs(previousState, newState));
    }

    private static CoreConnectionState MapPlayerState(PlayerState state)
        => state switch
        {
            PlayerState.Playing => CoreConnectionState.Connected,
            PlayerState.Opening => CoreConnectionState.Connecting,
            PlayerState.Stopped => CoreConnectionState.Disconnected,
            PlayerState.Error => CoreConnectionState.Error,
            _ => CoreConnectionState.Disconnected,
        };
}