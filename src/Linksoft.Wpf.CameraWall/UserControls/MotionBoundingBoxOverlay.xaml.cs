#pragma warning disable CS0169, CS0414 // Field is never used / assigned but never used (dependency property backing fields)
namespace Linksoft.Wpf.CameraWall.UserControls;

/// <summary>
/// Overlay control for displaying motion detection bounding boxes.
/// </summary>
public partial class MotionBoundingBoxOverlay
{
    private const int DefaultAnalysisWidth = 320;
    private const int DefaultAnalysisHeight = 240;

    // Smoothed position values for jitter reduction
    private double smoothedLeft;
    private double smoothedTop;
    private double smoothedWidth;
    private double smoothedHeight;
    private bool hasInitialPosition;

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
        UpdateBrush();
        UpdateThickness();
    }

    /// <summary>
    /// Updates the bounding box display with a new position.
    /// </summary>
    /// <param name="boundingBox">The bounding box in analysis coordinates, or null to hide.</param>
    /// <param name="containerSize">The size of the container to map coordinates to.</param>
    public void UpdateBoundingBox(
        Rect? boundingBox,
        Size containerSize)
    {
        if (!IsOverlayEnabled || boundingBox is null || containerSize.Width <= 0 || containerSize.Height <= 0)
        {
            HideBoundingBox();
            return;
        }

        // Map from analysis coordinates to container coordinates
        var scaleX = containerSize.Width / AnalysisWidth;
        var scaleY = containerSize.Height / AnalysisHeight;

        var targetLeft = boundingBox.Value.X * scaleX;
        var targetTop = boundingBox.Value.Y * scaleY;
        var targetWidth = boundingBox.Value.Width * scaleX;
        var targetHeight = boundingBox.Value.Height * scaleY;

        // Apply smoothing to reduce jitter
        if (!hasInitialPosition || SmoothingFactor <= 0)
        {
            // First position or no smoothing - set directly
            smoothedLeft = targetLeft;
            smoothedTop = targetTop;
            smoothedWidth = targetWidth;
            smoothedHeight = targetHeight;
            hasInitialPosition = true;
        }
        else
        {
            // Apply exponential smoothing
            var alpha = 1.0 - SmoothingFactor;
            smoothedLeft = Lerp(smoothedLeft, targetLeft, alpha);
            smoothedTop = Lerp(smoothedTop, targetTop, alpha);
            smoothedWidth = Lerp(smoothedWidth, targetWidth, alpha);
            smoothedHeight = Lerp(smoothedHeight, targetHeight, alpha);
        }

        // Update rectangle position and size
        Canvas.SetLeft(BoundingBoxRect, smoothedLeft);
        Canvas.SetTop(BoundingBoxRect, smoothedTop);
        BoundingBoxRect.Width = Math.Max(1, smoothedWidth);
        BoundingBoxRect.Height = Math.Max(1, smoothedHeight);
        BoundingBoxRect.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the bounding box overlay.
    /// </summary>
    public void HideBoundingBox()
    {
        BoundingBoxRect.Visibility = Visibility.Collapsed;
        hasInitialPosition = false;
    }

    /// <summary>
    /// Resets the smoothing state, causing the next update to set position directly.
    /// </summary>
    public void ResetSmoothing()
    {
        hasInitialPosition = false;
    }

    private static void OnIsOverlayEnabledChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MotionBoundingBoxOverlay overlay && e.NewValue is bool isEnabled && !isEnabled)
        {
            overlay.HideBoundingBox();
        }
    }

    private static void OnBoxColorChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MotionBoundingBoxOverlay overlay)
        {
            overlay.UpdateBrush();
        }
    }

    private static void OnBoxThicknessChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is MotionBoundingBoxOverlay overlay)
        {
            overlay.UpdateThickness();
        }
    }

    private void UpdateBrush()
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(BoxColor);
            BoundingBoxRect.Stroke = new SolidColorBrush(color);
        }
        catch
        {
            // Fallback to default red color if parsing fails
            BoundingBoxRect.Stroke = new SolidColorBrush(Colors.Red);
        }
    }

    private void UpdateThickness()
    {
        BoundingBoxRect.StrokeThickness = BoxThickness;
    }

    private static double Lerp(
        double current,
        double target,
        double alpha)
        => current + ((target - current) * alpha);
}