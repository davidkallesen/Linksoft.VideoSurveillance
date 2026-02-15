namespace Linksoft.VideoEngine.DirectX;

/// <summary>
/// Converts NV12 decoded textures to BGRA using the D3D11 Video Processor,
/// which runs on the GPU's dedicated video processing hardware.
/// </summary>
internal sealed class VideoProcessorRenderer : IDisposable
{
    private readonly ID3D11VideoDevice videoDevice;
    private readonly ID3D11VideoContext videoContext;
    private readonly ID3D11Device device;

    private ID3D11VideoProcessorEnumerator? enumerator;
    private ID3D11VideoProcessor? processor;
    private ID3D11Texture2D? outputTexture;
    private ID3D11VideoProcessorOutputView? outputView;

    private int cachedWidth;
    private int cachedHeight;
    private bool disposed;

    public VideoProcessorRenderer(D3D11Device d3d11Device)
    {
        device = d3d11Device.Device;
        videoDevice = device.QueryInterface<ID3D11VideoDevice>();
        videoContext = d3d11Device.DeviceContext.QueryInterface<ID3D11VideoContext>();
    }

    /// <summary>
    /// Gets the latest BGRA output texture after processing.
    /// </summary>
    public ID3D11Texture2D? OutputTexture => outputTexture;

    /// <summary>
    /// Gets the current output width.
    /// </summary>
    public int OutputWidth => cachedWidth;

    /// <summary>
    /// Gets the current output height.
    /// </summary>
    public int OutputHeight => cachedHeight;

    /// <summary>
    /// Processes a decoded NV12 texture through the Video Processor, producing a BGRA output.
    /// </summary>
    /// <param name="nv12Texture">The NV12 texture from the decoder.</param>
    /// <param name="arraySlice">The texture array slice index.</param>
    /// <param name="width">The frame width.</param>
    /// <param name="height">The frame height.</param>
    public void ProcessFrame(
        ID3D11Texture2D nv12Texture,
        int arraySlice,
        int width,
        int height)
    {
        EnsurePipeline(width, height);

        var inputViewDesc = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = { ArraySlice = (uint)arraySlice, MipSlice = 0 },
        };

        videoDevice.CreateVideoProcessorInputView(
            nv12Texture,
            enumerator!,
            inputViewDesc,
            out var inputView).CheckError();

        using (inputView)
        {
            VideoProcessorStream[] streams =
            [
                new VideoProcessorStream
                {
                    Enable = true,
                    InputSurface = inputView,
                },
            ];

            videoContext.VideoProcessorBlt(
                processor!,
                outputView!,
                0,
                (uint)streams.Length,
                streams);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleasePipeline();
        videoContext.Dispose();
        videoDevice.Dispose();
    }

    private void EnsurePipeline(
        int width,
        int height)
    {
        if (enumerator is not null && cachedWidth == width && cachedHeight == height)
        {
            return;
        }

        ReleasePipeline();

        var contentDesc = new VideoProcessorContentDescription
        {
            InputFrameFormat = VideoFrameFormat.Progressive,
            InputWidth = (uint)width,
            InputHeight = (uint)height,
            OutputWidth = (uint)width,
            OutputHeight = (uint)height,
            InputFrameRate = new Rational(
                30,
                1),
            OutputFrameRate = new Rational(
                30,
                1),
            Usage = VideoUsage.PlaybackNormal,
        };

        videoDevice.CreateVideoProcessorEnumerator(
            ref contentDesc,
            out enumerator).CheckError();

        videoDevice.CreateVideoProcessor(
            enumerator,
            0,
            out processor).CheckError();

        var outputDesc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
        };

        outputTexture = device.CreateTexture2D(outputDesc);

        var outputViewDesc = new VideoProcessorOutputViewDescription
        {
            ViewDimension = VideoProcessorOutputViewDimension.Texture2D,
        };

        videoDevice.CreateVideoProcessorOutputView(
            outputTexture,
            enumerator,
            outputViewDesc,
            out outputView).CheckError();

        cachedWidth = width;
        cachedHeight = height;
    }

    private void ReleasePipeline()
    {
        outputView?.Dispose();
        outputView = null;

        outputTexture?.Dispose();
        outputTexture = null;

        processor?.Dispose();
        processor = null;

        enumerator?.Dispose();
        enumerator = null;

        cachedWidth = 0;
        cachedHeight = 0;
    }
}