namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents streaming settings for a network camera.
/// </summary>
public partial class StreamSettings : ObservableObject
{
    [ObservableProperty]
    private bool useLowLatencyMode = true;

    [ObservableProperty]
    private int maxLatencyMs = 500;

    [ObservableProperty]
    private string rtspTransport = "tcp";

    [ObservableProperty]
    private int bufferDurationMs;
}
