namespace Renderer;

/// <summary>
/// CPU texture: packed ARGB pixels with bilinear sampling and repeat wrapping.
/// Decoding image files is the host's job (see <c>TextureIO</c>), so the
/// renderer core has no GDI+ dependency.
/// </summary>
public sealed class Texture
{
    private readonly int[] _pixels;

    public int Width { get; }
    public int Height { get; }

    public Texture(int[] argbPixels, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (argbPixels.Length != width * height)
            throw new ArgumentException("Pixel array size does not match the dimensions.", nameof(argbPixels));

        _pixels = argbPixels;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Bilinear sample with repeat wrapping; the UV origin is bottom-left
    /// (OBJ convention). Returns packed ARGB.
    /// </summary>
    public int Sample(float u, float v)
    {
        // repeat wrap into [0, 1); OBJ V points up, pixel rows go down
        u -= MathF.Floor(u);
        v = 1f - (v - MathF.Floor(v));

        float fx = u * Width - 0.5f;
        float fy = v * Height - 0.5f;
        int x0 = (int)MathF.Floor(fx);
        int y0 = (int)MathF.Floor(fy);
        float tx = fx - x0;
        float ty = fy - y0;

        int x1 = Wrap(x0 + 1, Width);
        int y1 = Wrap(y0 + 1, Height);
        x0 = Wrap(x0, Width);
        y0 = Wrap(y0, Height);

        int c00 = _pixels[y0 * Width + x0], c10 = _pixels[y0 * Width + x1];
        int c01 = _pixels[y1 * Width + x0], c11 = _pixels[y1 * Width + x1];

        int a = Lerp2(c00 >>> 24, c10 >>> 24, c01 >>> 24, c11 >>> 24, tx, ty);
        int r = Lerp2((c00 >> 16) & 0xFF, (c10 >> 16) & 0xFF, (c01 >> 16) & 0xFF, (c11 >> 16) & 0xFF, tx, ty);
        int g = Lerp2((c00 >> 8) & 0xFF, (c10 >> 8) & 0xFF, (c01 >> 8) & 0xFF, (c11 >> 8) & 0xFF, tx, ty);
        int b = Lerp2(c00 & 0xFF, c10 & 0xFF, c01 & 0xFF, c11 & 0xFF, tx, ty);
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    private static int Wrap(int i, int size)
    {
        i %= size;
        return i < 0 ? i + size : i;
    }

    private static int Lerp2(int c00, int c10, int c01, int c11, float tx, float ty)
    {
        float top = c00 + (c10 - c00) * tx;
        float bottom = c01 + (c11 - c01) * tx;
        return (int)(top + (bottom - top) * ty + 0.5f);
    }
}
