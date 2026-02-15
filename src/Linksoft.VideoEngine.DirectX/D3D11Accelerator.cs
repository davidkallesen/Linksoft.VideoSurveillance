namespace Linksoft.VideoEngine.DirectX;

/// <summary>
/// Composition root implementing <see cref="IGpuAccelerator"/> for D3D11VA.
/// Composes <see cref="D3D11Device"/>, <see cref="HwAccelContext"/>,
/// <see cref="VideoProcessorRenderer"/>, and <see cref="GpuSnapshotCapture"/>.
/// </summary>
public sealed unsafe class D3D11Accelerator : IGpuAccelerator
{
    private readonly ILogger logger;
    private readonly D3D11Device d3d11Device;
    private readonly HwAccelContext hwAccelContext;
    private readonly VideoProcessorRenderer renderer;
    private readonly GpuSnapshotCapture snapshotCapture;
    private readonly Lock frameLock = new();

    private ID3D11Texture2D? latestBgraTexture;
    private int latestWidth;
    private int latestHeight;
    private bool disposed;

    public D3D11Accelerator(ILogger logger)
    {
        this.logger = logger;

        d3d11Device = new D3D11Device();
        hwAccelContext = new HwAccelContext(d3d11Device);
        renderer = new VideoProcessorRenderer(d3d11Device);
        snapshotCapture = new GpuSnapshotCapture(d3d11Device);

        logger.LogInformation("D3D11 GPU accelerator initialized");
    }

    public AVHWDeviceType HwDeviceType => AVHWDeviceType.D3d11va;

    public AVBufferRef* HwDeviceContext => hwAccelContext.DeviceContextBuffer;

    public bool IsInitialized => !disposed && HwDeviceContext is not null;

    /// <summary>
    /// Gets the underlying D3D11 device for swap chain creation.
    /// </summary>
    public D3D11Device D3D11DeviceRef => d3d11Device;

    public event Action? FrameReady;

    public void OnFrameDecoded(AVFrame* frame)
    {
        if (frame is null || (AVPixelFormat)frame->format != AVPixelFormat.D3d11)
        {
            return;
        }

        var texturePtr = frame->data[0];
        var arrayIndex = (int)frame->data[1];

        if (texturePtr == nint.Zero)
        {
            return;
        }

        // Wrap the FFmpeg-owned texture pointer as a Vortice COM object.
        // Do NOT dispose — the texture is owned by FFmpeg's HW frames context.
#pragma warning disable CA2000
        var nv12Texture = new ID3D11Texture2D(texturePtr);
#pragma warning restore CA2000
        var desc = nv12Texture.Description;
        int width = (int)desc.Width;
        int height = (int)desc.Height;

        try
        {
            // Hold frameLock during GPU processing + present to prevent
            // CaptureSnapshot from reading the output texture mid-write.
            // D3D11 immediate context is single-threaded.
            lock (frameLock)
            {
                renderer.ProcessFrame(nv12Texture, arrayIndex, width, height);

                latestBgraTexture = renderer.OutputTexture;
                latestWidth = renderer.OutputWidth;
                latestHeight = renderer.OutputHeight;

                FrameReady?.Invoke();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GPU frame processing failed");
        }
    }

    /// <summary>
    /// Retrieves the latest BGRA texture and its dimensions (thread-safe).
    /// </summary>
    public bool TryGetBgraTexture(
        out ID3D11Texture2D? texture,
        out int width,
        out int height)
    {
        lock (frameLock)
        {
            texture = latestBgraTexture;
            width = latestWidth;
            height = latestHeight;
        }

        return texture is not null && width > 0 && height > 0;
    }

    public byte[]? CaptureSnapshot()
    {
        // Hold frameLock during the entire capture to prevent OnFrameDecoded
        // from writing to the BGRA texture (via ProcessFrame) while
        // CopyResource reads from it. D3D11 immediate context is single-threaded.
        lock (frameLock)
        {
            if (latestBgraTexture is null || latestWidth <= 0 || latestHeight <= 0)
            {
                return null;
            }

            try
            {
                return snapshotCapture.Capture(latestBgraTexture, latestWidth, latestHeight);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "GPU snapshot capture failed");
                return null;
            }
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        lock (frameLock)
        {
            latestBgraTexture = null;
        }

        snapshotCapture.Dispose();
        renderer.Dispose();
        hwAccelContext.Dispose();
        d3d11Device.Dispose();

        logger.LogInformation("D3D11 GPU accelerator disposed");
    }
}