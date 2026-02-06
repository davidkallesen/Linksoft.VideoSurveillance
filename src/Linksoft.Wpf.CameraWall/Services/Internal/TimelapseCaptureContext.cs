namespace Linksoft.Wpf.CameraWall.Services.Internal;

/// <summary>
/// Context for an active timelapse capture session.
/// </summary>
internal sealed class TimelapseCaptureContext : IDisposable
{
    public TimelapseCaptureContext(
        CameraConfiguration camera,
        Player player,
        TimeSpan interval)
    {
        Camera = camera;
        Player = player;
        Interval = interval;
        Timer = new DispatcherTimer
        {
            Interval = interval,
        };
    }

    public CameraConfiguration Camera { get; }

    public Player Player { get; }

    public TimeSpan Interval { get; }

    public DispatcherTimer Timer { get; }

    public void Dispose()
    {
        Timer.Stop();
    }
}