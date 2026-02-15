namespace Linksoft.VideoEngine.DirectX;

/// <summary>
/// Factory that creates <see cref="D3D11Accelerator"/> instances.
/// Returns <c>null</c> when D3D11 GPU acceleration is not available.
/// </summary>
public sealed class D3D11AcceleratorFactory : IGpuAcceleratorFactory
{
    public IGpuAccelerator? TryCreate(ILogger logger)
    {
        try
        {
            return new D3D11Accelerator(logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "D3D11 GPU acceleration unavailable, using CPU fallback");
            return null;
        }
    }
}