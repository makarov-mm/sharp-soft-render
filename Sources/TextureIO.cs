using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Renderer;

/// <summary>GDI+ side of the pipeline: decoding image files, uploading a frame into a bitmap.</summary>
public static class TextureIO
{
    /// <summary>Decodes any format GDI+ reads (PNG, JPG, BMP, ...) into a <see cref="Texture"/>.</summary>
    public static Texture FromFile(string path)
    {
        using var bitmap = new Bitmap(path);
        var pixels = new int[bitmap.Width * bitmap.Height];
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                          ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
        return new Texture(pixels, bitmap.Width, bitmap.Height);
    }

    /// <summary>Copies the colour buffer into a 32-bit ARGB bitmap of the same size.</summary>
    public static void CopyTo(this RenderTarget target, Bitmap bitmap)
    {
        if (bitmap.Width != target.Width || bitmap.Height != target.Height)
            throw new ArgumentException("Bitmap size does not match the render target.", nameof(bitmap));

        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                          ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            if (data.Stride == target.Width * sizeof(int))
            {
                Marshal.Copy(target.Pixels, 0, data.Scan0, target.Pixels.Length);
            }
            else
            {
                for (int y = 0; y < target.Height; y++)
                    Marshal.Copy(target.Pixels, y * target.Width, data.Scan0 + y * data.Stride, target.Width);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
