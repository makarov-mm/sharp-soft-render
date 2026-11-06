using System.Drawing;

namespace Renderer;

/// <summary>
/// A colour buffer (packed ARGB) with a depth buffer. +Z points at the
/// viewer: a fragment is visible when its Z is larger than what is stored.
/// </summary>
public sealed class RenderTarget
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>Row-major ARGB pixels, <c>Width * Height</c> of them.</summary>
    public int[] Pixels { get; }

    /// <summary>Row-major depth, <see cref="float.MinValue"/> where nothing was drawn.</summary>
    public float[] Depth { get; }

    public RenderTarget(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Width = width;
        Height = height;
        Pixels = new int[width * height];
        Depth = new float[width * height];
    }

    public void Clear(Color color)
    {
        Array.Fill(Pixels, color.ToArgb());
        Array.Fill(Depth, float.MinValue);
    }
}
