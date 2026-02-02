namespace Linksoft.Wpf.CameraWall.Services.Internal;

/// <summary>
/// Internal context for tracking motion detection state per camera.
/// </summary>
internal sealed class MotionDetectionContext : IDisposable
{
    public MotionDetectionContext(
        Guid cameraId,
        Player player,
        MotionDetectionSettings settings)
    {
        CameraId = cameraId;
        Player = player;
        Settings = settings;
    }

    public Guid CameraId { get; }

    public Player Player { get; }

    public MotionDetectionSettings Settings { get; }

    public byte[]? PreviousFrame { get; set; }

    public bool IsMotionDetected { get; set; }

    public DateTime? LastMotionTime { get; set; }

    public IReadOnlyList<Rect> LastBoundingBoxes { get; set; } = [];

    public void Dispose()
    {
        PreviousFrame = null;
        LastBoundingBoxes = [];
    }
}