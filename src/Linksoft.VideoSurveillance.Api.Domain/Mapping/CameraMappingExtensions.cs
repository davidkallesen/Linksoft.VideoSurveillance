using CoreCameraConfiguration = Linksoft.VideoSurveillance.Models.CameraConfiguration;
using CoreCameraProtocol = Linksoft.VideoSurveillance.Enums.CameraProtocol;
using CoreConnectionState = Linksoft.VideoSurveillance.Enums.ConnectionState;
using CoreOverlayPosition = Linksoft.VideoSurveillance.Enums.OverlayPosition;
using CoreRecordingState = Linksoft.VideoSurveillance.Enums.RecordingState;

namespace Linksoft.VideoSurveillance.Api.Domain.Mapping;

internal static class CameraMappingExtensions
{
    public static Camera ToApiModel(
        this CoreCameraConfiguration core,
        CoreConnectionState? connectionState = null,
        bool isRecording = false)
        => new(
            Id: core.Id,
            DisplayName: core.Display.DisplayName,
            Description: core.Display.Description ?? string.Empty,
            IpAddress: core.Connection.IpAddress,
            Port: core.Connection.Port,
            Protocol: core.Connection.Protocol.ToApiProtocol(),
            Path: core.Connection.Path ?? string.Empty,
            Username: core.Authentication.UserName ?? string.Empty,
            OverlayPosition: ToApiOverlayPosition(core.Display.OverlayPosition),
            StreamUseLowLatencyMode: core.Stream.UseLowLatencyMode,
            StreamMaxLatencyMs: core.Stream.MaxLatencyMs,
            StreamRtspTransport: ToApiRtspTransport(core.Stream.RtspTransport),
            StreamBufferDurationMs: core.Stream.BufferDurationMs,
            ConnectionState: connectionState?.ToApiConnectionState(),
            IsRecording: isRecording);

    public static CoreCameraConfiguration ToCoreModel(
        this CreateCameraRequest request)
    {
        var camera = new CoreCameraConfiguration
        {
            Connection =
            {
                IpAddress = request.IpAddress,
                Port = request.Port,
                Protocol = request.Protocol?.ToCoreProtocol() ?? CoreCameraProtocol.Rtsp,
                Path = request.Path,
            },
            Authentication =
            {
                UserName = request.Username,
                Password = request.Password,
            },
            Display =
            {
                DisplayName = request.DisplayName,
                Description = request.Description,
            },
        };

        if (request.OverlayPosition is not null)
        {
            camera.Display.OverlayPosition = ToCoreOverlayPosition(request.OverlayPosition.Value);
        }

        camera.Stream.UseLowLatencyMode = request.StreamUseLowLatencyMode;

        if (request.StreamMaxLatencyMs > 0)
        {
            camera.Stream.MaxLatencyMs = request.StreamMaxLatencyMs;
        }

        if (request.StreamRtspTransport is not null)
        {
            camera.Stream.RtspTransport = request.StreamRtspTransport.Value.ToString().ToLowerInvariant();
        }

        if (request.StreamBufferDurationMs > 0)
        {
            camera.Stream.BufferDurationMs = request.StreamBufferDurationMs;
        }

        return camera;
    }

    public static void ApplyUpdate(
        this CoreCameraConfiguration core,
        UpdateCameraRequest request)
    {
        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            core.Display.DisplayName = request.DisplayName;
        }

        if (request.Description is not null)
        {
            core.Display.Description = request.Description;
        }

        if (!string.IsNullOrEmpty(request.IpAddress))
        {
            core.Connection.IpAddress = request.IpAddress;
        }

        if (request.Port > 0)
        {
            core.Connection.Port = request.Port;
        }

        if (request.Protocol is not null)
        {
            core.Connection.Protocol = request.Protocol.Value.ToCoreProtocol();
        }

        if (request.Path is not null)
        {
            core.Connection.Path = request.Path;
        }

        if (request.Username is not null)
        {
            core.Authentication.UserName = request.Username;
        }

        if (request.Password is not null)
        {
            core.Authentication.Password = request.Password;
        }

        if (request.OverlayPosition is not null)
        {
            core.Display.OverlayPosition = ToCoreOverlayPosition(request.OverlayPosition.Value);
        }

        core.Stream.UseLowLatencyMode = request.StreamUseLowLatencyMode;

        if (request.StreamMaxLatencyMs > 0)
        {
            core.Stream.MaxLatencyMs = request.StreamMaxLatencyMs;
        }

        if (request.StreamRtspTransport is not null)
        {
            core.Stream.RtspTransport = request.StreamRtspTransport.Value.ToString().ToLowerInvariant();
        }

        if (request.StreamBufferDurationMs > 0)
        {
            core.Stream.BufferDurationMs = request.StreamBufferDurationMs;
        }
    }

    public static CameraProtocol ToApiProtocol(this CoreCameraProtocol protocol)
        => protocol switch
        {
            CoreCameraProtocol.Http => CameraProtocol.Http,
            CoreCameraProtocol.Https => CameraProtocol.Https,
            _ => CameraProtocol.Rtsp,
        };

    public static CoreCameraProtocol ToCoreProtocol(
        this CameraProtocol protocol)
        => protocol switch
        {
            CameraProtocol.Http => CoreCameraProtocol.Http,
            CameraProtocol.Https => CoreCameraProtocol.Https,
            _ => CoreCameraProtocol.Rtsp,
        };

    public static CameraConnectionState ToApiConnectionState(
        this CoreConnectionState state)
        => state switch
        {
            CoreConnectionState.Connecting => CameraConnectionState.Connecting,
            CoreConnectionState.Connected => CameraConnectionState.Connected,
            CoreConnectionState.Reconnecting => CameraConnectionState.Reconnecting,
            CoreConnectionState.Error => CameraConnectionState.Error,
            _ => CameraConnectionState.Disconnected,
        };

    public static RecordingStatusState ToApiRecordingState(
        this CoreRecordingState state)
        => state switch
        {
            CoreRecordingState.Recording => RecordingStatusState.Recording,
            CoreRecordingState.RecordingMotion => RecordingStatusState.RecordingMotion,
            CoreRecordingState.RecordingPostMotion => RecordingStatusState.RecordingPostMotion,
            _ => RecordingStatusState.Idle,
        };

    private static CameraOverlayPosition? ToApiOverlayPosition(
        CoreOverlayPosition position)
        => position switch
        {
            CoreOverlayPosition.TopRight => CameraOverlayPosition.TopRight,
            CoreOverlayPosition.BottomLeft => CameraOverlayPosition.BottomLeft,
            CoreOverlayPosition.BottomRight => CameraOverlayPosition.BottomRight,
            _ => CameraOverlayPosition.TopLeft,
        };

    private static CoreOverlayPosition ToCoreOverlayPosition(
        CameraOverlayPosition position)
        => position switch
        {
            CameraOverlayPosition.TopRight => CoreOverlayPosition.TopRight,
            CameraOverlayPosition.BottomLeft => CoreOverlayPosition.BottomLeft,
            CameraOverlayPosition.BottomRight => CoreOverlayPosition.BottomRight,
            _ => CoreOverlayPosition.TopLeft,
        };

    private static CameraStreamRtspTransport? ToApiRtspTransport(
        string? transport)
    {
        if (string.IsNullOrEmpty(transport))
        {
            return null;
        }

        return string.Equals(transport, "udp", StringComparison.OrdinalIgnoreCase)
            ? CameraStreamRtspTransport.Udp
            : CameraStreamRtspTransport.Tcp;
    }
}