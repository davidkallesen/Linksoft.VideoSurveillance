// ReSharper disable RedundantArgumentDefaultValue
namespace Linksoft.Wpf.CameraWall.Services;

/// <summary>
/// Service for detecting motion in camera video streams using frame differencing.
/// </summary>
[Registration(Lifetime.Singleton)]
public class MotionDetectionService : IMotionDetectionService, IDisposable
{
    private const int AnalysisWidth = 320;
    private const int AnalysisHeight = 240;

    private readonly ConcurrentDictionary<Guid, MotionDetectionContext> contexts = new();
    private bool disposed;

    /// <inheritdoc/>
    public event EventHandler<MotionDetectedEventArgs>? MotionDetected;

    /// <inheritdoc/>
    public void StartDetection(
        Guid cameraId,
        FlyleafLib.MediaPlayer.Player player,
        MotionDetectionSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(player);

        // Stop existing detection if any
        StopDetection(cameraId);

        var context = new MotionDetectionContext(cameraId, player, settings ?? new MotionDetectionSettings());
        if (!contexts.TryAdd(cameraId, context))
        {
            context.Dispose();
            return;
        }

        // Start the analysis timer
        context.Timer.Tick += (_, _) => AnalyzeFrame(cameraId);
        context.Timer.Interval = TimeSpan.FromMilliseconds(1000.0 / context.Settings.AnalysisFrameRate);
        context.Timer.Start();
    }

    /// <inheritdoc/>
    public void StopDetection(Guid cameraId)
    {
        if (contexts.TryRemove(cameraId, out var context))
        {
            context.Dispose();
        }
    }

    /// <inheritdoc/>
    public bool IsDetectionActive(Guid cameraId)
        => contexts.ContainsKey(cameraId);

    /// <inheritdoc/>
    public bool IsMotionDetected(Guid cameraId)
    {
        if (contexts.TryGetValue(cameraId, out var context))
        {
            return context.IsMotionDetected;
        }

        return false;
    }

    /// <summary>
    /// Disposes of the service resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var cameraId in contexts.Keys.ToList())
            {
                StopDetection(cameraId);
            }
        }

        disposed = true;
    }

    private void AnalyzeFrame(Guid cameraId)
    {
        if (!contexts.TryGetValue(cameraId, out var context))
        {
            return;
        }

        try
        {
            // TODO: Implement frame capture from FlyleafLib Player
            // FlyleafLib's Player.TakeSnapshotToFile() saves to a file, not memory.
            // Options for implementation:
            // 1. Use temp file approach: TakeSnapshotToFile to temp, read back, delete
            // 2. Use FlyleafLib's renderer API if available in future versions
            // 3. Hook into the video frame pipeline directly

            // For now, use temp file approach for frame capture
            var tempFile = Path.Combine(Path.GetTempPath(), $"motion_{cameraId:N}.png");

            try
            {
                context.Player.TakeSnapshotToFile(tempFile);

                if (!File.Exists(tempFile))
                {
                    return;
                }

                using var bitmap = new System.Drawing.Bitmap(tempFile);

                // Convert to grayscale and downscale for analysis
                var currentFrame = ConvertToGrayscale(bitmap, AnalysisWidth, AnalysisHeight);

                // Compare with previous frame
                if (context.PreviousFrame is not null)
                {
                    var changePercent = CalculateFrameDifference(
                        context.PreviousFrame,
                        currentFrame,
                        context.Settings.Sensitivity);

                    var wasMotionDetected = context.IsMotionDetected;
                    var minimumChange = context.Settings.MinimumChangePercent;

                    if (changePercent >= minimumChange)
                    {
                        // Check cooldown
                        var cooldownElapsed = context.LastMotionTime is null ||
                            (DateTime.UtcNow - context.LastMotionTime.Value).TotalSeconds >= context.Settings.CooldownSeconds;

                        if (cooldownElapsed || wasMotionDetected)
                        {
                            context.IsMotionDetected = true;
                            context.LastMotionTime = DateTime.UtcNow;

                            // Raise event
                            MotionDetected?.Invoke(
                                this,
                                new MotionDetectedEventArgs(cameraId, changePercent));
                        }
                    }
                    else
                    {
                        context.IsMotionDetected = false;
                    }
                }

                // Store current frame for next comparison
                context.PreviousFrame = currentFrame;
            }
            finally
            {
                // Clean up temp file
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Motion detection error for camera {cameraId}: {ex.Message}");
        }
    }

    private static byte[] ConvertToGrayscale(
        System.Drawing.Bitmap bitmap,
        int targetWidth,
        int targetHeight)
    {
        // Resize and convert to grayscale using safe code
        using var resized = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(targetWidth, targetHeight));

        var grayscale = new byte[targetWidth * targetHeight];

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var pixel = resized.GetPixel(x, y);

                // Standard grayscale conversion using luminosity method
                grayscale[(y * targetWidth) + x] = (byte)((0.299 * pixel.R) + (0.587 * pixel.G) + (0.114 * pixel.B));
            }
        }

        return grayscale;
    }

    private static double CalculateFrameDifference(
        byte[] previous,
        byte[] current,
        int sensitivity)
    {
        if (previous.Length != current.Length)
        {
            return 0;
        }

        // Threshold based on sensitivity (0-100)
        // Lower sensitivity = higher threshold (less sensitive to changes)
        // Higher sensitivity = lower threshold (more sensitive to changes)
        var threshold = (int)((100 - sensitivity) * 2.55); // 0-255 range

        var changedPixels = 0;

        for (var i = 0; i < previous.Length; i++)
        {
            var diff = Math.Abs(current[i] - previous[i]);
            if (diff > threshold)
            {
                changedPixels++;
            }
        }

        return (double)changedPixels / previous.Length * 100.0;
    }

    /// <summary>
    /// Internal context for tracking motion detection state per camera.
    /// </summary>
    private sealed class MotionDetectionContext : IDisposable
    {
        public MotionDetectionContext(
            Guid cameraId,
            FlyleafLib.MediaPlayer.Player player,
            MotionDetectionSettings settings)
        {
            CameraId = cameraId;
            Player = player;
            Settings = settings;
            Timer = new DispatcherTimer();
        }

        public Guid CameraId { get; }

        public FlyleafLib.MediaPlayer.Player Player { get; }

        public MotionDetectionSettings Settings { get; }

        public DispatcherTimer Timer { get; }

        public byte[]? PreviousFrame { get; set; }

        public bool IsMotionDetected { get; set; }

        public DateTime? LastMotionTime { get; set; }

        public void Dispose()
        {
            Timer.Stop();
            PreviousFrame = null;
        }
    }
}