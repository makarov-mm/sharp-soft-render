using System.Drawing;
using System.Numerics;

namespace Renderer;

/// <summary>How a mesh is coloured: a flat colour, or textured and lit.</summary>
public readonly record struct Shading
{
    public Color BaseColor { get; init; }
    public Texture? Texture { get; init; }

    /// <summary>Unit direction towards the light, in screen space; null = unlit.</summary>
    public Vector3? LightDirection { get; init; }

    public float Ambient { get; init; }
    public float SpecularStrength { get; init; }
    public float Shininess { get; init; }

    public static Shading Unlit(Color color) => new() { BaseColor = color };

    /// <summary>Lambert diffuse + Blinn-Phong specular + ambient, Phong (per-pixel normal) shading.</summary>
    public static Shading Lit(Color baseColor, Texture? texture, Vector3 lightDirection,
                              float ambient = 0.15f, float specularStrength = 0.35f, float shininess = 32f) => new()
    {
        BaseColor = baseColor,
        Texture = texture,
        LightDirection = Vector3.Normalize(lightDirection),
        Ambient = ambient,
        SpecularStrength = specularStrength,
        Shininess = shininess,
    };
}

/// <summary>
/// Barycentric triangle rasterizer with a Z-buffer. Vertices are expected in
/// screen space (pixels, +Z towards the viewer); the pipeline is orthographic,
/// so affine interpolation of depth, UV and normals is exact.
/// </summary>
public static class Rasterizer
{
    /// <summary>Draws a triangle list: three consecutive vertices per triangle.</summary>
    public static void Draw(RenderTarget target, ReadOnlySpan<Vertex> triangles, in Shading shading)
    {
        for (int i = 0; i + 2 < triangles.Length; i += 3)
            DrawTriangle(target, triangles[i], triangles[i + 1], triangles[i + 2], shading);
    }

    private static void DrawTriangle(RenderTarget target, in Vertex v0, in Vertex v1, in Vertex v2, in Shading shading)
    {
        float x0 = v0.Position.X, y0 = v0.Position.Y, z0 = v0.Position.Z;
        float x1 = v1.Position.X, y1 = v1.Position.Y, z1 = v1.Position.Z;
        float x2 = v2.Position.X, y2 = v2.Position.Y, z2 = v2.Position.Z;

        float area = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);
        if (MathF.Abs(area) < 1e-6f) return;   // degenerate (or edge-on)

        int minX = Math.Max(0, (int)MathF.Floor(MathF.Min(x0, MathF.Min(x1, x2))));
        int maxX = Math.Min(target.Width - 1, (int)MathF.Ceiling(MathF.Max(x0, MathF.Max(x1, x2))));
        int minY = Math.Max(0, (int)MathF.Floor(MathF.Min(y0, MathF.Min(y1, y2))));
        int maxY = Math.Min(target.Height - 1, (int)MathF.Ceiling(MathF.Max(y0, MathF.Max(y1, y2))));
        if (minX > maxX || minY > maxY) return;

        // Barycentric coordinates as signed sub-areas over the signed area:
        // inside iff all three are >= 0, whatever the winding. They are affine
        // in x, so each row starts from the exact value and steps by a constant.
        float invArea = 1f / area;
        float db0dx = -(y2 - y1) * invArea, db0dy = (x2 - x1) * invArea;
        float db1dx = -(y0 - y2) * invArea, db1dy = (x0 - x2) * invArea;
        float startX = minX + 0.5f;

        bool lit = shading.LightDirection.HasValue;
        Vector3 light = shading.LightDirection.GetValueOrDefault();
        // the view direction is +Z, so the Blinn-Phong half vector is constant
        Vector3 half = lit ? Vector3.Normalize(light + Vector3.UnitZ) : Vector3.Zero;
        float specularScale = shading.SpecularStrength * 255f;
        int shininess = (int)shading.Shininess;   // integer exponents use repeated multiplication (see Pow)
        float baseR = shading.BaseColor.R, baseG = shading.BaseColor.G, baseB = shading.BaseColor.B;
        Texture? texture = shading.Texture;
        int[] pixels = target.Pixels;
        float[] depth = target.Depth;

        for (int y = minY; y <= maxY; y++)
        {
            float py = y + 0.5f;
            float b0 = ((x2 - x1) * (py - y1) - (y2 - y1) * (startX - x1)) * invArea;
            float b1 = ((x0 - x2) * (py - y2) - (y0 - y2) * (startX - x2)) * invArea;
            int row = y * target.Width;

            for (int x = minX; x <= maxX; x++, b0 += db0dx, b1 += db1dx)
            {
                float b2 = 1f - b0 - b1;
                if (b0 < 0f || b1 < 0f || b2 < 0f) continue;

                float z = b0 * z0 + b1 * z1 + b2 * z2;
                int index = row + x;
                if (z <= depth[index]) continue;

                float r = baseR, g = baseG, b = baseB;
                if (texture is not null)
                {
                    Vector2 uv = b0 * v0.Uv + b1 * v1.Uv + b2 * v2.Uv;
                    int texel = texture.Sample(uv.X, uv.Y);
                    r = (texel >> 16) & 0xFF;
                    g = (texel >> 8) & 0xFF;
                    b = texel & 0xFF;
                }

                if (lit)
                {
                    Vector3 n = VectorMath.NormalizeOrZero(b0 * v0.Normal + b1 * v1.Normal + b2 * v2.Normal);
                    // two-sided: face the normal towards the viewer, so models
                    // with inconsistent winding still light correctly
                    if (n.Z < 0f) n = -n;

                    float diffuse = MathF.Max(0f, Vector3.Dot(n, light));
                    float intensity = shading.Ambient + (1f - shading.Ambient) * diffuse;
                    float specular = 0f;
                    if (specularScale > 0f)
                    {
                        float facing = Vector3.Dot(n, half);
                        if (facing > 0f) specular = Pow(facing, shininess, shading.Shininess) * specularScale;
                    }
                    r = r * intensity + specular;
                    g = g * intensity + specular;
                    b = b * intensity + specular;
                }

                pixels[index] = unchecked((int)0xFF000000) | (Clamp(r) << 16) | (Clamp(g) << 8) | Clamp(b);
                depth[index] = z;
            }
        }
    }

    private static int Clamp(float channel) => Math.Clamp((int)channel, 0, 255);

    // x^e for a per-pixel specular term: exponentiation by squaring when the
    // exponent is a small integer (the usual case), MathF.Pow otherwise.
    private static float Pow(float x, int integerExponent, float exponent)
    {
        if (integerExponent != exponent || integerExponent is < 1 or > 4096) return MathF.Pow(x, exponent);
        float result = 1f;
        for (int e = integerExponent; e > 0; e >>= 1, x *= x)
            if ((e & 1) != 0) result *= x;
        return result;
    }
}
