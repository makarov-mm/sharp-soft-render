using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Renderer;

/// <summary>
/// GDI+ based texture decoding (PNG, JPG, BMP, ...). Registered into
/// <see cref="TextureRegistry"/> by the WinForms host at startup.
/// </summary>
public static class TextureIO
{
    public static Texture FromFile(string path)
    {
        using var bmp = new Bitmap(path);

        var pixels = new int[bmp.Width * bmp.Height];

        BitmapData data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return new Texture(pixels, bmp.Width, bmp.Height);
    }
}
