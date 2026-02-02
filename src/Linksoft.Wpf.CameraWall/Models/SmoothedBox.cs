namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Holds smoothed position values for a single bounding box.
/// </summary>
internal sealed class SmoothedBox
{
    public double Left { get; set; }

    public double Top { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public bool HasInitialPosition { get; set; }
}