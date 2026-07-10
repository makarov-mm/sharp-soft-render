namespace Renderer;

public sealed class VertexBuffer
{
    private List<Vertex> _vertices;

    public VertexBuffer()
    {
        _vertices = new List<Vertex>();
    }

    public VertexBuffer(int capacity)
    {
        _vertices = new List<Vertex>(capacity);
    }

    /// <summary>Diffuse texture of the model, if any.</summary>
    public Texture Texture { get; set; }

    /// <summary>
    /// True when vertices carry meaningful (smooth) normals that should be
    /// interpolated across triangles instead of being recomputed per face.
    /// </summary>
    public bool HasNormals { get; set; }

    public void Add(Vertex vx)
    {
        _vertices.Add(vx);
    }

    public VertexBuffer Multiply(Matrix matrix)
    {
        var vb = new VertexBuffer(_vertices.Count)
        {
            Texture = Texture,
            HasNormals = HasNormals
        };

        foreach (Vertex vx in _vertices)
        {
            vb.Add(vx * matrix);
        }

        return vb;
    }

    public VertexBuffer Clone()
    {
        var vb = new VertexBuffer(_vertices.Count)
        {
            Texture = Texture,
            HasNormals = HasNormals
        };

        vb._vertices.AddRange(_vertices);
        return vb;
    }

    public static VertexBuffer operator* (VertexBuffer vb, Matrix matrix)
    {
        return vb.Multiply(matrix);
    }

    /// <summary>
    /// Legacy entry point: flat shading (face normal, light along +Z) or unlit
    /// constant color. Kept for the DXF path and the shadow pass.
    /// </summary>
    public void DrawTriangles(IRenderTarget target, Color color, bool lighting)
    {
        Draw(target, color, null, lighting, new Vector(0, 0, 1), ambient: 0, specularStrength: 0);
    }

    /// <summary>
    /// Per-pixel shaded rasterization: barycentric triangle fill with interpolated
    /// depth, UV and normals; bilinear texture sampling; Lambert diffuse +
    /// Blinn-Phong specular + ambient. Vertices are expected in screen space
    /// (the existing orthographic pipeline), so affine interpolation is exact.
    /// </summary>
    public void Draw(
        IRenderTarget target,
        Color baseColor,
        Texture texture,
        bool lighting,
        Vector lightDir,
        double ambient = 0.15,
        double specularStrength = 0.35,
        double shininess = 32)
    {
        Vector light = lightDir.Normalize();

        // View direction is +Z (larger Z is closer to the viewer);
        // half-vector for Blinn-Phong is constant for a directional light.
        Vector half = (light + new Vector(0, 0, 1)).Normalize();

        for (int i = 0; i + 2 < _vertices.Count; i += 3)
        {
            RasterizeTriangle(
                target,
                _vertices[i], _vertices[i + 1], _vertices[i + 2],
                baseColor, texture, lighting, light, half,
                ambient, specularStrength, shininess);
        }
    }

    private static void RasterizeTriangle(
        IRenderTarget target,
        Vertex v0, Vertex v1, Vertex v2,
        Color baseColor, Texture texture, bool lighting,
        Vector light, Vector half,
        double ambient, double specularStrength, double shininess)
    {
        double x0 = v0.Coords.X, y0 = v0.Coords.Y, z0 = v0.Coords.Z;
        double x1 = v1.Coords.X, y1 = v1.Coords.Y, z1 = v1.Coords.Z;
        double x2 = v2.Coords.X, y2 = v2.Coords.Y, z2 = v2.Coords.Z;

        double area = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);

        if (Math.Abs(area) < 1e-9)
            return;

        double invArea = 1.0 / area;

        int minX = Math.Max(0, (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2))));
        int maxX = Math.Min(target.Width - 1, (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2))));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2))));
        int maxY = Math.Min(target.Height - 1, (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2))));

        if (minX > maxX || minY > maxY)
            return;

        double baseR = baseColor.R, baseG = baseColor.G, baseB = baseColor.B;

        for (int y = minY; y <= maxY; ++y)
        {
            double py = y + 0.5;

            for (int x = minX; x <= maxX; ++x)
            {
                double px = x + 0.5;

                // Signed sub-areas; dividing by the signed full area makes the
                // test winding-independent: inside iff all coordinates >= 0.
                double b0 = ((x2 - x1) * (py - y1) - (y2 - y1) * (px - x1)) * invArea;
                double b1 = ((x0 - x2) * (py - y2) - (y0 - y2) * (px - x2)) * invArea;
                double b2 = 1.0 - b0 - b1;

                if (b0 < 0 || b1 < 0 || b2 < 0)
                    continue;

                double z = b0 * z0 + b1 * z1 + b2 * z2;

                if (z <= target.GetDepth(x, y))
                    continue;

                double r = baseR, g = baseG, b = baseB;

                if (texture != null)
                {
                    double u = b0 * v0.U + b1 * v1.U + b2 * v2.U;
                    double v = b0 * v0.V + b1 * v1.V + b2 * v2.V;

                    int texel = texture.Sample(u, v);
                    r = (texel >> 16) & 0xFF;
                    g = (texel >> 8) & 0xFF;
                    b = texel & 0xFF;
                }

                if (lighting)
                {
                    Vector n = new Vector(
                        b0 * v0.Normal.X + b1 * v1.Normal.X + b2 * v2.Normal.X,
                        b0 * v0.Normal.Y + b1 * v1.Normal.Y + b2 * v2.Normal.Y,
                        b0 * v0.Normal.Z + b1 * v1.Normal.Z + b2 * v2.Normal.Z).Normalize();

                    // Two-sided shading: flip the normal towards the viewer (+Z),
                    // models with inconsistent winding still light correctly.
                    if (n.Z < 0)
                        n = -n;

                    double diffuse = Math.Max(0, n.Dot(light));
                    double intensity = ambient + (1.0 - ambient) * diffuse;

                    double specular = specularStrength > 0
                        ? Math.Pow(Math.Max(0, n.Dot(half)), shininess) * specularStrength * 255.0
                        : 0;

                    r = r * intensity + specular;
                    g = g * intensity + specular;
                    b = b * intensity + specular;
                }

                target[x, y] = Color.FromArgb(
                    255,
                    Math.Min(Math.Max((int)r, 0), 255),
                    Math.Min(Math.Max((int)g, 0), 255),
                    Math.Min(Math.Max((int)b, 0), 255));

                target.SetDepth(x, y, z);
            }
        }
    }

    /// <summary>Assigns each triangle's face normal to its three vertices.</summary>
    public void CalcTrianglesNormals()
    {
        for (int i = 0; i + 2 < _vertices.Count; i += 3)
        {
            Vertex vx0 = _vertices[i];
            Vertex vx1 = _vertices[i + 1];
            Vertex vx2 = _vertices[i + 2];

            Vector normal = FaceNormal(vx0.Coords, vx1.Coords, vx2.Coords);

            vx0.Normal = normal;
            vx1.Normal = normal;
            vx2.Normal = normal;
        }
    }

    /// <summary>
    /// Computes smooth vertex normals by averaging area-weighted face normals
    /// over all vertices sharing the same position. Sets <see cref="HasNormals"/>.
    /// </summary>
    public void CalcSmoothNormals()
    {
        var accumulated = new Dictionary<(long, long, long), Vector>();

        (long, long, long) Key(Vector v) =>
            ((long)Math.Round(v.X * 10000.0),
             (long)Math.Round(v.Y * 10000.0),
             (long)Math.Round(v.Z * 10000.0));

        for (int i = 0; i + 2 < _vertices.Count; i += 3)
        {
            Vector a = _vertices[i].Coords;
            Vector b = _vertices[i + 1].Coords;
            Vector c = _vertices[i + 2].Coords;

            // Unnormalized cross product: length is proportional to the
            // triangle area, so large faces dominate — a natural weighting.
            Vector faceNormal = (b - a).Cross(c - a);

            for (int j = 0; j < 3; ++j)
            {
                var key = Key(_vertices[i + j].Coords);
                accumulated[key] = accumulated.TryGetValue(key, out Vector sum)
                    ? sum + faceNormal
                    : faceNormal;
            }
        }

        foreach (Vertex vertex in _vertices)
        {
            vertex.Normal = accumulated[Key(vertex.Coords)].Normalize();
        }

        HasNormals = true;
    }

    private static Vector FaceNormal(Vector a, Vector b, Vector c)
    {
        return (b - a).Cross(c - a).Normalize();
    }

    public void SortByDistance()
    {
        bool repeat = true;

        while (repeat)
        {
            repeat = false;

            for (int i = 0; i < _vertices.Count - 6; i += 3)
            {
                double dist0 = _vertices[i].Coords.Z + _vertices[i + 1].Coords.Z + _vertices[i + 2].Coords.Z;
                double dist1 = _vertices[i + 3].Coords.Z + _vertices[i + 4].Coords.Z + _vertices[i + 5].Coords.Z;

                if (dist0 > dist1)
                {
                    (_vertices[i], _vertices[i + 3]) = (_vertices[i + 3], _vertices[i]);
                    (_vertices[i + 1], _vertices[i + 4]) = (_vertices[i + 4], _vertices[i + 1]);
                    (_vertices[i + 2], _vertices[i + 5]) = (_vertices[i + 5], _vertices[i + 2]);
                    repeat = true;
                }
            }
        }
    }

    public double Width
    {
        get
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (Vertex vertex in _vertices)
            {
                maxValue = Math.Max(maxValue, vertex.Coords.X);
                minValue = Math.Min(minValue, vertex.Coords.X);
            }

            return Math.Max(maxValue - minValue, 0);
        }
    }

    public double Length
    {
        get
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (Vertex vertex in _vertices)
            {
                maxValue = Math.Max(maxValue, vertex.Coords.Z);
                minValue = Math.Min(minValue, vertex.Coords.Z);
            }

            return Math.Max(maxValue - minValue, 0);
        }
    }

    public double Height
    {
        get
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;

            foreach (Vertex vertex in _vertices)
            {
                maxValue = Math.Max(maxValue, vertex.Coords.Y);
                minValue = Math.Min(minValue, vertex.Coords.Y);
            }

            return Math.Max(maxValue - minValue, 0);
        }
    }

    public Vector Volume => new Vector(Width, Height, Length);

    public void CenterCoordsToZero()
    {
        if (_vertices.Count == 0)
            return;

        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

        foreach (Vertex vertex in _vertices)
        {
            minX = Math.Min(vertex.Coords.X, minX);
            maxX = Math.Max(vertex.Coords.X, maxX);

            minY = Math.Min(vertex.Coords.Y, minY);
            maxY = Math.Max(vertex.Coords.Y, maxY);

            minZ = Math.Min(vertex.Coords.Z, minZ);
            maxZ = Math.Max(vertex.Coords.Z, maxZ);
        }

        var offset = new Vector(
            (maxX + minX) / 2.0,
            (maxY + minY) / 2.0,
            (maxZ + minZ) / 2.0);

        foreach (Vertex vertex in _vertices)
        {
            vertex.Coords -= offset;
        }
    }
}
