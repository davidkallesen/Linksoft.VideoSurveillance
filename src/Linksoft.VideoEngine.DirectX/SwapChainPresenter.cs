namespace Linksoft.VideoEngine.DirectX;

/// <summary>
/// Manages a DXGI composition swap chain and DirectComposition visual tree
/// for presenting BGRA textures to a WPF-hosted HWND.
/// </summary>
public sealed class SwapChainPresenter : IDisposable
{
    private readonly D3D11Device d3d11Device;
    private readonly IDXGISwapChain1 swapChain;
    private readonly IDCompositionDevice dcompDevice;
    private readonly IDCompositionTarget dcompTarget;
    private readonly IDCompositionVisual dcompVisual;

    private readonly Lock presentLock = new();
    private uint swapChainWidth;
    private uint swapChainHeight;
    private int lastControlWidth;
    private int lastControlHeight;
    private float zoomLevel = 1.0f;
    private float panX;
    private float panY;
    private bool disposed;

    public SwapChainPresenter(
        D3D11Device d3d11Device,
        nint hwnd)
    {
        ArgumentNullException.ThrowIfNull(d3d11Device);

        this.d3d11Device = d3d11Device;

        using var dxgiDevice = d3d11Device.Device.QueryInterface<IDXGIDevice>();

        // Create swap chain via DXGI factory.
        dxgiDevice.GetAdapter(out var adapter);
        using (adapter)
        {
            using var factory = adapter.GetParent<IDXGIFactory2>();

            var desc = new SwapChainDescription1
            {
                Width = 2,
                Height = 2,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0),
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                AlphaMode = AlphaMode.Ignore,
                Flags = SwapChainFlags.None,
            };

            swapChain = factory.CreateSwapChainForComposition(
                d3d11Device.Device,
                desc);
        }

        swapChainWidth = 2;
        swapChainHeight = 2;

        // Create DirectComposition device, target, and visual.
        dcompDevice = DComp.DCompositionCreateDevice<IDCompositionDevice>(dxgiDevice);

        // Topmost: false — render DComp visuals behind child HWNDs so the
        // overlay window (WPF content with camera info) appears on top of video.
        dcompDevice.CreateTargetForHwnd(
            hwnd,
            false,
            out dcompTarget).CheckError();
        dcompDevice.CreateVisual(out dcompVisual).CheckError();

        dcompVisual.SetContent(swapChain).CheckError();
        dcompTarget.SetRoot(dcompVisual).CheckError();
        dcompDevice.Commit().CheckError();
    }

    /// <summary>
    /// Copies the BGRA texture to the swap chain back buffer and presents it.
    /// Called on the demux thread.
    /// </summary>
    public void Present(
        ID3D11Texture2D bgraTexture,
        int videoWidth,
        int videoHeight)
    {
        if (disposed)
        {
            return;
        }

        lock (presentLock)
        {
            if (disposed)
            {
                return;
            }

            var w = (uint)videoWidth;
            var h = (uint)videoHeight;

            if (w != swapChainWidth || h != swapChainHeight)
            {
                swapChain.ResizeBuffers(
                    0,
                    w,
                    h,
                    Format.Unknown,
                    SwapChainFlags.None);
                swapChainWidth = w;
                swapChainHeight = h;

                // Swap chain dimensions changed — update the DComp transform
                // so it scales correctly to the control size.
                if (lastControlWidth > 0 && lastControlHeight > 0)
                {
                    ApplyTransform();
                }
            }

            using var backBuffer = swapChain.GetBuffer<ID3D11Texture2D>(0);
            d3d11Device.DeviceContext.CopyResource(backBuffer, bgraTexture);
            swapChain.Present(0, PresentFlags.None);
        }
    }

    /// <summary>
    /// Updates the DComp visual transform to scale the swap chain content
    /// to the control size. Called on the WPF UI thread.
    /// </summary>
    public void Resize(
        int controlWidth,
        int controlHeight)
    {
        if (disposed || swapChainWidth == 0 || swapChainHeight == 0)
        {
            return;
        }

        lock (presentLock)
        {
            if (disposed || swapChainWidth == 0 || swapChainHeight == 0)
            {
                return;
            }

            lastControlWidth = controlWidth;
            lastControlHeight = controlHeight;
            ApplyTransform();
        }
    }

    /// <summary>
    /// Sets the zoom level and pan offset for the video viewport.
    /// </summary>
    /// <param name="zoom">Zoom level (1.0 = fit to control, >1.0 = zoomed in).</param>
    /// <param name="offsetX">Horizontal pan offset in normalized coordinates (0.0-1.0).</param>
    /// <param name="offsetY">Vertical pan offset in normalized coordinates (0.0-1.0).</param>
    public void SetZoom(
        float zoom,
        float offsetX,
        float offsetY)
    {
        if (disposed)
        {
            return;
        }

        lock (presentLock)
        {
            if (disposed)
            {
                return;
            }

            zoomLevel = Math.Max(1.0f, zoom);
            panX = offsetX;
            panY = offsetY;

            if (lastControlWidth > 0 && lastControlHeight > 0 && swapChainWidth > 0 && swapChainHeight > 0)
            {
                ApplyTransform();
            }
        }
    }

    private void ApplyTransform()
    {
        float scaleX = lastControlWidth / (float)swapChainWidth;
        float scaleY = lastControlHeight / (float)swapChainHeight;

        // Uniform scale preserves aspect ratio; the surface window's black
        // background provides letterbox/pillarbox bars automatically.
        float baseScale = Math.Min(scaleX, scaleY);
        float totalScale = baseScale * zoomLevel;

        // Video dimensions after scaling
        float scaledW = swapChainWidth * totalScale;
        float scaledH = swapChainHeight * totalScale;

        // Center when at fit-to-view; pan offset shifts the viewport when zoomed.
        // panX/panY are in normalized coordinates (0.0 = centered, -1.0/+1.0 = edges).
        float maxPanX = Math.Max(0, (scaledW - lastControlWidth) / 2f);
        float maxPanY = Math.Max(0, (scaledH - lastControlHeight) / 2f);

        float offsetX = ((lastControlWidth - scaledW) / 2f) - (panX * maxPanX);
        float offsetY = ((lastControlHeight - scaledH) / 2f) - (panY * maxPanY);

        var matrix = Matrix3x2.CreateScale(totalScale, totalScale)
            * Matrix3x2.CreateTranslation(offsetX, offsetY);
        dcompVisual.SetTransform(ref matrix);
        dcompDevice.Commit();
    }

    public void Dispose()
    {
        lock (presentLock)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            dcompVisual.Dispose();
            dcompTarget.Dispose();
            dcompDevice.Dispose();
            swapChain.Dispose();
        }
    }
}