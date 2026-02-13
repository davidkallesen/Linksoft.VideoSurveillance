namespace Linksoft.Wpf.CameraWall.Services;

using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingImaging = System.Drawing.Imaging;

/// <summary>
/// Service for generating recording thumbnails by capturing frames and creating a grid layout.
/// </summary>
[Registration(Lifetime.Singleton)]
public class ThumbnailGeneratorService : IThumbnailGeneratorService
{
    private const int FrameWidth = 320;
    private const int FrameHeight = 240;

    private readonly ILogger<ThumbnailGeneratorService> logger;
    private readonly ConcurrentDictionary<Guid, ThumbnailCaptureContext> captures = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailGeneratorService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ThumbnailGeneratorService(ILogger<ThumbnailGeneratorService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void StartCapture(
        Guid cameraId,
        IMediaPipeline pipeline,
        string videoFilePath,
        int tileCount = 4)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        // Validate tile count (must be 1 or 4)
        if (tileCount != 1 && tileCount != 4)
        {
            tileCount = 4;
        }

        // Stop any existing capture for this camera
        StopCapture(cameraId);

        var thumbnailPath = Path.ChangeExtension(videoFilePath, ".png");
        var context = new ThumbnailCaptureContext(pipeline, thumbnailPath, tileCount);

        if (!captures.TryAdd(cameraId, context))
        {
            return;
        }

        logger.LogDebug(
            "Starting thumbnail capture for camera {CameraId}, output: {ThumbnailPath}, tiles: {TileCount}",
            cameraId,
            thumbnailPath,
            tileCount);

        // Capture first frame immediately
        _ = CaptureFrameAsync(cameraId, context);

        // For single tile, we only need one frame - stop immediately
        if (tileCount == 1)
        {
            StopCapture(cameraId);
            return;
        }

        // Set up timer for remaining frames (only for 4-tile mode)
        context.Timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };

        context.Timer.Tick += (_, _) => OnTimerTick(cameraId);
        context.Timer.Start();
    }

    /// <inheritdoc/>
    public void StopCapture(Guid cameraId)
    {
        if (!captures.TryRemove(cameraId, out var context))
        {
            return;
        }

        context.Timer?.Stop();
        context.Timer = null;

        // Generate thumbnail with whatever frames we have
        GenerateThumbnail(cameraId, context);

        // Dispose captured frames
        foreach (var frame in context.CapturedFrames)
        {
            frame?.Dispose();
        }

        context.CapturedFrames.Clear();
    }

    /// <inheritdoc/>
    public bool IsCaptureActive(Guid cameraId)
        => captures.ContainsKey(cameraId);

    /// <inheritdoc/>
    public void StopAllCaptures()
    {
        var cameraIds = captures.Keys.ToList();

        foreach (var cameraId in cameraIds)
        {
            StopCapture(cameraId);
        }
    }

    private void OnTimerTick(Guid cameraId)
    {
        if (!captures.TryGetValue(cameraId, out var context))
        {
            return;
        }

        _ = CaptureFrameAsync(cameraId, context);

        // Check if we have all frames
        if (context.CapturedFrames.Count >= context.TileCount)
        {
            StopCapture(cameraId);
        }
    }

    private async Task CaptureFrameAsync(
        Guid cameraId,
        ThumbnailCaptureContext context)
    {
        try
        {
            var pngBytes = await context.Pipeline.CaptureFrameAsync().ConfigureAwait(false);

            if (pngBytes is null || pngBytes.Length == 0)
            {
                logger.LogWarning("Snapshot capture returned no data for camera {CameraId}", cameraId);
                context.CapturedFrames.Add(null);
                return;
            }

            using var memoryStream = new MemoryStream(pngBytes);
            var bitmap = new Drawing.Bitmap(memoryStream);
            context.CapturedFrames.Add(bitmap);

            logger.LogDebug(
                "Captured frame {FrameIndex} for camera {CameraId}",
                context.CapturedFrames.Count - 1,
                cameraId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to capture frame for camera {CameraId}", cameraId);
            context.CapturedFrames.Add(null);
        }
    }

    private void GenerateThumbnail(
        Guid cameraId,
        ThumbnailCaptureContext context)
    {
        if (context.CapturedFrames.Count == 0)
        {
            logger.LogWarning(
                "No frames captured for camera {CameraId}, skipping thumbnail generation",
                cameraId);
            return;
        }

        try
        {
            // Determine thumbnail dimensions based on tile count
            // 1 tile = 320x240 (single frame), 4 tiles = 640x480 (2x2 grid)
            var (thumbnailWidth, thumbnailHeight) = context.TileCount == 1
                ? (FrameWidth, FrameHeight)
                : (FrameWidth * 2, FrameHeight * 2);

            using var thumbnail = new Drawing.Bitmap(
                thumbnailWidth,
                thumbnailHeight,
                DrawingImaging.PixelFormat.Format24bppRgb);
            using var graphics = Drawing.Graphics.FromImage(thumbnail);

            // Fill background with black (for missing frames)
            graphics.Clear(Drawing.Color.Black);

            // Configure high-quality rendering
            graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality;

            if (context.TileCount == 1)
            {
                // Single tile: draw first frame scaled to fit
                var frame = context.CapturedFrames.FirstOrDefault();
                if (frame is not null)
                {
                    var destRect = new Drawing.Rectangle(0, 0, FrameWidth, FrameHeight);
                    graphics.DrawImage(frame, destRect);
                }
            }
            else
            {
                // Draw frames in 2x2 grid
                // Position mapping:
                // [0] Top-Left (0,0)     [1] Top-Right (320,0)
                // [2] Bottom-Left (0,240) [3] Bottom-Right (320,240)
                var positions = new[]
                {
                    (X: 0, Y: 0),
                    (X: FrameWidth, Y: 0),
                    (X: 0, Y: FrameHeight),
                    (X: FrameWidth, Y: FrameHeight),
                };

                for (var i = 0; i < context.TileCount; i++)
                {
                    var frame = i < context.CapturedFrames.Count ? context.CapturedFrames[i] : null;

                    if (frame is not null)
                    {
                        var destRect = new Drawing.Rectangle(
                            positions[i].X,
                            positions[i].Y,
                            FrameWidth,
                            FrameHeight);
                        graphics.DrawImage(frame, destRect);
                    }

                    // Missing frames stay black (already filled)
                }
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(context.ThumbnailPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save thumbnail
            thumbnail.Save(context.ThumbnailPath, DrawingImaging.ImageFormat.Png);

            logger.LogInformation(
                "Generated thumbnail for camera {CameraId} with {FrameCount}/{TotalFrames} frames ({TileCount} tiles): {ThumbnailPath}",
                cameraId,
                context.CapturedFrames.Count(f => f is not null),
                context.TileCount,
                context.TileCount,
                context.ThumbnailPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate thumbnail for camera {CameraId}", cameraId);
        }
    }

    /// <summary>
    /// Context for tracking thumbnail capture state for a single camera.
    /// </summary>
    private sealed class ThumbnailCaptureContext
    {
        public ThumbnailCaptureContext(
            IMediaPipeline pipeline,
            string thumbnailPath,
            int tileCount = 4)
        {
            Pipeline = pipeline;
            ThumbnailPath = thumbnailPath;
            TileCount = tileCount;
        }

        public IMediaPipeline Pipeline { get; }

        public string ThumbnailPath { get; }

        public int TileCount { get; }

        public List<Drawing.Bitmap?> CapturedFrames { get; } = [];

        public DispatcherTimer? Timer { get; set; }
    }
}