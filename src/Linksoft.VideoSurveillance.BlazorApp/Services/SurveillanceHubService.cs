#pragma warning disable CA1034 // Do not nest type
#pragma warning disable CA1054 // URI parameters should not be strings
#pragma warning disable CA1056 // URI properties should not be strings

namespace Linksoft.VideoSurveillance.BlazorApp.Services;

/// <summary>
/// Service for connecting to the SignalR surveillance hub.
/// Provides real-time camera events: connection state, recording, motion, and streaming.
/// </summary>
#pragma warning disable CA1003 // Use generic event handler instances
#pragma warning disable MA0046 // The delegate must have 2 parameters
public sealed class SurveillanceHubService : IAsyncDisposable
{
    private HubConnection? hubConnection;

    public event Action<ConnectionStateEvent>? OnConnectionStateChanged;

    public event Action<RecordingStateEvent>? OnRecordingStateChanged;

    public event Action<MotionDetectedEvent>? OnMotionDetected;

    public event Action<StreamStartedEvent>? OnStreamStarted;

    public event Action<string>? OnHubConnectionStateChanged;

    public bool IsConnected
        => hubConnection?.State == HubConnectionState.Connected;

    public string ConnectionState
        => hubConnection?.State.ToString() ?? "Disconnected";

    public string ApiBaseUrl { get; }

    public SurveillanceHubService(string apiBaseUrl)
    {
        ApiBaseUrl = apiBaseUrl;
    }

    public async Task ConnectAsync()
    {
        if (hubConnection is not null)
        {
            return;
        }

        var hubUrl = $"{ApiBaseUrl}/hubs/surveillance";

        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<ConnectionStateEvent>("ConnectionStateChanged", e =>
        {
            OnConnectionStateChanged?.Invoke(e);
        });

        hubConnection.On<RecordingStateEvent>("RecordingStateChanged", e =>
        {
            OnRecordingStateChanged?.Invoke(e);
        });

        hubConnection.On<MotionDetectedEvent>("MotionDetected", e =>
        {
            OnMotionDetected?.Invoke(e);
        });

        hubConnection.On<StreamStartedEvent>("StreamStarted", e =>
        {
            OnStreamStarted?.Invoke(e);
        });

        hubConnection.Reconnecting += _ =>
        {
            OnHubConnectionStateChanged?.Invoke("Reconnecting...");
            return Task.CompletedTask;
        };

        hubConnection.Reconnected += _ =>
        {
            OnHubConnectionStateChanged?.Invoke("Connected");
            return Task.CompletedTask;
        };

        hubConnection.Closed += _ =>
        {
            OnHubConnectionStateChanged?.Invoke("Disconnected");
            return Task.CompletedTask;
        };

        try
        {
            await hubConnection.StartAsync().ConfigureAwait(false);
            OnHubConnectionStateChanged?.Invoke("Connected");
        }
        catch (HttpRequestException)
        {
            OnHubConnectionStateChanged?.Invoke("Disconnected");
        }
    }

    public async Task StartRecordingAsync(Guid cameraId)
    {
        if (hubConnection?.State == HubConnectionState.Connected)
        {
            await hubConnection
                .InvokeAsync("StartRecording", cameraId)
                .ConfigureAwait(false);
        }
    }

    public async Task StopRecordingAsync(Guid cameraId)
    {
        if (hubConnection?.State == HubConnectionState.Connected)
        {
            await hubConnection
                .InvokeAsync("StopRecording", cameraId)
                .ConfigureAwait(false);
        }
    }

    public async Task StartStreamAsync(Guid cameraId)
    {
        if (hubConnection?.State == HubConnectionState.Connected)
        {
            await hubConnection
                .InvokeAsync("StartStream", cameraId)
                .ConfigureAwait(false);
        }
    }

    public async Task StopStreamAsync(Guid cameraId)
    {
        if (hubConnection?.State == HubConnectionState.Connected)
        {
            await hubConnection
                .InvokeAsync("StopStream", cameraId)
                .ConfigureAwait(false);
        }
    }

    public async Task DisconnectAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.StopAsync().ConfigureAwait(false);
            await hubConnection.DisposeAsync().ConfigureAwait(false);
            hubConnection = null;
            OnHubConnectionStateChanged?.Invoke("Disconnected");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }

    public sealed record ConnectionStateEvent(Guid CameraId, string NewState, DateTimeOffset Timestamp);

    public sealed record RecordingStateEvent(Guid CameraId, string NewState, string OldState, string? FilePath);

    public sealed record MotionDetectedEvent(Guid CameraId, bool IsMotionActive);

    public sealed record StreamStartedEvent(Guid CameraId, string PlaylistUrl);
}
#pragma warning restore MA0046
#pragma warning restore CA1003