namespace Renderer;

/// <summary>
/// CPU-side texture: an ARGB pixel array with bilinear sampling and repeat wrapping.
/// Decoding image files into pixels is delegated to <see cref="TextureRegistry"/>
/// so this class stays free of any GDI+ dependency.
/// </summary>
public sealed class Texture
{
    private readonly int[] _pixels;

    public int Width { get; }
    public int Height { get; }

    public Texture(int[] argbPixels, int width, int height)
    {
        if (argbPixels == null || argbPixels.Length != width * height)
            throw new ArgumentException("Pixel array size does not match dimensions.");

        _pixels = argbPixels;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Bilinear sample with repeat wrapping. UV origin is bottom-left (OBJ convention).
    /// Returns packed ARGB.
    /// </summary>
    public int Sample(double u, double v)
    {
        // Repeat wrap into [0, 1).
        u -= Math.Floor(u);
        v -= Math.Floor(v);

        // OBJ V axis points up, pixel rows go down.
        v = 1.0 - v;

        double fx = u * Width - 0.5;
        double fy = v * Height - 0.5;

        int x0 = (int)Math.Floor(fx);
        int y0 = (int)Math.Floor(fy);

        double tx = fx - x0;
        double ty = fy - y0;

        int x1 = Wrap(x0 + 1, Width);
        int y1 = Wrap(y0 + 1, Height);
        x0 = Wrap(x0, Width);
        y0 = Wrap(y0, Height);

        int c00 = _pixels[y0 * Width + x0];
        int c10 = _pixels[y0 * Width + x1];
        int c01 = _pixels[y1 * Width + x0];
        int c11 = _pixels[y1 * Width + x1];

        int a = Lerp2(A(c00), A(c10), A(c01), A(c11), tx, ty);
        int r = Lerp2(R(c00), R(c10), R(c01), R(c11), tx, ty);
        int g = Lerp2(G(c00), G(c10), G(c01), G(c11), tx, ty);
        int b = Lerp2(B(c00), B(c10), B(c01), B(c11), tx, ty);

        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    private static int Wrap(int i, int size)
    {
        i %= size;
        return i < 0 ? i + size : i;
    }

    private static int A(int c) => (c >> 24) & 0xFF;
    private static int R(int c) => (c >> 16) & 0xFF;
    private static int G(int c) => (c >> 8) & 0xFF;
    private static int B(int c) => c & 0xFF;

    private static int Lerp2(int c00, int c10, int c01, int c11, double tx, double ty)
    {
        double top = c00 + (c10 - c00) * tx;
        double bottom = c01 + (c11 - c01) * tx;
        return (int)(top + (bottom - top) * ty + 0.5);
    }
}

/// <summary>
/// Pluggable texture file decoder. The WinForms host installs a GDI+ based
/// implementation at startup; other hosts (tests, ports) can install their own.
/// </summary>
public static class TextureRegistry
{
    public static Func<string, Texture> LoadFromFile { get; set; } = _ => null;
}
