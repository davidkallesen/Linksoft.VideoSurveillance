#pragma warning disable CS0169, CS0414 // Field is never used / assigned but never used (dependency property backing fields)
namespace Linksoft.Wpf.CameraWall.UserControls;

using Rectangle = System.Windows.Shapes.Rectangle;

/// <summary>
/// Overlay control for displaying multiple motion detection bounding boxes.
/// </summary>
public partial class MotionBoundingBoxOverlay
{
    private const int DefaultAnalysisWidth = 320;
    private const int DefaultAnalysisHeight = 240;
    private const int MaxBoundingBoxes = 10; // Limit to prevent excessive UI elements

    // Pool of rectangle elements for reuse
    private readonly List<Rectangle> rectanglePool = [];

    // Smoothed position values for jitter reduction (per box, indexed by position in list)
    private readonly List<SmoothedBox> smoothedBoxes = [];

    [DependencyProperty(DefaultValue = true, PropertyChangedCallback = nameof(OnIsOverlayEnabledChanged))]
    private bool isOverlayEnabled;

    [DependencyProperty(DefaultValue = "Red", PropertyChangedCallback = nameof(OnBoxColorChanged))]
    private string boxColor = "Red";

    [DependencyProperty(DefaultValue = 2, PropertyChangedCallback = nameof(OnBoxThicknessChanged))]
    private int boxThickness;

    [DependencyProperty(DefaultValue = 0.3)]
    private double smoothingFactor;

    [DependencyProperty(DefaultValue = DefaultAnalysisWidth)]
    private int analysisWidth;

    [DependencyProperty(DefaultValue = DefaultAnalysisHeight)]
    private int analysisHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="MotionBoundingBoxOverlay"/> class.
    /// </summary>
    public MotionBoundingBoxOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the bounding box display with new positions for multiple motion regions.
    /// </summary>
    /// <param name="boundingBoxes">The bounding boxes in analysis coordinates, or null/empty to hide all.</param>
    /// <param name="containerSize">The size of the container to map coordinates to.</param>
    public void UpdateBoundingBoxes(
        IReadOnlyList<Rect>? boundingBoxes,
        Size containerSize)
    {
        if (!IsOverlayEnabled ||
            boundingBoxes is null ||
            boundingBoxes.Count == 0 ||
            containerSize.Width <= 0 ||
            containerSize.Height <= 0)
        {
            HideBoundingBoxes();
            return;
        }

        // Limit the number of boxes to prevent performance issues
        var boxCount = Math.Min(boundingBoxes.Count, MaxBoundingBoxes);

        // Ensure we have enough rectangles in the pool
        EnsureRectangles(boxCount);

        // Calculate scale factors
        var scaleX = containerSize.Width / AnalysisWidth;
        var scaleY = containerSize.Height / AnalysisHeight;

        // Ensure we have enough smoothed box states
        while (smoothedBoxes.Count < boxCount)
        {
            smoothedBoxes.Add(new SmoothedBox());
        }

        // Update each bounding box
        for (var i = 0; i < boxCount; i++)
        {
            var box = boundingBoxes[i];
            var rect = rectanglePool[i];
            var smoothed = smoothedBoxes[i];

            // Map from analysis coordinates to container coordinates
            var targetLeft = box.X * scaleX;
            var targetTop = box.Y * scaleY;
            var targetWidth = box.Width * scaleX;
            var targetHeight = box.Height * scaleY;

            // Apply smoothing to reduce jitter
            if (!smoothed.HasInitialPosition || SmoothingFactor <= 0)
            {
                // First position or no smoothing - set directly
                smoothed.Left = targetLeft;
                smoothed.Top = targetTop;
                smoothed.Width = targetWidth;
                smoothed.Height = targetHeight;
                smoothed.HasInitialPosition = true;
            }
            else
            {
                // Apply exponential smoothing
                var alpha = 1.0 - SmoothingFactor;
                smoothed.Left = Lerp(smoothed.Left, targetLeft, alpha);
                smoothed.Top = Lerp(smoothed.Top, targetTop, alpha);
                smoothed.Width = Lerp(smoothed.Width, targetWidth, alpha);
                smoothed.Height = Lerp(smoothed.Height, targetHeight, alpha);
            }

            // Update rectangle position and size
            Canvas.SetLeft(rect, smoothed.Left);
            Canvas.SetTop(rect, smoothed.Top);
            rect.Width = Math.Max(1, smoothed.Width);
            rect.Height = Math.Max(1, smoothed.Height);
            rect.Visibility = Visibility.Visible;
        }

        // Hide unused rectangles
        for (var i = boxCount; i < rectanglePool.Count; i++)
        {
            rectanglePool[i].Visibility = Visibility.Collapsed;
        }

        // Reset smoothing for boxes that are no longer visible
        for (var i = boxCount; i < smoothedBoxes.Count; i++)
        {
            smoothedBoxes[i].HasInitialPosition = false;
        }
    }

    /// <summary>
    /// Updates the bounding box display with a single position (backward compatibility).
    /// </summary>
    /// <param name="boundingBox">The bounding box in analysis coordinates, or null to hide.</param>
    /// <param name="containerSize">The size of the container to map coordinates to.</param>
    public void UpdateBoundingBox(
        Rect? boundingBox,
        Size containerSize)
    {
        if (boundingBox.HasValue)
        {
            UpdateBoundingBoxes([boundingBox.Value], containerSize);
        }
        else
        {
            UpdateBoundingBoxes(null, containerSize);
        }
    }

    /// <summary>
    /// Hides all bounding box overlays.
    /// </summary>
    public void HideBoundingBoxes()
    {
        foreach (var rect in rectanglePool)
        {
            rect.Visibility = Visibility.Collapsed;
        }

        foreach (var smoothed in smoothedBoxes)
        {
            smoothed.HasInitialPosition = false;
        }
    }

    /// <summary>
    /// Hides the bounding box overlay (backward compatibility).
    /// </summary>
    public void HideBoundingBox()
    {
        HideBoundingBoxes();
    }

    /// <summary>
    /// Resets the smoothing state for all boxes, causing the next update to set positions directly.
    /// </summary>
    public void ResetSmoothing()
    {
        foreach (var smoothed in smoothedBoxes)
        {
            smoothed.HasInitialPosition = false;
        }
    }

    private void EnsureRectangles(int count)
    {
        // Add more rectangles to the pool if needed
        while (rectanglePool.Count < count)
        {
            var rect = new Rectangle
            {
                Fill = Brushes.Transparent,
                Visibility = Visibility.Collapsed,
            };

            UpdateRectangleBrush(rect);
            UpdateRectangleThickness(rect);

            rectanglePool.Add(rect);
            OverlayCanvas.Children.Add(rect);
        }
    }

    private static void OnIsOverlayEnabledChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MotionBoundingBoxOverlay overlay && e.NewValue is false)
        {
            overlay.HideBoundingBoxes();
        }
    }

    private static void OnBoxColorChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MotionBoundingBoxOverlay overlay)
        {
            overlay.UpdateAllBrushes();
        }
    }

    private static void OnBoxThicknessChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MotionBoundingBoxOverlay overlay)
        {
            overlay.UpdateAllThicknesses();
        }
    }

    private void UpdateAllBrushes()
    {
        foreach (var rect in rectanglePool)
        {
            UpdateRectangleBrush(rect);
        }
    }

    private void UpdateRectangleBrush(Rectangle rect)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(BoxColor);
            rect.Stroke = new SolidColorBrush(color);
        }
        catch
        {
            // Fallback to default red color if parsing fails
            rect.Stroke = new SolidColorBrush(Colors.Red);
        }
    }

    private void UpdateAllThicknesses()
    {
        foreach (var rect in rectanglePool)
        {
            UpdateRectangleThickness(rect);
        }
    }

    private void UpdateRectangleThickness(Rectangle rect)
    {
        rect.StrokeThickness = BoxThickness;
    }

    private static double Lerp(
        double current,
        double target,
        double alpha)
        => current + ((target - current) * alpha);
}